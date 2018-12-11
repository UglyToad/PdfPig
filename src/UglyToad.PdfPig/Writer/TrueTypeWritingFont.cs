namespace UglyToad.PdfPig.Writer
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Fonts;
    using Fonts.Encodings;
    using Fonts.Exceptions;
    using Fonts.TrueType;
    using Fonts.TrueType.Tables;
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

            public CharacterCodeToGlyphIdMapper(TrueTypeFontProgram font)
            {
                this.font = font ?? throw new ArgumentNullException(nameof(font));
            }

            public FontDictionaryMetrics GetMetrics()
            {
                var encoding = ReadFontEncoding();
                var firstCharacter = encoding.CodeToNameMap.Keys.Min();
                var lastCharacter = encoding.CodeToNameMap.Keys.Max();

                var glyphList = GlyphList.AdobeGlyphList;

                var length = lastCharacter - firstCharacter + 1;
                var widths = Enumerable.Range(0, length).Select(x => new NumericToken(0)).ToList();

                foreach (var pair in encoding.CodeToNameMap)
                {
                    var unicode = glyphList.NameToUnicode(pair.Value);
                    if (unicode == null)
                    {
                        continue;
                    }

                    var characterCode = (int) unicode[0];
                    if (!font.TryGetBoundingAdvancedWidth(characterCode, out var width))
                    {
                        throw new InvalidFontFormatException();
                    }

                    widths[pair.Key - firstCharacter] = new NumericToken(width);
                }

                return new FontDictionaryMetrics
                {
                    FirstChar = new NumericToken(firstCharacter),
                    LastChar = new NumericToken(lastCharacter),
                    Widths = new ArrayToken(widths)
                };
            }

            private Encoding ReadFontEncoding()
            {
                var codeToName = new Dictionary<int, string>();
                var postscript = font.TableRegister.PostScriptTable;
                for (int i = 0; i <= 256; i++)
                {
                    if (!font.TableRegister.CMapTable.TryGetGlyphIndex(i, out var glyphIndex))
                    {
                        continue;
                    }

                    codeToName[i] = postscript.GlyphNames[glyphIndex];
                }

                return new BuiltInEncoding(codeToName);
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