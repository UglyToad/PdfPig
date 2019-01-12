namespace UglyToad.PdfPig.Fonts.Encodings
{
    using System.Collections.Generic;
    using Util;

    internal class CompactFontFormatFormat0Encoding : CompactFontFormatBuiltInEncoding
    {
        public CompactFontFormatFormat0Encoding(IReadOnlyList<(int code, int sid, string str)> values) : base(EmptyArray<Supplement>.Instance)
        {
            Add(0, 0, ".notdef");

            foreach (var value in values)
            {
                Add(value.code, value.sid, value.str);
            }
        }
    }
}