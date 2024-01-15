namespace UglyToad.PdfPig.Fonts.CompactFontFormat
{
    using System.Collections.Generic;

    internal sealed class CompactFontFormatFormat1Encoding : CompactFontFormatBuiltInEncoding
    {
        public int NumberOfRanges { get; set; }

        public CompactFontFormatFormat1Encoding(int numberOfRanges, IReadOnlyList<(int code, int sid, string str)> values, IReadOnlyList<Supplement> supplements) : base(supplements)
        {
            NumberOfRanges = numberOfRanges;

            Add(0, 0, NotDefined);

            foreach (var value in values)
            {
                Add(value.code, value.sid, value.str);
            }
        }
    }
}