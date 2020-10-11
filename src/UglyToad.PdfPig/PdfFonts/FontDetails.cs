namespace UglyToad.PdfPig.PdfFonts
{
    /// <summary>
    /// Summary details of the font used to draw a glyph.
    /// </summary>
    public class FontDetails
    {
        /// <summary>
        /// The normal weight for a font.
        /// </summary>
        public const int DefaultWeight = 500;

        /// <summary>
        /// The bold weight for a font.
        /// </summary>
        public const int BoldWeight = 700;
        
        /// <summary>
        /// The font name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The font family name.
        /// </summary>
        public string FontFamily { get; }

        /// <summary>
        /// Whether the font is bold.
        /// </summary>
        public bool IsBold { get; }

        /// <summary>
        /// The font weight, values above 500 represent bold.
        /// </summary>
        public int Weight { get; }
        
        /// <summary>
        /// Whether the font is italic.
        /// </summary>
        public bool IsItalic { get; }

        /// <summary>
        /// Create a new <see cref="FontDetails"/>.
        /// </summary>
        public FontDetails(string name, string fontFamily, bool isBold, int weight, bool isItalic)
        {
            Name = name ?? string.Empty;
            FontFamily = fontFamily ?? string.Empty;
            IsBold = isBold;
            Weight = weight;
            IsItalic = isItalic;
        }

        internal static FontDetails GetDefault(string name = null, string fontFamily = null) => new FontDetails(name ?? string.Empty,
            fontFamily ?? string.Empty,
            false,
            DefaultWeight,
            false);

        internal FontDetails WithName(string name) => name != null
            ? new FontDetails(name, FontFamily, IsBold, Weight, IsItalic)
            : this;

        /// <inheritdoc />
        public override string ToString()
        {
            var boldString = IsBold ? " (bold)" : string.Empty;
            var italicString = IsItalic ? " (italic)" : string.Empty;
            return $"{Name}{boldString}{italicString}";
        }
    }
}
