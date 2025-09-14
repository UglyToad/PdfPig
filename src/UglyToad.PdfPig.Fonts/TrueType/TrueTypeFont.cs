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

        // TODO - It would be better to use 'PdfCidCompactFontFormatFont' but the class is not accessible from here.
        private readonly CompactFontFormatFontCollection? cffFontCollection;

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
            this.cffFontCollection = cffFontCollection;

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
                if (cffFontCollection is not null)
                {
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

                    var name = cffFontCollection.FirstFont.GetCharacterName(characterCode, true); // TODO cid?
                    if (string.IsNullOrEmpty(name))
                    {
                        return false;
                    }

                    var bbox = cffFontCollection.FirstFont.GetCharacterBoundingBox(name);
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
                if (cffFontCollection is not null)
                {
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

                    var name = cffFontCollection.FirstFont.GetCharacterName(characterCode, true);
                    if (string.IsNullOrEmpty(name))
                    {
                        return false;
                    }
                    return cffFontCollection.FirstFont.TryGetPath(name, out path);
                }

                return false;
            }

            if (!TryGetGlyphIndex(characterCode, characterCodeToGlyphId, out var index))
            {
                return false;
            }

            return TableRegister.GlyphTable.TryGetGlyphPath(index, out path);
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

        private bool TryGetGlyphIndex(int characterIdentifier, Func<int, int?> characterCodeToGlyphId, out int glyphId)
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
