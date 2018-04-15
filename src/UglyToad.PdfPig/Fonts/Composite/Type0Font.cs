namespace UglyToad.PdfPig.Fonts.Composite
{
    using System;
    using CidFonts;
    using Cmap;
    using Core;
    using Geometry;
    using IO;
    using Tokenization.Tokens;
    using Util.JetBrains.Annotations;

    /// <summary>
    /// Defines glyphs using a CIDFont
    /// </summary>
    internal class Type0Font : IFont
    {
        public NameToken Name => BaseFont;

        [NotNull]
        public NameToken BaseFont { get; }

        [NotNull]
        public ICidFont CidFont { get; }

        [NotNull]
        public CMap CMap { get; }

        [NotNull]
        public ToUnicodeCMap ToUnicode { get; }

        public bool IsVertical => CMap.WritingMode == WritingMode.Vertical;

        private readonly TransformationMatrix fontMatrix = TransformationMatrix.FromValues(0.001m, 0, 0, 0.001m, 0, 0);

        public Type0Font(NameToken baseFont, ICidFont cidFont, CMap cmap, CMap toUnicodeCMap)
        {
            BaseFont = baseFont ?? throw new ArgumentNullException(nameof(baseFont));
            CidFont = cidFont ?? throw new ArgumentNullException(nameof(cidFont));
            CMap = cmap ?? throw new ArgumentNullException(nameof(cmap));
            ToUnicode = new ToUnicodeCMap(toUnicodeCMap);
        }

        public int ReadCharacterCode(IInputBytes bytes, out int codeLength)
        {
            var current = bytes.CurrentOffset;

            var code = CMap.ReadCode(bytes);

            codeLength = (int)(bytes.CurrentOffset - current);

            return code;
        }

        public bool TryGetUnicode(int characterCode, out string value)
        {
            value = null;

            if (!ToUnicode.CanMapToUnicode)
            {
                return false;
            }

            // According to PdfBox certain providers incorrectly using Identity CMaps as ToUnicode.
            if (ToUnicode.IsUsingIdentityAsUnicodeMap)
            {
                value = new string((char)characterCode, 1);

                return true;
            }

            return ToUnicode.TryGet(characterCode, out value);
        }

        public PdfRectangle GetBoundingBox(int characterCode)
        {
            return fontMatrix.Transform(GetBoundingBoxInGlyphSpace(characterCode));
        }

        public decimal GetWidth(int characterCode)
        {
            var cid = CMap.ConvertToCid(characterCode);

            var fromFont = CidFont.GetWidthFromDictionary(cid);

            return fromFont;
        }

        public PdfRectangle GetBoundingBoxInGlyphSpace(int characterCode)
        {
            var cid = CMap.ConvertToCid(characterCode);

            return CidFont.GetBoundingBox(cid);
        }

        public TransformationMatrix GetFontMatrix()
        {
            return CidFont.FontMatrix;
        }
    }
}
