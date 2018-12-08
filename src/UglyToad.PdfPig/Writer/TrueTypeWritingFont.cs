namespace UglyToad.PdfPig.Writer
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Fonts;
    using Fonts.Exceptions;
    using Fonts.TrueType;
    using Fonts.TrueType.Tables;
    using Geometry;
    using Tokens;

    internal class TrueTypeWritingFont : IWritingFont
    {
        private readonly TrueTypeFontProgram font;
        private readonly IReadOnlyList<byte> fontFileBytes;

        public TrueTypeWritingFont(TrueTypeFontProgram font, IReadOnlyList<byte> fontFileBytes)
        {
            this.font = font;
            this.fontFileBytes = fontFileBytes;
        }

        public bool HasWidths { get; } = true;

        public bool TryGetBoundingBox(char character, out PdfRectangle boundingBox)
        {
            return font.TryGetBoundingBox(character, out boundingBox);
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
                // TODO: cap, x height, stem v
                { NameToken.CapHeight, new NumericToken(90) },
                { NameToken.StemV, new NumericToken(90) },
                // TODO: font file 2
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

            var widths = font.TableRegister.GlyphTable.Glyphs.Select(x => new NumericToken(x.Bounds.Width)).ToArray();

            var widthsRef = context.WriteObject(outputStream, new ArrayToken(widths));

            var descriptor = context.WriteObject(outputStream, new DictionaryToken(descriptorDictionary));

            var dictionary = new Dictionary<NameToken, IToken>
            {
                { NameToken.Type, NameToken.Font },
                { NameToken.Subtype, NameToken.TrueType },
                { NameToken.BaseFont, baseFont },
                { NameToken.FontDescriptor, new IndirectReferenceToken(descriptor.Number) },
                { NameToken.FirstChar, new NumericToken(0) },
                { NameToken.LastChar, new NumericToken(font.TableRegister.GlyphTable.Glyphs.Count - 1) },
                { NameToken.Widths, new IndirectReferenceToken(widthsRef.Number) },
                { NameToken.Encoding, NameToken.WinAnsiEncoding }
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
    }
}