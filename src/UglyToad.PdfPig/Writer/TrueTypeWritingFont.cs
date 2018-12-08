namespace UglyToad.PdfPig.Writer
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Fonts;
    using Fonts.Exceptions;
    using Fonts.TrueType;
    using Fonts.TrueType.Tables;
    using Fonts.TrueType.Tables.CMapSubTables;
    using Geometry;
    using Tokens;

    internal class TrueTypeWritingFont : IWritingFont
    {
        private readonly TrueTypeFontProgram font;
        private readonly IReadOnlyList<byte> fontFileBytes;

        public bool HasWidths { get; } = true;

        public string Name => font.Name;

        public TrueTypeWritingFont(TrueTypeFontProgram font, IReadOnlyList<byte> fontFileBytes)
        {
            this.font = font;
            this.fontFileBytes = fontFileBytes;
        }

        public bool TryGetBoundingBox(char character, out PdfRectangle boundingBox)
        {
            return font.TryGetBoundingBox(character, out boundingBox);
        }

        public bool TryGetAdvanceWidth(char character, out decimal width)
        {
            return font.TryGetBoundingAdvancedWidth(character, out width);
        }

        public ObjectToken WriteFont(NameToken fontKeyName, Stream outputStream, BuilderContext context)
        {
            var bytes = fontFileBytes;
            var embeddedFile = new StreamToken(new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                { NameToken.Length, new NumericToken(bytes.Count) }
            }), bytes);

            var fileRef = context.WriteObject(outputStream, embeddedFile);

            var baseFont = NameToken.Create(font.TableRegister.NameTable.GetPostscriptName());

            var charCodeToGlyphId = new CharacterCodeToGlyphIdMapper(font);

            var postscript = font.TableRegister.PostScriptTable;
            var hhead = font.TableRegister.HorizontalHeaderTable;

            var bbox = font.TableRegister.HeaderTable.Bounds;

            var scaling = 1000m / font.TableRegister.HeaderTable.UnitsPerEm;
            var descriptorDictionary = new Dictionary<NameToken, IToken>
            {
                { NameToken.Type, NameToken.FontDescriptor },
                { NameToken.FontName, baseFont },
                // TODO: get flags TrueTypeEmbedder.java
                { NameToken.Flags, new NumericToken((int)FontFlags.Symbolic) },
                { NameToken.FontBbox, GetBoundingBox(bbox, scaling) },
                { NameToken.ItalicAngle, new NumericToken(postscript.ItalicAngle) },
                { NameToken.Ascent, new NumericToken(hhead.Ascender * scaling) },
                { NameToken.Descent, new NumericToken(hhead.Descender * scaling) },
                { NameToken.CapHeight, new NumericToken(90) },
                { NameToken.StemV, new NumericToken(90) },
                { NameToken.FontFile2, new IndirectReferenceToken(fileRef.Number) }
            };

            var os2 = font.TableRegister.Os2Table;
            if (os2 == null)
            {
                throw new InvalidFontFormatException("Embedding TrueType font requires OS/2 table.");
            }
            
            if (os2 is Os2Version2To4OpenTypeTable twoPlus)
            {
                descriptorDictionary[NameToken.CapHeight] = new NumericToken(twoPlus.CapHeight);
                descriptorDictionary[NameToken.Xheight] = new NumericToken(twoPlus.XHeight);
            }

            descriptorDictionary[NameToken.StemV] = new NumericToken(bbox.Width * scaling * 0.13m);

            var metrics = charCodeToGlyphId.GetMetrics();

            var widthsRef = context.WriteObject(outputStream, metrics.Widths);

            var descriptor = context.WriteObject(outputStream, new DictionaryToken(descriptorDictionary));
            
            var dictionary = new Dictionary<NameToken, IToken>
            {
                { NameToken.Type, NameToken.Font },
                { NameToken.Subtype, NameToken.TrueType },
                { NameToken.BaseFont, baseFont },
                { NameToken.FontDescriptor, new IndirectReferenceToken(descriptor.Number) },
                { NameToken.FirstChar, metrics.FirstChar },
                { NameToken.LastChar, metrics.LastChar },
                { NameToken.Widths, new IndirectReferenceToken(widthsRef.Number) },
                { NameToken.Encoding, NameToken.MacRomanEncoding }
            };

            var token = new DictionaryToken(dictionary);

            var result = context.WriteObject(outputStream, token);

            return result;
        }

        private static ArrayToken GetBoundingBox(PdfRectangle boundingBox, decimal scaling)
        {
            return new ArrayToken(new[]
            {
                new NumericToken(boundingBox.Left * scaling),
                new NumericToken(boundingBox.Bottom * scaling),
                new NumericToken(boundingBox.Right * scaling),
                new NumericToken(boundingBox.Top * scaling)
            });
        }

        private class CharacterCodeToGlyphIdMapper
        {
            private readonly TrueTypeFontProgram font;
            private readonly ICMapSubTable cmapSubTable;

            public CharacterCodeToGlyphIdMapper(TrueTypeFontProgram font)
            {
                var microsoftUnicode = font.TableRegister.CMapTable.SubTables.FirstOrDefault(x => x.PlatformId == TrueTypeCMapPlatform.Windows && x.EncodingId == 1);
                cmapSubTable = microsoftUnicode ?? font.TableRegister.CMapTable.SubTables.FirstOrDefault(x => x.PlatformId == TrueTypeCMapPlatform.Macintosh && x.EncodingId == 0);
                this.font = font ?? throw new ArgumentNullException(nameof(font));
            }

            public FontDictionaryMetrics GetMetrics()
            {
                var widths = font.TableRegister.HorizontalMetricsTable.AdvancedWidths;

                var lastCharacter = 0;
                var fullWidths = new List<NumericToken>();
                switch (cmapSubTable)
                {
                    case Format4CMapTable format4:
                    {
                        var firstCharacter = format4.Segments[0].StartCode;
                        var gid = format4.CharacterCodeToGlyphIndex(firstCharacter);
                        // Include unmapped character codes except for .notdef
                        firstCharacter -= gid - 1;

                        var widthIndex = 0;
                        var lastSegment = default(Format4CMapTable.Segment?);

                        for (var i = 0; i < format4.Segments.Count; i++)
                        {
                            var segment = format4.Segments[i];

                            if (segment.StartCode + segment.IdDelta >= 0xFFF)
                            {
                                break;
                            }

                            if (lastSegment.HasValue)
                            {
                                var endGlyph = lastSegment.Value.EndCode + lastSegment.Value.IdDelta;
                                var startGlyph = segment.StartCode + segment.IdDelta;
                                var gap = startGlyph - endGlyph - 1;
                                for (int j = 0; j < gap; j++)
                                {
                                    fullWidths.Add(new NumericToken(0));
                                }
                            }

                            lastCharacter = segment.EndCode;

                            for (int j = 0; j < (segment.EndCode - segment.StartCode); j++)
                            {
                                var width = widths[widthIndex];
                                fullWidths.Add(new NumericToken(width));

                                widthIndex++;
                            }

                            lastSegment = segment;
                        }

                        return new FontDictionaryMetrics
                        {
                            Widths = new ArrayToken(fullWidths),
                            FirstChar = new NumericToken(firstCharacter),
                            LastChar = new NumericToken(lastCharacter)
                        };
                    }
                    case ByteEncodingCMapTable bytes:
                    default:
                        throw new NotSupportedException($"No dictionary mapping for format yet: {cmapSubTable.GetType().Name}.");
                }
            }
        }

        private class FontDictionaryMetrics
        {
            public ArrayToken Widths { get; set; }

            public NumericToken FirstChar { get; set; }

            public NumericToken LastChar { get; set; }
        }
    }
}