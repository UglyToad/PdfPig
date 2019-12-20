namespace UglyToad.PdfPig.Fonts.CompactFontFormat
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CidFonts;
    using Core;
    using Geometry;
    using Util.JetBrains.Annotations;

    /// <summary>
    /// A Compact Font Format (CFF) font program as described in The Compact Font Format specification (Adobe Technical Note #5176).
    /// A CFF font may contain multiple fonts and achieves compression by sharing details between fonts in the set.
    /// </summary>
    internal class CompactFontFormatFontProgram : ICidFontProgram
    {
        /// <summary>
        /// The decoded header table for this font.
        /// </summary>
        public CompactFontFormatHeader Header { get; }

        /// <summary>
        /// The individual fonts contained in this font keyed by name.
        /// </summary>
        [NotNull]
        public IReadOnlyDictionary<string, CompactFontFormatFont> Fonts { get; }

        /// <summary>
        /// Create a new <see cref="CompactFontFormatFontProgram"/>.
        /// </summary>
        /// <param name="header">The header table for the font.</param>
        /// <param name="fontSet">The fonts in this font program.</param>
        public CompactFontFormatFontProgram(CompactFontFormatHeader header, [NotNull] IReadOnlyDictionary<string, CompactFontFormatFont> fontSet)
        {
            Header = header;
            Fonts = fontSet ?? throw new ArgumentNullException(nameof(fontSet));
        }

        public TransformationMatrix GetFontTransformationMatrix()
        {
            var result = GetFont().TopDictionary.FontMatrix;
            return result;
        }

        public PdfRectangle? GetCharacterBoundingBox(string characterName)
        {
            var font = GetFont();
            return font.GetCharacterBoundingBox(characterName);
        }

        private CompactFontFormatFont GetFont()
        {
#if DEBUG
            // TODO: what to do if there are multiple fonts?
            if (Fonts.Count > 1)
            {
                throw new NotSupportedException("Multiple fonts in a CFF");
            }
#endif
            return Fonts.First().Value;
        }

        public bool TryGetBoundingBox(int characterIdentifier, out PdfRectangle boundingBox)
        {
            var font = GetFont();

            var characterName = GetCharacterName(characterIdentifier);

            boundingBox = font.GetCharacterBoundingBox(characterName) ?? new PdfRectangle(0, 0, 500, 0);

            return true;
        }

        public bool TryGetBoundingBox(int characterIdentifier, Func<int, int?> characterCodeToGlyphId, out PdfRectangle boundingBox)
        {
            throw new NotImplementedException();
        }

        public bool TryGetBoundingAdvancedWidth(int characterIdentifier, Func<int, int?> characterCodeToGlyphId, out decimal width)
        {
            throw new NotImplementedException();
        }

        public bool TryGetBoundingAdvancedWidth(int characterIdentifier, out decimal width)
        {
            throw new NotImplementedException();
        }

        public int GetFontMatrixMultiplier()
        {
            return 1000;
        }

        public string GetCharacterName(int characterCode)
        {
            var font = GetFont();

            if (font.Encoding != null)
            {
                return font.Encoding.GetName(characterCode);
            }

            return ".notdef";
        }
    }
}
