namespace UglyToad.PdfPig.PdfFonts.Encodings
{
    using System.Collections.Generic;

    internal class CompactFontFormatFormat1Encoding : CompactFontFormatBuiltInEncoding
    {
        public int NumberOfRanges { get; set; }

        public CompactFontFormatFormat1Encoding(int numberOfRanges, IReadOnlyList<(int code, int sid, string str)> values, IReadOnlyList<Supplement> supplements) : base(supplements)
        {
            NumberOfRanges = numberOfRanges;

            Add(0, 0, ".notdef");

            foreach (var value in values)
            {
                Add(value.code, value.sid, value.str);
            }
        }
    }
}