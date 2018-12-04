namespace UglyToad.PdfPig.Writer
{
    using System.Collections.Generic;
    using System.IO;
    using Fonts;
    using Fonts.TrueType;
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
            var embeddedFile = new StreamToken(new DictionaryToken(new Dictionary<IToken, IToken>
            {
                { NameToken.Length, new NumericToken(bytes.Count) }
            }), bytes);

            var fileRef = context.WriteObject(outputStream, embeddedFile);

            var baseFont = NameToken.Create(font.TableRegister.NameTable.FontName);

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

            var dictionary = new Dictionary<IToken, IToken>
            {
                { NameToken.Type, NameToken.Font },
                { NameToken.Subtype, NameToken.TrueType },
                { NameToken.BaseFont, baseFont },
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