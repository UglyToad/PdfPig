namespace UglyToad.PdfPig.Fonts.TrueType
{
    using System;
    using System.Collections.Generic;
    using Core;
    using Parser;
    using Tables.CMapSubTables;
    using UglyToad.PdfPig.Fonts.CompactFontFormat;

    /// <summary>
    /// A TrueType font.
    /// </summary>
    public sealed class TrueTypeFont
    {
        /// <summary>
        /// The font version number.
        /// </summary>
        public float Version { get; }

        /// <summary>
        /// The table directory, entries indicate the offset and length of the data for a given table name.
        /// </summary>
        public IReadOnlyDictionary<string, TrueTypeHeaderTable> TableHeaders { get; }

        /// <summary>
        /// The actual table data parsed for this TrueType font.
        /// </summary>
        public TableRegister TableRegister { get; }

        /// <summary>
        /// The name of the font according to the font's name table.
        /// </summary>
        public string Name => TableRegister.NameTable?.FontName;

        /// <summary>
        /// The cmap subtable for Windows Unicode (3, 1).
        /// Can be <see langword="null" />.
        /// </summary>
        public ICMapSubTable WindowsUnicodeCMap { get; }

        /// <summary>
        /// The cmap subtable for Mac Roman (1, 0).
        /// Can be <see langword="null" />.
        /// </summary>
        public ICMapSubTable MacRomanCMap { get; }

        /// <summary>
        /// The cmap subtable for Windows Symbol (3, 0).
        /// Can be <see langword="null" />.
        /// </summary>
        public ICMapSubTable WindowsSymbolCMap { get; }

        /// <summary>
        /// The number of tables in this font.
        /// </summary>
        public int NumberOfTables { get; }

        /// <summary>
        /// The glyph outlines of an OpenType font with PostScript outlines (a CFF table instead of glyf), if any.
        /// </summary>
        // TODO - It would be better to use 'PdfCidCompactFontFormatFont' but the class is not accessible from here.
        internal CompactFontFormatFontCollection? CffFontCollection { get; }

        /// <summary>
        /// Create a new <see cref="TrueTypeFont"/>.
        /// </summary>
        internal TrueTypeFont(float version, IReadOnlyDictionary<string, TrueTypeHeaderTable> tableHeaders, TableRegister tableRegister, CompactFontFormatFontCollection? cffFontCollection)
        {
            Version = version;
            TableHeaders = tableHeaders ?? throw new ArgumentNullException(nameof(tableHeaders));
            TableRegister = tableRegister ?? throw new ArgumentNullException(nameof(tableRegister));
            NumberOfTables = tableHeaders.Count;

            /*
             * The presence of a CFF (Compact Font Format) table in a TrueType font creates a hybrid situation where the font
             * container uses TrueType structure but contains PostScript-based glyph descriptions. According to the OpenType
             * specification, when a TrueType font contains a CFF table instead of a traditional glyf table, it indicates
             * "an OpenType font with PostScript outlines". This creates what's known as an OpenType CFF font, which uses
             * PostScript Type 2 charstrings for glyph descriptions rather than TrueType quadratic curves.
             *
             * This is to fix P2P-33713919.pdf
             * See https://github.com/BobLd/PdfPig.Rendering.Skia/issues/46
             * TODO - Add test coverage and need to review if the logic belongs here
             */
            CffFontCollection = cffFontCollection;

            if (TableRegister.CMapTable is not null)
            {
                const int encodingSymbol = 0;
                const int encodingUnicode = 1;
                const int encodingMacRoman = 0;

                foreach (var subTable in TableRegister.CMapTable.SubTables)
                {
                    if (WindowsSymbolCMap is null
                        && subTable.PlatformId == TrueTypeCMapPlatform.Windows
                        && subTable.EncodingId == encodingSymbol)
                    {
                        WindowsSymbolCMap = subTable;
                    }
                    else if (WindowsUnicodeCMap is null
                             && subTable.PlatformId == TrueTypeCMapPlatform.Windows
                             && subTable.EncodingId == encodingUnicode)
                    {
                        WindowsUnicodeCMap = subTable;
                    }
                    else if (MacRomanCMap is null
                             && subTable.PlatformId == TrueTypeCMapPlatform.Macintosh
                             && subTable.EncodingId == encodingMacRoman)
                    {
                        MacRomanCMap = subTable;
                    }
                }
            }
        }

        /// <summary>
        /// Try to get the bounding box for a glyph representing the specified character code if present.
        /// </summary>
        public bool TryGetBoundingBox(int characterCode, out PdfRectangle boundingBox) => TryGetBoundingBox(characterCode, null, out boundingBox);

        /// <summary>
        /// Try to get the bounding box for a glyph representing the specified character code if present.
        /// Uses a custom mapping of character code to glyph index.
        /// </summary>
        public bool TryGetBoundingBox(int characterCode, Func<int, int?> characterCodeToGlyphId, out PdfRectangle boundingBox)
        {
            boundingBox = default(PdfRectangle);

            if (TableRegister.GlyphTable is null)
            {
                // An OpenType font with PostScript outlines, see TryGetCffGlyphName.
                if (TryGetCffGlyphName(characterCode, characterCodeToGlyphId, out var cffFont, out var name))
                {
                    var bbox = cffFont.GetCharacterBoundingBox(name);
                    if (bbox.HasValue)
                    {
                        boundingBox = bbox.Value;
                        return true;
                    }
                }

                return false;
            }

            if (!TryGetGlyphIndex(characterCode, characterCodeToGlyphId, out var index))
            {
                return false;
            }

            if (!TableRegister.GlyphTable.TryGetGlyphBounds(index, out boundingBox))
            {
                return false;
            }

            if (boundingBox.Width.Equals(0) && TryGetBoundingAdvancedWidthByIndex(index, out var advanceWidth))
            {
                boundingBox = new PdfRectangle(0, 0, advanceWidth, 0);
            }

            return true;
        }

        /// <summary>
        /// Try to get the bounding box for a glyph representing the specified character code if present.
        /// </summary>
        public bool TryGetPath(int characterCode, out IReadOnlyList<PdfSubpath> path) => TryGetPath(characterCode, null, out path);

        /// <summary>
        /// Try to get the path for a glyph representing the specified character code if present.
        /// Uses a custom mapping of character code to glyph index.
        /// </summary>
        public bool TryGetPath(int characterCode, Func<int, int?> characterCodeToGlyphId, out IReadOnlyList<PdfSubpath> path)
        {
            path = null;

            if (TableRegister.GlyphTable is null)
            {
                // An OpenType font with PostScript outlines, see TryGetCffGlyphName.
                return TryGetCffGlyphName(characterCode, characterCodeToGlyphId, out var cffFont, out var name)
                       && cffFont.TryGetPath(name, out path);
            }

            if (!TryGetGlyphIndex(characterCode, characterCodeToGlyphId, out var index))
            {
                return false;
            }

            return TableRegister.GlyphTable.TryGetGlyphPath(index, out path);
        }

        /// <summary>
        /// The CFF glyph for a character code, in a font with a CFF table instead of glyf.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The presence of a CFF (Compact Font Format) table in a TrueType font creates a hybrid situation where the font
        /// container uses TrueType structure but contains PostScript-based glyph descriptions. According to the OpenType
        /// specification, when a TrueType font contains a CFF table instead of a traditional glyf table, it indicates
        /// "an OpenType font with PostScript outlines". This creates what's known as an OpenType CFF font, which uses
        /// PostScript Type 2 charstrings for glyph descriptions rather than TrueType quadratic curves.
        /// This is to fix P2P-33713919.pdf, see https://github.com/BobLd/PdfPig.Rendering.Skia/issues/46.
        /// </para>
        /// <para>
        /// The glyph index is resolved as for glyf outlines (<see cref="TryGetGlyphIndex(int, Func{int, int?}, out int)"/>):
        /// the supplied mapping (for a CIDFont the CIDToGIDMap, or Identity when none, see
        /// https://github.com/UglyToad/PdfPig/issues/1320), else the font's cmap, which an installed font is read with.
        /// Only a font without a cmap takes the code as the glyph index. The index is then resolved by glyph id in the
        /// CFF (see <see cref="CompactFontFormatFont.GetCharacterName"/>).
        /// </para>
        /// <para>
        /// TODO: for a CID-keyed CFF that treats the glyph index as a CID, which is only right when the charset maps
        /// glyph indexes to themselves; see the note on CID-keyed fonts in SystemFontFinder.
        /// </para>
        /// </remarks>
        private bool TryGetCffGlyphName(int characterCode, Func<int, int?>? characterCodeToGlyphId,
            out CompactFontFormatFont cffFont, out string name)
        {
            cffFont = CffFontCollection?.FirstFont!;
            name = null!;

            if (cffFont is null)
            {
                return false;
            }

            if (!TryGetGlyphIndex(characterCode, characterCodeToGlyphId, out var cffGlyphId))
            {
                if (TableRegister.CMapTable is not null)
                {
                    return false;
                }

                cffGlyphId = characterCode;
            }

            name = cffFont.GetCharacterName(cffGlyphId, true);
            return !string.IsNullOrEmpty(name);
        }

        /// <summary>
        /// Try to get the advance width for a glyph representing the specified character code if present.
        /// </summary>
        public bool TryGetAdvanceWidth(int characterCode, out double width) => TryGetAdvanceWidth(characterCode, null, out width);

        /// <summary>
        /// Try to get the advance width for a glyph representing the specified character code if present.
        /// </summary>
        public bool TryGetAdvanceWidth(int characterCode, Func<int, int?> characterCodeToGlyphId, out double width)
        {
            width = 0.0;

            if (!TryGetGlyphIndex(characterCode, characterCodeToGlyphId, out var index))
            {
                return false;
            }

            return TryGetBoundingAdvancedWidthByIndex(index, out width);
        }

        /// <summary>
        /// Get the number of units per em for this font.
        /// </summary>
        public int GetUnitsPerEm()
        {
            return TableRegister.HeaderTable.UnitsPerEm;
        }

        private bool TryGetBoundingAdvancedWidthByIndex(int index, out double width)
        {
            width = 0;

            if (TableRegister.HorizontalMetricsTable is null)
            {
                return false;
            }

            width = TableRegister.HorizontalMetricsTable.GetAdvanceWidth(index);

            return true;
        }

        /// <summary>
        /// Try to get the glyph index for the specified character code by looking it up in the font's cmap table.
        /// </summary>
        /// <param name="characterCode">The character code (or CID) to resolve.</param>
        /// <param name="glyphId">The glyph index in this font's glyf/CFF table; 0 is .notdef.</param>
        /// <returns><see langword="true"/> when the font has a cmap table containing the code.</returns>
        public bool TryGetGlyphIndex(int characterCode, out int glyphId) => TryGetGlyphIndex(characterCode, null, out glyphId);

        /// <summary>
        /// Try to get the glyph index for the specified character code. This is the same resolution
        /// <see cref="TryGetPath(int, Func{int, int?}, out IReadOnlyList{PdfSubpath})"/> uses: when the
        /// supplied mapping yields a glyph index it is used directly (e.g. a CIDToGIDMap or the PDF's
        /// simple-font encoding rules), otherwise the raw code is looked up in the font's cmap table.
        /// </summary>
        /// <param name="characterIdentifier">The character code (or CID) to resolve.</param>
        /// <param name="characterCodeToGlyphId">
        /// Optional external mapping. Return <see langword="null"/> to fall back to the cmap lookup.
        /// </param>
        /// <param name="glyphId">The glyph index in this font's glyf/CFF table; 0 is .notdef.</param>
        public bool TryGetGlyphIndex(int characterIdentifier, Func<int, int?>? characterCodeToGlyphId, out int glyphId)
        {
            glyphId = 0;

            var externalGlyphId = characterCodeToGlyphId?.Invoke(characterIdentifier);

            if (externalGlyphId != null)
            {
                glyphId = externalGlyphId.Value;
                return true;
            }

            if (TableRegister.CMapTable is null)
            {
                return false;
            }

            return TableRegister.CMapTable.TryGetGlyphIndex(characterIdentifier, out glyphId);
        }
    }
}
