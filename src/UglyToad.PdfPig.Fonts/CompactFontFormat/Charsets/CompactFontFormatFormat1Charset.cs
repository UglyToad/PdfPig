namespace UglyToad.PdfPig.Fonts.CompactFontFormat.Charsets
{
    using System.Collections.Generic;

    /// <summary>
    /// A Charset from a Compact Font Format font file best for fonts with well ordered string ids.
    /// </summary>
    internal class CompactFontFormatFormat1Charset : CompactFontFormatCharset
    {
        public CompactFontFormatFormat1Charset(IReadOnlyList<(int glyphId, int stringId, string name)> data)
            : base(data)
        {
        }
    }
}