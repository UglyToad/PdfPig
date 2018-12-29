namespace UglyToad.PdfPig.Fonts.CompactFontFormat.Charsets
{
    using System;

    /// <summary>
    /// An empty Charset for CID fonts which map from Character Id to Glyph Id without using strings.
    /// </summary>
    internal class CompactFontFormatEmptyCharset : ICompactFontFormatCharset
    {
        private readonly int numberOfCharstrings;

        public bool IsCidCharset { get; } = true;

        public CompactFontFormatEmptyCharset(int numberOfCharstrings)
        {
            this.numberOfCharstrings = numberOfCharstrings;
        }

        public string GetNameByGlyphId(int glyphId)
        {
            throw new NotSupportedException("Cid Charsets do not support named glyphs.");
        }

        public string GetNameByStringId(int stringId)
        {
            throw new NotSupportedException("Cid Charsets do not support named glyphs.");
        }

        public string GetStringIdByGlyphId(int glyphId)
        {
            throw new NotSupportedException("Cid Charsets do not support named glyphs.");
        }
    }
}