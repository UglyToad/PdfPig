namespace UglyToad.PdfPig.Fonts.Type1
{
    using System.Collections.Generic;
    using Parser;

    internal class Type1PrivateDictionary
    {
        public class Builder
        {
            public object Rd { get; set; }

            public object NoAccessPut { get; set; }

            public object NoAccessDef { get; set; }

            public IReadOnlyList<Type1CharstringDecryptedBytes> Subroutines { get; set; }

            public object[] OtherSubroutines { get; set; }

            public int? UniqueId { get; set; }

            public IReadOnlyList<int> BlueValues { get; set; }

            public IReadOnlyList<int> OtherBlues { get; set; }

            public IReadOnlyList<int> FamilyBlues { get; set; }

            public IReadOnlyList<int> FamilyOtherBlues { get; set; }

            public decimal? BlueScale { get; set; }

            public int? BlueShift { get; set; }

            public int? BlueFuzz { get; set; }

            public decimal? StandardHorizontalWidth { get; set; }

            public decimal? StandardVerticalWidth { get; set; }

            public IReadOnlyList<decimal> StempSnapHorizontalWidths { get; set; }

            public IReadOnlyList<decimal> StemSnapVerticalWidths { get; set; }

            public bool ForceBold { get; set; }

            public int LanguageGroup { get; set; }

            public int? Password { get; set; }

            public int LenIv { get; set; }

            public MinFeature MinFeature { get; set; }

            public bool RoundStemUp { get; set; }

            public decimal? ExpansionFactor { get; set; }
        }

        public class MinFeature
        {
            public int First { get; }

            public int Second { get; }

            public MinFeature(int first, int second)
            {
                First = first;
                Second = second;
            }
        }
    }
}
