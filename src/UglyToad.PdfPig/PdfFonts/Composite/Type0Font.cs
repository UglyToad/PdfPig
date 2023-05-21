namespace UglyToad.PdfPig.PdfFonts.Composite
{
    using CidFonts;
    using Cmap;
    using Core;
    using Geometry;
    using System;
    using System.Collections.Generic;
    using Tokens;
    using Util.JetBrains.Annotations;
    using Debug = System.Diagnostics.Debug;

    /// <summary>
    /// Defines glyphs using a CIDFont
    /// </summary>
    internal class Type0Font : IFont, IVerticalWritingSupported
    {
        private readonly CMap ucs2CMap;
        // ReSharper disable once NotAccessedField.Local
        private readonly bool isChineseJapaneseOrKorean;
        private readonly Dictionary<int, CharacterBoundingBox> boundingBoxCache
            = new Dictionary<int, CharacterBoundingBox>();

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

        public FontDetails Details { get; }

        public Type0Font(NameToken baseFont, ICidFont cidFont, CMap cmap, CMap toUnicodeCMap,
            CMap ucs2CMap,
            bool isChineseJapaneseOrKorean)
        {
            this.ucs2CMap = ucs2CMap;
            this.isChineseJapaneseOrKorean = isChineseJapaneseOrKorean;

            BaseFont = baseFont ?? throw new ArgumentNullException(nameof(baseFont));
            CidFont = cidFont ?? throw new ArgumentNullException(nameof(cidFont));
            CMap = cmap ?? throw new ArgumentNullException(nameof(cmap));
            ToUnicode = new ToUnicodeCMap(toUnicodeCMap);
            Details = cidFont.Details?.WithName(Name.Data)
                      ?? FontDetails.GetDefault(Name.Data);
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

            var HaveCMap = ToUnicode.CanMapToUnicode;
            if (HaveCMap == false)
            {
                var HaveUnicode2CMap = (ucs2CMap is null == false);
                if (HaveUnicode2CMap)
                {
                    // Have both ucs2Map and CMap convert to unicode by
                    // characterCode  ----by CMAP---> CID ---ucs2Map---> Unicode
                    var CID = CMap.ConvertToCid(characterCode);
                    if (CID == 0)
                    {
                        Debug.WriteLine($"Warning: No mapping from characterCode (0x{characterCode:X} to CID by ucs2Map.");
                        return false; // No mapping from characterCode to CID.
                    }
                    // CID ---ucs2Map---> Unicode
                    if (ucs2CMap.TryConvertToUnicode(CID, out value))
                    {
                        return value != null;
                    }
                }
                if (HaveUnicode2CMap) // 2022-12-24 @fnatzke left as fall-back. Possible?
                {
                    // characterCode ---ucs2Map---> Unicode      (?) @fnatzke possible?
                    if (ucs2CMap.TryConvertToUnicode(characterCode, out value))
                    {
                        return value != null;
                    }
                }
            }

            // According to PdfBox certain providers incorrectly using Identity CMaps as ToUnicode.
            if (ToUnicode.IsUsingIdentityAsUnicodeMap)
            {
                value = new string((char)characterCode, 1);

                return true;
            }

            return ToUnicode.TryGet(characterCode, out value);
        }

        public CharacterBoundingBox GetBoundingBox(int characterCode)
        {
            if (boundingBoxCache.TryGetValue(characterCode, out var cached))
            {
                return cached;
            }

            var matrix = GetFontMatrix();

            var boundingBox = GetBoundingBoxInGlyphSpace(characterCode);

            boundingBox = matrix.Transform(boundingBox);

            var characterIdentifier = CMap.ConvertToCid(characterCode);

            var width = CidFont.GetWidthFromFont(characterIdentifier);

            var advanceWidth = matrix.TransformX(width);

            var result = new CharacterBoundingBox(boundingBox, advanceWidth);

            boundingBoxCache[characterCode] = result;

            return result;
        }

        public PdfRectangle GetBoundingBoxInGlyphSpace(int characterCode)
        {
            var characterIdentifier = CMap.ConvertToCid(characterCode);

            return CidFont.GetBoundingBox(characterIdentifier);
        }

        public TransformationMatrix GetFontMatrix()
        {
            return CidFont.FontMatrix;
        }

        public PdfVector GetPositionVector(int characterCode)
        {
            var characterIdentifier = CMap.ConvertToCid(characterCode);

            return CidFont.GetPositionVector(characterIdentifier).Scale(-1 / 1000.0);
        }

        public PdfVector GetDisplacementVector(int characterCode)
        {
            var characterIdentifier = CMap.ConvertToCid(characterCode);

            return CidFont.GetDisplacementVector(characterIdentifier).Scale(1 / 1000.0);
        }

        /// <inheritdoc/>
        public bool TryGetPath(int characterCode, out IReadOnlyList<PdfSubpath> path)
        {
            return CidFont.TryGetPath(characterCode, out path);
        }

        /// <inheritdoc/>
        public bool TryGetNormalisedPath(int characterCode, out IReadOnlyList<PdfSubpath> path)
        {
            return CidFont.TryGetNormalisedPath(characterCode, out path);
        }
    }
}
