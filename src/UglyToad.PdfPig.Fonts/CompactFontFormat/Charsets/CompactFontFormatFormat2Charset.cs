namespace UglyToad.PdfPig.Fonts.CompactFontFormat.Charsets
{
    using System;

    /// <summary>
    /// A Charset from a Compact Font Format font file best for fonts with a large number of well ordered string ids.
    /// </summary>
    internal class CompactFontFormatFormat2Charset : CompactFontFormatCharset
    {
        public CompactFontFormatFormat2Charset(ReadOnlySpan<(int glyphId, int stringId, string name)> data)
            : base(data)
        {
        }
    }
}