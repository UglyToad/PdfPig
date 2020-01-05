namespace UglyToad.PdfPig.Fonts.CompactFontFormat
{
    using System.Collections.Generic;

    internal abstract class CompactFontFormatBuiltInEncoding : CompactFontFormatBaseEncoding
    {
        public IReadOnlyList<Supplement> Supplements { get; }

        protected CompactFontFormatBuiltInEncoding(IReadOnlyList<Supplement> supplements)
        {
            Supplements = supplements;
            foreach (var supplement in supplements)
            {
                Add(supplement.Code, supplement.Sid, supplement.Name);
            }
        }

        public class Supplement
        {
            public int Code { get; }

            public int Sid { get; }

            public string Name { get; }

            public Supplement(int code, int sid, string name)
            {
                Code = code;
                Sid = sid;
                Name = name;
            }
        }
    }
}