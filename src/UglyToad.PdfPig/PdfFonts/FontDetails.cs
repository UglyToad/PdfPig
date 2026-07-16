namespace UglyToad.PdfPig.PdfFonts
{
    using Core;

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

        /// <summary>
        /// The indirect reference of the font dictionary this font was loaded from,
        /// when it was loaded from an indirect object. Uniquely identifies the font
        /// within its source document even when multiple font objects share the same
        /// name (e.g. two different subsets of one typeface embedded without unique
        /// subset prefixes). <c>null</c> for fonts defined directly inside a resource
        /// dictionary (no indirect object) or for synthetic/fallback fonts.
        /// </summary>
        public IndirectReference? FontDictionaryReference { get; private set; }

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

            // Evaluated lazily so a FontDictionaryReference assigned after construction
            // still propagates to the bold variant.
            _bold = isBold
                ? new Lazy<FontDetails>(() => this)
                : new Lazy<FontDetails>(() => new FontDetails(Name, true, Weight, IsItalic)
                {
                    FontDictionaryReference = FontDictionaryReference
                });
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
            {
                FontDictionaryReference = FontDictionaryReference
            }
            : this;

        /// <summary>
        /// Record the indirect reference of the font dictionary this font was loaded from.
        /// Called by the resource system immediately after font construction; assigning the
        /// same value again is a no-op.
        /// </summary>
        internal void SetFontDictionaryReference(IndirectReference reference)
        {
            FontDictionaryReference = reference;

            // Propagate to an already-materialized bold variant. The bold Lazy caches its
            // value, so a variant created before this stamp would otherwise keep a stale
            // null reference forever. (A not-yet-materialized variant picks the reference
            // up at evaluation time via the initializer closure.)
            if (_bold.IsValueCreated && !ReferenceEquals(_bold.Value, this))
            {
                _bold.Value.SetFontDictionaryReference(reference);
            }
        }

        /// <inheritdoc />
        public override string ToString()
        {
            var boldString = IsBold ? " (bold)" : string.Empty;
            var italicString = IsItalic ? " (italic)" : string.Empty;
            return $"{Name}{boldString}{italicString}";
        }
    }
}
