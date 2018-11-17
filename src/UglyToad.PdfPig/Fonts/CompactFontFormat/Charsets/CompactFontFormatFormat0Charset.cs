namespace UglyToad.PdfPig.Fonts.CompactFontFormat.Charsets
{
    using System.Collections.Generic;

    /// <summary>
    /// A Charset from a Compact Font Format font file best for fonts with relatively unordered string ids.
    /// </summary>
    internal class CompactFontFormatFormat0Charset : CompactFontFormatCharset
    {
        public CompactFontFormatFormat0Charset(IReadOnlyList<(int glyphId, int stringId, string name)> data)
        :base(data)
        {
        }
    }
}
