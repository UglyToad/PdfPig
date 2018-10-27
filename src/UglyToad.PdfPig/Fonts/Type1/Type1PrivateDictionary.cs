namespace UglyToad.PdfPig.Fonts.Type1
{
    using System.Collections.Generic;

    internal class Type1PrivateDictionary
    {
        public class Builder
        {
            public object Rd { get; set; }

            public object NoAccessPut { get; set; }

            public object NoAccessDef { get; set; }

            public object[] Subroutines { get; set; }

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

            public object[] MinFeature { get; set; }

            public bool RoundStemUp { get; set; }
        }
    }
}
