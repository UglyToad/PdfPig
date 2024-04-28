namespace UglyToad.PdfPig.Fonts.CompactFontFormat
{
    using System;
    using System.Collections.Generic;

    internal sealed class CompactFontFormatFormat0Encoding : CompactFontFormatBuiltInEncoding
    {
        public CompactFontFormatFormat0Encoding(
            ReadOnlySpan<(int code, int sid, string str)> values,
            IReadOnlyList<Supplement> supplements) : base(supplements)
        {
            Add(0, 0, NotDefined);

            foreach (var value in values)
            {
                Add(value.code, value.sid, value.str);
            }
        }
    }
}