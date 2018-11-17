namespace UglyToad.PdfPig.Fonts.CompactFontFormat.Dictionaries
{
    internal class CompactFontFormatPrivateDictionary
    {
        public decimal[] BlueValues { get; set; }

        public decimal[] OtherBlues { get; set; }

        public decimal[] FamilyBlues { get; set; }

        public decimal[] FamilyOtherBlues { get; set; }

        public decimal BlueScale { get; set; } = 0.039625m;

        public decimal BlueShift { get; set; } = 7;

        public decimal BlueFuzz { get; set; } = 1;

        public decimal StandardHorizontalWidth { get; set; }

        public decimal StandardVerticalWidth { get; set; }

        public decimal[] StemSnapHorizontal { get; set; }

        public decimal[] StemStapVertical { get; set; }

        public bool ForceBold { get; set; }

        public decimal LanguageGroup { get; set; }

        public decimal ExpansionFactor { get; set; }

        public decimal InitialRandomSeed { get; set; }

        public int? LocalSubroutineLocalOffset { get; set; }

        /// <summary>
        /// If a glyph's width equals the default width X it can be omitted from the charstring.
        /// </summary>
        public decimal DefaultWidthX { get; set; }

        /// <summary>
        /// If not equal to <see cref="DefaultWidthX"/>, Glyph width is computed by adding the charstring width to the nominal width X value.
        /// </summary>
        public decimal NominalWidthX { get; set; }
    }
}