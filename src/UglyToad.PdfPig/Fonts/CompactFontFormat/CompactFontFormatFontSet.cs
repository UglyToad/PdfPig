namespace UglyToad.PdfPig.Fonts.CompactFontFormat
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core;
    using Geometry;

    internal class CompactFontFormatFontSet
    {
        private readonly CompactFontFormatHeader header;
        private readonly IReadOnlyList<string> fontNames;
        private readonly IReadOnlyDictionary<string, CompactFontFormatFont> fontSet;

        public CompactFontFormatFontSet(CompactFontFormatHeader header, IReadOnlyList<string> fontNames, 
            IReadOnlyDictionary<string, CompactFontFormatFont> fontSet)
        {
            this.header = header;
            this.fontNames = fontNames;
            this.fontSet = fontSet;
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
            if (fontSet.Count > 1)
            {
                throw new NotSupportedException("Multiple fonts in a CFF");
            }
#endif
            return fontSet.First().Value;
        }
    }
}
