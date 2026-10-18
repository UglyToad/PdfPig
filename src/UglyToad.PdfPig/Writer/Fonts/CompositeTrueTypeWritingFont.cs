namespace UglyToad.PdfPig.Writer.Fonts
{
    using System;
    using System.Collections.Generic;
    using Core;
    using PdfFonts;
    using PdfPig.Fonts;
    using PdfPig.Fonts.TrueType;
    using PdfPig.Fonts.TrueType.Parser;
    using PdfPig.Fonts.TrueType.Subsetting;
    using PdfPig.Fonts.TrueType.Tables;
    using PdfPig.Fonts.TrueType.Tables.CMapSubTables;
    using Tokens;

    /// <summary>
    /// A TrueType font written as a Type 0 font with the Identity-H encoding and a CIDFontType2 descendant.
    /// Each character gets a 2-byte code in order of first use, which is also its glyph index in the embedded subset.
    /// </summary>
    internal sealed class CompositeTrueTypeWritingFont : IWritingFont
    {
        private readonly TrueTypeFont font;
        private readonly ReadOnlyMemory<byte> fontFileBytes;
        private readonly ICMapSubTable unicodeCMap;

        private readonly object mappingLock = new object();
        private readonly Dictionary<char, ushort> characterCodes = new Dictionary<char, ushort>();
        private readonly List<char> characters = new List<char>();

        public bool HasWidths { get; } = true;

        public string Name => font.Name;

        public CompositeTrueTypeWritingFont(TrueTypeFont font, ReadOnlyMemory<byte> fontFileBytes)
        {
            this.font = font;
            this.fontFileBytes = fontFileBytes;
            unicodeCMap = font.WindowsUnicodeCMap
                ?? throw new InvalidFontFormatException("A composite font requires a Windows Unicode (platform 3, encoding 1) cmap subtable.");
        }

        public bool TryGetBoundingBox(char character, out PdfRectangle boundingBox)
        {
            boundingBox = default;
            return TryGetGlyphIndex(character, out var glyphIndex)
                   && font.TryGetBoundingBox(glyphIndex, _ => glyphIndex, out boundingBox);
        }

        public bool TryGetAdvanceWidth(char character, out double width)
        {
            width = 0;
            return TryGetGlyphIndex(character, out var glyphIndex)
                   && font.TryGetAdvanceWidth(glyphIndex, _ => glyphIndex, out width);
        }

        public TransformationMatrix GetFontMatrix()
        {
            var unitsPerEm = font.GetUnitsPerEm();
            return TransformationMatrix.FromValues(1.0 / unitsPerEm, 0, 0, 1.0 / unitsPerEm, 0, 0);
        }

        public void AppendCharacterCode(char character, List<byte> textBytes)
        {
            var code = GetCharacterCode(character);
            textBytes.Add((byte)(code >> 8));
            textBytes.Add((byte)code);
        }

        public IndirectReferenceToken WriteFont(IPdfStreamWriter writer, IndirectReferenceToken? reservedIndirect = null)
        {
            var subsetBytes = TrueTypeSubsetter.Subset(fontFileBytes.ToArray(), new TrueTypeSubsetEncoding(characters));
            var fontFile = writer.WriteToken(DataCompressor.CompressToStream(subsetBytes));

            var subset = TrueTypeFontParser.Parse(new TrueTypeDataBytes(new MemoryInputBytes(subsetBytes)));
            var cidSet = writer.WriteToken(CreateCidSet(subset.TableRegister.IndexToLocationTable.GlyphOffsets.Count - 1));

            var baseFont = NameToken.Create(GetSubsetTag() + font.TableRegister.NameTable.GetPostscriptName());

            var descriptor = writer.WriteToken(CreateFontDescriptor(baseFont, fontFile, cidSet));

            var cidFontDictionary = new Dictionary<NameToken, IToken>
            {
                { NameToken.Type, NameToken.Font },
                { NameToken.Subtype, NameToken.CidFontType2 },
                { NameToken.BaseFont, baseFont },
                {
                    NameToken.CidSystemInfo, new DictionaryToken(new Dictionary<NameToken, IToken>
                    {
                        { NameToken.Registry, new StringToken("Adobe") },
                        { NameToken.Ordering, new StringToken("Identity") },
                        { NameToken.Supplement, new NumericToken(0) }
                    })
                },
                { NameToken.FontDescriptor, descriptor },
                { NameToken.CidToGidMap, NameToken.Identity }
            };

            if (characters.Count > 0)
            {
                cidFontDictionary[NameToken.W] = new ArrayToken(new IToken[] { new NumericToken(1), GetWidths() });
            }

            var cidFont = writer.WriteToken(new DictionaryToken(cidFontDictionary));

            var toUnicode = writer.WriteToken(DataCompressor.CompressToStream(ToUnicodeCMapBuilder.ConvertToCMapStream(characterCodes)));

            var dictionary = new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                { NameToken.Type, NameToken.Font },
                { NameToken.Subtype, NameToken.Type0 },
                { NameToken.BaseFont, baseFont },
                { NameToken.Encoding, NameToken.IdentityH },
                { NameToken.DescendantFonts, new ArrayToken(new IToken[] { cidFont }) },
                { NameToken.ToUnicode, toUnicode }
            });

            return reservedIndirect is not null
                ? writer.WriteToken(dictionary, reservedIndirect)
                : writer.WriteToken(dictionary);
        }

        private bool TryGetGlyphIndex(char character, out int glyphIndex)
        {
            glyphIndex = unicodeCMap.CharacterCodeToGlyphIndex(character);
            return glyphIndex != 0;
        }

        private ushort GetCharacterCode(char character)
        {
            lock (mappingLock)
            {
                if (characterCodes.TryGetValue(character, out var code))
                {
                    return code;
                }

                if (characters.Count == ushort.MaxValue)
                {
                    throw new NotSupportedException($"Cannot support more than {ushort.MaxValue} separate characters in a composite font.");
                }

                // Code 0 selects the .notdef glyph, so the characters take the codes from 1 onwards.
                code = (ushort)(characters.Count + 1);
                characters.Add(character);
                characterCodes[character] = code;

                return code;
            }
        }

        /// <summary>
        /// The six uppercase letters and plus sign that mark a font subset. As in PDFBox's TrueTypeEmbedder.getTag the tag
        /// is derived from the subset, so the same subset always gets the same tag.
        /// </summary>
        private string GetSubsetTag()
        {
            var hash = 17u;
            foreach (var character in characters)
            {
                hash = unchecked((hash * 31) + character);
            }

            var tag = new char[7];
            for (var i = 0; i < 6; i++)
            {
                tag[i] = (char)('A' + (hash % 26));
                hash /= 26;
            }

            tag[6] = '+';

            return new string(tag);
        }

        private ArrayToken GetWidths()
        {
            var scaling = 1000.0 / font.TableRegister.HeaderTable.UnitsPerEm;
            var widths = new IToken[characters.Count];

            for (var i = 0; i < characters.Count; i++)
            {
                TryGetAdvanceWidth(characters[i], out var width);
                widths[i] = new NumericToken(Math.Round(width * scaling, 2));
            }

            return new ArrayToken(widths);
        }

        /// <summary>
        /// PDF/A-1 requires a CIDSet for CID font subsets. With the Identity CIDToGIDMap the CIDs of the glyphs in the subset
        /// are their glyph indices, including the glyphs added for the components of composite glyphs.
        /// </summary>
        private static StreamToken CreateCidSet(int glyphCount)
        {
            var bits = new byte[(glyphCount + 7) / 8];
            for (var cid = 0; cid < glyphCount; cid++)
            {
                bits[cid / 8] |= (byte)(0x80 >> (cid % 8));
            }

            return DataCompressor.CompressToStream(bits);
        }

        private DictionaryToken CreateFontDescriptor(NameToken fontName, IndirectReferenceToken fontFile, IndirectReferenceToken cidSet)
        {
            var os2 = font.TableRegister.Os2Table ?? throw new InvalidFontFormatException("Embedding TrueType font requires OS/2 table.");
            var postScript = font.TableRegister.PostScriptTable ?? throw new InvalidFontFormatException("Embedding TrueType font requires post table.");
            var os2Version2 = os2 as Os2Version2To4OpenTypeTable;
            var horizontalHeader = font.TableRegister.HorizontalHeaderTable;
            var bounds = font.TableRegister.HeaderTable.Bounds;
            var scaling = 1000.0 / font.TableRegister.HeaderTable.UnitsPerEm;

            var dictionary = new Dictionary<NameToken, IToken>
            {
                { NameToken.Type, NameToken.FontDescriptor },
                { NameToken.FontName, fontName },
                { NameToken.Flags, new NumericToken((int)FontDescriptorFlags.Symbolic) },
                { NameToken.FontBbox, new ArrayToken(new IToken[] { Scale(bounds.Left), Scale(bounds.Bottom), Scale(bounds.Right), Scale(bounds.Top) }) },
                { NameToken.ItalicAngle, new NumericToken(postScript.ItalicAngle) },
                { NameToken.Ascent, Scale(horizontalHeader.Ascent) },
                { NameToken.Descent, Scale(horizontalHeader.Descent) },
                { NameToken.CapHeight, Scale(os2Version2?.CapHeight ?? horizontalHeader.Ascent) },
                { NameToken.StemV, Scale(bounds.Width * 0.13) },
                { NameToken.FontFile2, fontFile },
                { NameToken.CidSet, cidSet }
            };

            if (os2Version2 is not null)
            {
                dictionary[NameToken.Xheight] = Scale(os2Version2.XHeight);
            }

            return new DictionaryToken(dictionary);

            NumericToken Scale(double value) => new NumericToken(Math.Round(value * scaling, 2));
        }
    }
}