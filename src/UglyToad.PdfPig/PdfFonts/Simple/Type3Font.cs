namespace UglyToad.PdfPig.PdfFonts.Simple
{    
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using Cmap;
    using Composite;
    using Core;
    using Fonts;
    using Fonts.Encodings;
    using Tokens;

    internal class Type3Font : IType3Font
    {
        private readonly PdfRectangle boundingBox;
        private readonly TransformationMatrix fontMatrix;
        private readonly double ascent;
        private readonly double descent;
        private readonly Encoding encoding;
        private readonly int firstChar;
        private readonly int lastChar;
        private readonly double[] widths;
        private readonly ToUnicodeCMap toUnicodeCMap;
        private readonly IReadOnlyDictionary<string, StreamToken>? charProcs;
        private readonly Dictionary<int, CharacterBoundingBox> boundingBoxCache = new();

        /// <summary>
        /// Type 3 fonts are usually unnamed.
        /// </summary>
        public NameToken Name { get; }

        public bool IsVertical { get; } = false;

        public FontDetails Details { get; }

        /// <inheritdoc/>
        public DictionaryToken? Type3Resources { get; }

        public Type3Font(NameToken name, PdfRectangle boundingBox, TransformationMatrix fontMatrix,
            Encoding encoding, int firstChar, int lastChar, double[] widths,
            CMap toUnicodeCMap, IReadOnlyDictionary<string, StreamToken>? charProcs,
            DictionaryToken? resources)
        {
            Name = name;

            this.boundingBox = boundingBox;
            this.fontMatrix = fontMatrix;
            this.encoding = encoding;
            this.firstChar = firstChar;
            this.lastChar = lastChar;
            this.widths = widths;
            this.toUnicodeCMap = new ToUnicodeCMap(toUnicodeCMap);
            this.charProcs = charProcs;
            Type3Resources = resources;
            Details = FontDetails.GetDefault(name?.Data);

            // Assumption is ZapfDingbats is not possible here. We need to change the behaviour if not the case
            System.Diagnostics.Debug.Assert(!(encoding is ZapfDingbatsEncoding || Details.Name.Contains("ZapfDingbats")));
            descent = ComputeDescent();
            ascent = ComputeAscent();
        }

        private double ComputeDescent()
        {
            return 0;
        }

        private double ComputeAscent()
        {
            return fontMatrix.TransformY(boundingBox.Top);
        }

        public int ReadCharacterCode(IInputBytes bytes, out int codeLength)
        {
            codeLength = 1;
            return bytes.CurrentByte;
        }

        public bool TryGetUnicode(int characterCode, [NotNullWhen(true)] out string? value)
        {
            value = null;

            if (toUnicodeCMap.CanMapToUnicode && toUnicodeCMap.TryGet(characterCode, out value))
            {
                return true;
            }

            if (encoding is null)
            {
                return false;
            }

            try
            {
                var name = encoding.GetName(characterCode);
                value = GlyphList.AdobeGlyphList.NameToUnicode(name);
            }
            catch
            {
                return false;
            }

            return value is not null;
        }

        private double GetWidth(int characterCode)
        {
            var widthIndex = characterCode - firstChar;

            if (widthIndex >= 0 && widthIndex < widths.Length)
            {
                return widths[widthIndex];
            }
            
            return boundingBox.Width;
        }

        public CharacterBoundingBox GetBoundingBox(int characterCode)
        {
            if (boundingBoxCache.TryGetValue(characterCode, out var cached))
            {
                return cached;
            }

            var characterBoundingBox = GetBoundingBoxInGlyphSpace(characterCode);

            characterBoundingBox = fontMatrix.Transform(characterBoundingBox);

            var width = fontMatrix.TransformX(GetWidth(characterCode));

            var result = new CharacterBoundingBox(characterBoundingBox, width);
            boundingBoxCache[characterCode] = result;
            return result;
        }

        private PdfRectangle GetBoundingBoxInGlyphSpace(int characterCode)
        {
            if (characterCode < firstChar || characterCode > lastChar)
            {
                throw new InvalidFontFormatException($"The character code was not contained in the widths array: {characterCode}.");
            }

            // The CharProc's d1 operator would declare a precise per-glyph
            // bbox, but parsing it requires running the CharProc stream which
            // this class does not do. Use the font-level FontBBox as the
            // upper bound for any glyph in this font.
            double width = GetWidth(characterCode);

            double left = boundingBox.Left;
            double right = boundingBox.Right;

            // If the advance width exceeds the FontBBox right edge (common
            // for d0 charprocs that only declare an advance), widen the
            // rectangle so paint up to the advance edge is covered.
            if (width > right)
            {
                right = width;
            }

            if (left > 0)
            {
                left = 0;
            }

            return new PdfRectangle(left, boundingBox.Bottom, right, boundingBox.Top);
        }

        public TransformationMatrix GetFontMatrix()
        {
            return fontMatrix;
        }

        public double GetDescent()
        {
            return descent;
        }

        public double GetAscent()
        {
            return ascent;
        }

        /// <summary>
        /// <inheritdoc/>
        /// <para>Type 3 fonts do not use vector paths. Always returns <c>false</c>.</para>
        /// </summary>
        public bool TryGetPath(int characterCode, [NotNullWhen(true)] out IReadOnlyList<PdfSubpath>? path)
        {
            path = null;
            return false;
        }

        /// <inheritdoc/>
        public bool TryGetCharProc(int characterCode, [NotNullWhen(true)] out StreamToken? charProcStream)
        {
            charProcStream = null;
            if (charProcs is null || encoding is null)
            {
                return false;
            }

            string name;
            try
            {
                name = encoding.GetName(characterCode);
            }
            catch
            {
                return false;
            }

            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            return charProcs.TryGetValue(name, out charProcStream);
        }

        /// <summary>
        /// <inheritdoc/>
        /// <para>Type 3 fonts do not use vector paths. Always returns <c>false</c>.</para>
        /// </summary>
        public bool TryGetNormalisedPath(int characterCode, [NotNullWhen(true)] out IReadOnlyList<PdfSubpath>? path)
        {
            return TryGetPath(characterCode, out path);
        }
    }
}
