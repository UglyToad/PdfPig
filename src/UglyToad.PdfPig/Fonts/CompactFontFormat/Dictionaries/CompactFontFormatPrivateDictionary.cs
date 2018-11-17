namespace UglyToad.PdfPig.Fonts.CompactFontFormat.Dictionaries
{
    internal class CompactFontFormatPrivateDictionary : AdobeStylePrivateDictionary
    {
        /// <summary>
        /// Compatibility entry.
        /// </summary>
        public decimal InitialRandomSeed { get; }

        /// <summary>
        /// The offset in bytes for the local subroutine index in this font. The value is relative to this private dictionary.
        /// </summary>
        public int? LocalSubroutineOffset { get; }

        /// <summary>
        /// If a glyph's width equals the default width X it can be omitted from the charstring.
        /// </summary>
        public decimal DefaultWidthX { get; }

        /// <summary>
        /// If not equal to <see cref="DefaultWidthX"/>, Glyph width is computed by adding the charstring width to the nominal width X value.
        /// </summary>
        public decimal NominalWidthX { get; }

        /// <inheritdoc />
        /// <summary>
        /// Create a new <see cref="T:UglyToad.PdfPig.Fonts.CompactFontFormat.Dictionaries.CompactFontFormatPrivateDictionary" />.
        /// </summary>
        /// <param name="builder">The builder used to gather property values.</param>
        public CompactFontFormatPrivateDictionary(Builder builder) : base(builder)
        {
            InitialRandomSeed = builder.InitialRandomSeed;
            LocalSubroutineOffset = builder.LocalSubroutineOffset;
            DefaultWidthX = builder.DefaultWidthX;
            NominalWidthX = builder.NominalWidthX;
        }

        public static CompactFontFormatPrivateDictionary GetDefault()
        {
            return new Builder().Build();
        }

        public class Builder : BaseBuilder
        {
            public decimal InitialRandomSeed { get; set; }

            public int? LocalSubroutineOffset { get; set; }

            /// <summary>
            /// If a glyph's width equals the default width X it can be omitted from the charstring.
            /// </summary>
            public decimal DefaultWidthX { get; set; }

            /// <summary>
            /// If not equal to <see cref="DefaultWidthX"/>, Glyph width is computed by adding the charstring width to the nominal width X value.
            /// </summary>
            public decimal NominalWidthX { get; set; }

            public CompactFontFormatPrivateDictionary Build()
            {
                return new CompactFontFormatPrivateDictionary(this);
            }
        }
    }
}