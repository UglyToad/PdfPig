namespace UglyToad.PdfPig.Fonts.CompactFontFormat.Charsets
{
    using System;

    /// <summary>
    /// A Charset from a Compact Font Format font file best for fonts with relatively unordered string ids.
    /// </summary>
    internal sealed class CompactFontFormatFormat0Charset : CompactFontFormatCharset
    {
        public CompactFontFormatFormat0Charset(ReadOnlySpan<(int glyphId, int stringId, string name)> data)
            : base(data)
        {
        }
    }
}
