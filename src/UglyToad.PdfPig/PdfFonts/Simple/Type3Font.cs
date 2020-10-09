namespace UglyToad.PdfPig.PdfFonts.Simple
{
    using Cmap;
    using Composite;
    using Core;
    using Fonts;
    using Fonts.Encodings;
    using System.Collections.Generic;
    using Tokens;
    using UglyToad.PdfPig.Filters;
    using UglyToad.PdfPig.Tokenization.Scanner;

    internal class Type3Font : IFont
    {
        private readonly PdfRectangle boundingBox;
        private readonly TransformationMatrix fontMatrix;
        private readonly Encoding encoding;
        private readonly int firstChar;
        private readonly int lastChar;
        private readonly double[] widths;
        private readonly ToUnicodeCMap toUnicodeCMap;

        /// <summary>
        /// Type 3 fonts are usually unnamed.
        /// </summary>
        public NameToken Name { get; }

        public bool IsVertical { get; } = false;

        public FontDetails Details { get; }

        public Type3Font(NameToken name, PdfRectangle boundingBox, TransformationMatrix fontMatrix,
            Encoding encoding, int firstChar, int lastChar, double[] widths,
            CMap toUnicodeCMap)
        {
            Name = name;

            this.boundingBox = boundingBox;
            this.fontMatrix = fontMatrix;
            this.encoding = encoding;
            this.firstChar = firstChar;
            this.lastChar = lastChar;
            this.widths = widths;
            this.toUnicodeCMap = new ToUnicodeCMap(toUnicodeCMap);
            Details = FontDetails.GetDefault(name?.Data);
        }

        public int ReadCharacterCode(IInputBytes bytes, out int codeLength)
        {
            codeLength = 1;
            return bytes.CurrentByte;
        }

        public bool TryGetUnicode(int characterCode, out string value)
        {
            if (toUnicodeCMap.CanMapToUnicode)
            {
                return toUnicodeCMap.TryGet(characterCode, out value);
            }

            var name = encoding.GetName(characterCode);

            var listed = GlyphList.AdobeGlyphList.NameToUnicode(name);

            value = listed;

            return true;
        }

        public CharacterBoundingBox GetBoundingBox(int characterCode)
        {
            var characterBoundingBox = GetBoundingBoxInGlyphSpace(characterCode);

            characterBoundingBox = fontMatrix.Transform(characterBoundingBox);

            var width = fontMatrix.TransformX(widths[characterCode - firstChar]);

            return new CharacterBoundingBox(characterBoundingBox, width);
        }

        private PdfRectangle GetBoundingBoxInGlyphSpace(int characterCode)
        {
            if (characterCode < firstChar || characterCode > lastChar)
            {
                throw new InvalidFontFormatException($"The character code was not contained in the widths array: {characterCode}.");
            }

            return new PdfRectangle(0, 0, widths[characterCode - firstChar], 0);
        }

        public TransformationMatrix GetFontMatrix()
        {
            return fontMatrix;
        }

        public bool TryGetPath(int characterCode, out IReadOnlyList<PdfSubpath> path)
        {
            path = new List<PdfSubpath>();
            return false;
        }

        public bool TryGetDecodedFontBytes(IPdfTokenScanner pdfTokenScanner, IFilterProvider filterProvider, out IReadOnlyList<byte> bytes)
        {
            bytes = null;
            return false;
        }
    }
}
