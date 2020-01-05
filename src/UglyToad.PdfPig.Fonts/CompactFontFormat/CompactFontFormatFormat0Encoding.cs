namespace UglyToad.PdfPig.Fonts.CompactFontFormat
{
    using System.Collections.Generic;

    internal class CompactFontFormatFormat0Encoding : CompactFontFormatBuiltInEncoding
    {
        public CompactFontFormatFormat0Encoding(IReadOnlyList<(int code, int sid, string str)> values,
            IReadOnlyList<Supplement> supplements) : base(supplements)
        {
            Add(0, 0, ".notdef");

            foreach (var value in values)
            {
                Add(value.code, value.sid, value.str);
            }
        }
    }
}