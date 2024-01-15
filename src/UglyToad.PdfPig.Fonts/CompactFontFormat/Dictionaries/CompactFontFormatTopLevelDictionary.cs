namespace UglyToad.PdfPig.Fonts.CompactFontFormat.Dictionaries
{
    using Core;

    internal sealed class CompactFontFormatTopLevelDictionary
    {
        public const int UnsetOffset = -1;

        public string Version { get; set; }

        public string Notice { get; set; }

        public string Copyright { get; set; }

        public string FullName { get; set; }

        public string FamilyName { get; set; }

        public string Weight { get; set; }

        public bool IsFixedPitch { get; set; }

        public decimal ItalicAngle { get; set; }

        public decimal UnderlinePosition { get; set; } = -100;

        public decimal UnderlineThickness { get; set; } = 50;

        public decimal PaintType { get; set; }

        public CompactFontFormatCharStringType CharStringType { get; set; } = CompactFontFormatCharStringType.Type2;

        public TransformationMatrix? FontMatrix { get; set; }

        public decimal StrokeWidth { get; set; }

        public decimal UniqueId { get; set; }

        public PdfRectangle FontBoundingBox { get; set; } = new PdfRectangle(0, 0, 0, 0);

        public decimal[] Xuid { get; set; }

        public int CharSetOffset { get; set; } = UnsetOffset;

        public int EncodingOffset { get; set; } = UnsetOffset;

        public SizeAndOffset? PrivateDictionaryLocation { get; set; }

        public int CharStringsOffset { get; set; } = -1;

        public int SyntheticBaseFontIndex { get; set; }

        public string PostScript { get; set; }

        public string BaseFontName { get; set; }

        public decimal[] BaseFontBlend { get; set; }

        public bool IsCidFont { get; set; }

        public CidFontOperators CidFontOperators { get; set; } = new CidFontOperators();

        public readonly struct SizeAndOffset
        {
            public int Size { get; }

            public int Offset { get; }

            public SizeAndOffset(int size, int offset)
            {
                Size = size;
                Offset = offset;
            }

            public override string ToString()
            {
                return $"Size: {Size}, Offset: {Offset}";
            }
        }
    }

    internal sealed class CidFontOperators
    {
        public RegistryOrderingSupplement Ros { get; set; }

        public int Version { get; set; } = 0;

        public int Revision { get; set; } = 0;

        public int Type { get; set; } = 0;

        public int Count { get; set; } = 8720;

        public decimal UidBase { get; set; }

        public int FontDictionaryArray { get; set; }

        public int FontDictionarySelect { get; set; }

        public string FontName { get; set; }
    }

    internal sealed class RegistryOrderingSupplement
    {
        public string Registry { get; set; }

        public string Ordering { get; set; }

        public decimal Supplement { get; set; }
    }

    /// <summary>
    /// Defines the format of the CharString data contained within a Compact Font Format font.
    /// </summary>
    internal enum CompactFontFormatCharStringType
    {
        /// <summary>
        /// The Type 1 CharString format as defined by the Adobe Type 1 Font Format.
        /// </summary>
        Type1 = 1,
        /// <summary>
        /// The Type 2 CharString format as defined by Adobe Technical Note #5177. This is the default type.
        /// </summary>
        Type2 = 2
    }
}