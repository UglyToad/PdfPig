namespace UglyToad.PdfPig.Fonts.CompactFontFormat.Charsets
{
    using System;

    /// <summary>
    /// A Charset from a Compact Font Format font file best for fonts with well ordered string ids.
    /// </summary>
    internal sealed class CompactFontFormatFormat1Charset : CompactFontFormatCharset
    {
        public CompactFontFormatFormat1Charset(ReadOnlySpan<(int glyphId, int stringId, string name)> data)
            : base(data)
        {
        }
    }
}