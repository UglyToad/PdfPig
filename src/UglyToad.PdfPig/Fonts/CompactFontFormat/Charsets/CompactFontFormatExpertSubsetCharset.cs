namespace UglyToad.PdfPig.Fonts.CompactFontFormat.Charsets
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// A predefined Charset for a Compact Font Format font with Charset Id of 2.
    /// </summary>
    internal class CompactFontFormatExpertSubsetCharset : ICompactFontFormatCharset
    {
        private static readonly IReadOnlyDictionary<int, string> StringIdToName = new Dictionary<int, string>
        {
            {0, ".notdef"},
            {1, "space"},
            {231, "dollaroldstyle"},
            {232, "dollarsuperior"},
            {235, "parenleftsuperior"},
            {236, "parenrightsuperior"},
            {237, "twodotenleader"},
            {238, "onedotenleader"},
            {13, "comma"},
            {14, "hyphen"},
            {15, "period"},
            {99, "fraction"},
            {239, "zerooldstyle"},
            {240, "oneoldstyle"},
            {241, "twooldstyle"},
            {242, "threeoldstyle"},
            {243, "fouroldstyle"},
            {244, "fiveoldstyle"},
            {245, "sixoldstyle"},
            {246, "sevenoldstyle"},
            {247, "eightoldstyle"},
            {248, "nineoldstyle"},
            {27, "colon"},
            {28, "semicolon"},
            {249, "commasuperior"},
            {250, "threequartersemdash"},
            {251, "periodsuperior"},
            {253, "asuperior"},
            {254, "bsuperior"},
            {255, "centsuperior"},
            {256, "dsuperior"},
            {257, "esuperior"},
            {258, "isuperior"},
            {259, "lsuperior"},
            {260, "msuperior"},
            {261, "nsuperior"},
            {262, "osuperior"},
            {263, "rsuperior"},
            {264, "ssuperior"},
            {265, "tsuperior"},
            {266, "ff"},
            {109, "fi"},
            {110, "fl"},
            {267, "ffi"},
            {268, "ffl"},
            {269, "parenleftinferior"},
            {270, "parenrightinferior"},
            {272, "hyphensuperior"},
            {300, "colonmonetary"},
            {301, "onefitted"},
            {302, "rupiah"},
            {305, "centoldstyle"},
            {314, "figuredash"},
            {315, "hypheninferior"},
            {158, "onequarter"},
            {155, "onehalf"},
            {163, "threequarters"},
            {320, "oneeighth"},
            {321, "threeeighths"},
            {322, "fiveeighths"},
            {323, "seveneighths"},
            {324, "onethird"},
            {325, "twothirds"},
            {326, "zerosuperior"},
            {150, "onesuperior"},
            {164, "twosuperior"},
            {169, "threesuperior"},
            {327, "foursuperior"},
            {328, "fivesuperior"},
            {329, "sixsuperior"},
            {330, "sevensuperior"},
            {331, "eightsuperior"},
            {332, "ninesuperior"},
            {333, "zeroinferior"},
            {334, "oneinferior"},
            {335, "twoinferior"},
            {336, "threeinferior"},
            {337, "fourinferior"},
            {338, "fiveinferior"},
            {339, "sixinferior"},
            {340, "seveninferior"},
            {341, "eightinferior"},
            {342, "nineinferior"},
            {343, "centinferior"},
            {344, "dollarinferior"},
            {345, "periodinferior"},
            {346, "commainferior"}
        };

        public static CompactFontFormatExpertSubsetCharset Value { get; } = new CompactFontFormatExpertSubsetCharset();

        private readonly IReadOnlyDictionary<int, KeyValuePair<int, string>> characterIdToStringIdAndName;

        public bool IsCidCharset { get; } = false;

        private CompactFontFormatExpertSubsetCharset()
        {
            var furtherMap = new Dictionary<int, KeyValuePair<int, string>>();
            var i = 0;
            foreach (var pair in StringIdToName)
            {
                furtherMap[i++] = pair;
            }

            characterIdToStringIdAndName = furtherMap;
        }

        public string GetNameByGlyphId(int glyphId)
        {
            return characterIdToStringIdAndName[glyphId].Value;
        }

        public string GetNameByStringId(int stringId)
        {
            return characterIdToStringIdAndName.Single(x => x.Value.Key == stringId).Value.Value;
        }

        public int GetStringIdByGlyphId(int glyphId)
        {
            return characterIdToStringIdAndName[glyphId].Key;
        }
    }
}