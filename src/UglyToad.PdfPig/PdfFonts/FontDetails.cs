namespace UglyToad.PdfPig.PdfFonts
{
    /// <summary>
    /// Summary details of the font used to draw a glyph.
    /// </summary>
    public sealed class FontDetails
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

        private readonly Lazy<FontDetails> _bold;

        /// <summary>
        /// Create a new <see cref="FontDetails"/>.
        /// </summary>
        public FontDetails(string? name, bool isBold, int weight, bool isItalic)
        {
            Name = name ?? string.Empty;
            IsBold = isBold;
            Weight = weight;
            IsItalic = isItalic;

            _bold = isBold ? new Lazy<FontDetails>(() => this) : new Lazy<FontDetails>(() => new FontDetails(Name, true, Weight, IsItalic));
        }

        /// <summary>
        /// An instance of <see cref="FontDetails"/> with the same properties as the current instance,
        /// but with the <see cref="IsBold"/> property set to <c>true</c>.
        /// </summary>
        public FontDetails AsBold()
        {
            return _bold.Value;
        }

        internal static FontDetails GetDefault(string? name = null) => new FontDetails(name ?? string.Empty,
            false,
            DefaultWeight,
            false);

        internal FontDetails WithName(string? name) => name is not null
            ? new FontDetails(name, IsBold, Weight, IsItalic)
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
