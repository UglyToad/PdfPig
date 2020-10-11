namespace UglyToad.PdfPig.PdfFonts
{
    using Core;
    using Tokens;
    using Util.JetBrains.Annotations;

    /// <summary>
    /// Specifies metrics and attributes of a simple font or CID Font for the whole font rather than per-glyph.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Provides information to enable consumer applications to find a substitute font when the font is unavailable.
    /// </para>
    /// <para>
    /// Font descriptors are not used with Type 0 fonts.
    /// </para>
    /// <para>
    /// A font descriptor is a dictionary used to specify various attributes.
    /// </para>
    /// </remarks>
    public class FontDescriptor
    {
        /// <summary>
        /// The PostScript name for the font.
        /// </summary>
        /// <remarks>Required</remarks>
        public NameToken FontName { get; }

        /// <summary>
        /// The preferred font family.
        /// </summary>
        /// <remarks>Optional</remarks>
        public string FontFamily { get; }

        /// <summary>
        /// The font stretch value.
        /// </summary>
        /// <remarks>Optional</remarks>
        public FontStretch Stretch { get; }

        /// <summary>
        /// The weight/thickness of the font.
        /// </summary>
        /// <remarks>
        /// Possible values:
        /// 100<br/>
        /// 200<br/>
        /// 300<br/>
        /// 500 (normal)<br/>
        /// 600<br/>
        /// 700<br/>
        /// 800<br/>
        /// 900<br/>
        /// Optional
        /// </remarks>
        public decimal FontWeight { get; }

        /// <summary>
        /// Defines various font characteristics. See <see cref="FontDescriptorFlags"/>.
        /// </summary>
        /// <remarks>Required</remarks>
        public FontDescriptorFlags Flags { get; }

        /// <summary>
        /// A rectangle in glyph coordinates which represents the smallest
        /// rectangle containing all glyphs of the font.
        /// </summary>
        /// <remarks>Required (Except Type 3)</remarks>
        public PdfRectangle BoundingBox { get; }

        /// <summary>
        /// The angle in degrees counter-clockwise from vertical of the vertical
        /// lines of the font.
        /// The value is negative for fonts sloping right (italic fonts).
        /// </summary>
        /// <example>9 o'clock is represented by 90 degrees. 3 o'clock is -90 degrees.</example>
        /// <remarks>Required</remarks>
        public decimal ItalicAngle { get; }

        /// <summary>
        /// The maximum height above the baseline for any glyph from this font (except for accents).
        /// </summary>
        /// <remarks>Required (Except Type 3)</remarks>
        public decimal Ascent { get; }

        /// <summary>
        /// The maximum depth below the baseline for any glyph in the font. This is negative.
        /// </summary>
        /// <remarks>Required (Except Type 3)</remarks>
        public decimal Descent { get; }

        /// <summary>
        /// The spacing between consecutive lines of text. Default 0.
        /// </summary>
        /// <remarks>Optional</remarks>
        public decimal Leading { get; }

        /// <summary>
        /// The vertical distance of the top of flat capital letters from the baseline.
        /// </summary>
        /// <remarks>Required (Where Latin Characters, Except Type 3)</remarks>
        public decimal CapHeight { get; }

        /// <summary>
        /// The x height of the font. The vertical distance of the top of flat non-ascending
        /// lowercase letters (e.g. x) from the baseline. Default 0.
        /// </summary>
        /// <remarks>Optional</remarks>
        public decimal XHeight { get; }

        /// <summary>
        /// The horizontal thickness of vertical stems of glyphs.
        /// </summary>
        /// <remarks>Required (Except Type 3)</remarks>
        public decimal StemVertical { get; }

        /// <summary>
        /// The vertical thickness of horizontal stems of glyphs. Default 0.
        /// </summary>
        /// <remarks>Optional</remarks>
        public decimal StemHorizontal { get; }

        /// <summary>
        /// The average glyph width in the font. Default 0.
        /// </summary>
        /// <remarks>Optional</remarks>
        public decimal AverageWidth { get; }

        /// <summary>
        /// The maximum glyph width in the font. Default 0.
        /// </summary>
        /// <remarks>Optional</remarks>
        public decimal MaxWidth { get; }

        /// <summary>
        /// The width for character codes whose widths are not present in the Widths
        /// array of the font dictionary. Default 0.
        /// </summary>
        /// <remarks>Optional</remarks>
        public decimal MissingWidth { get; }

        /// <summary>
        /// The bytes of the font program.
        /// </summary>
        /// <remarks>Optional</remarks>
        [CanBeNull]
        public DescriptorFontFile FontFile { get; }

        /// <summary>
        /// The character names defined in a font subset.
        /// </summary>
        /// <remarks>Optional</remarks>
        [CanBeNull]
        public string CharSet { get; }

        /// <summary>
        /// Create a new <see cref="FontDescriptor"/>.
        /// </summary>
        public FontDescriptor(Builder builder)
        {
            FontName = builder.FontName;
            FontFamily = builder.FontFamily;
            Stretch = builder.Stretch;
            FontWeight = builder.FontWeight;
            Flags = builder.Flags;
            BoundingBox = builder.BoundingBox;
            ItalicAngle = builder.ItalicAngle;
            Ascent = builder.Ascent;
            Descent = builder.Descent;
            Leading = builder.Leading;
            CapHeight = builder.CapHeight;
            XHeight = builder.XHeight;
            StemVertical = builder.StemVertical;
            StemHorizontal = builder.StemHorizontal;
            AverageWidth = builder.AverageWidth;
            MaxWidth = builder.MaxWidth;
            MissingWidth = builder.MissingWidth;
            FontFile = builder.FontFile;
            CharSet = builder.CharSet;
        }

        internal FontDetails ToDetails(string name = null)
        {
            return new FontDetails(name ?? FontName ?? string.Empty,
                FontFamily,
                FontWeight > 500,
                (int)FontWeight,
                Flags.HasFlag(FontDescriptorFlags.Italic) || ItalicAngle != 0);
        }

        /// <summary>
        /// Provides a mutable way to construct a <see cref="FontDescriptor"/>.
        /// </summary>
        public class Builder
        {
            /// <summary>
            /// Sets the <see cref="FontDescriptor.FontName"/>.
            /// </summary>
            public NameToken FontName { get; set; }

            /// <summary>
            /// Sets the <see cref="FontDescriptor.FontFamily"/>.
            /// </summary>
            public string FontFamily { get; set; }

            /// <summary>
            /// Sets the <see cref="FontDescriptor.Stretch"/>.
            /// </summary>
            public FontStretch Stretch { get; set; } = FontStretch.Normal;

            /// <summary>
            /// Sets the <see cref="FontDescriptor.FontWeight"/>.
            /// </summary>
            public decimal FontWeight { get; set; } = 400;

            /// <summary>
            /// Sets the <see cref="FontDescriptor.Flags"/>.
            /// </summary>
            public FontDescriptorFlags Flags { get; set; }

            /// <summary>
            /// Sets the <see cref="FontDescriptor.BoundingBox"/>.
            /// </summary>
            public PdfRectangle BoundingBox { get; set; }

            /// <summary>
            /// Sets the <see cref="FontDescriptor.ItalicAngle"/>.
            /// </summary>
            public decimal ItalicAngle { get; set; }

            /// <summary>
            /// Sets the <see cref="FontDescriptor.Ascent"/>.
            /// </summary>
            public decimal Ascent { get; set; }

            /// <summary>
            /// Sets the <see cref="FontDescriptor.Descent"/>.
            /// </summary>
            public decimal Descent { get; set; }

            /// <summary>
            /// Sets the <see cref="FontDescriptor.Leading"/>.
            /// </summary>
            public decimal Leading { get; set; }

            /// <summary>
            /// Sets the <see cref="FontDescriptor.CapHeight"/>.
            /// </summary>
            public decimal CapHeight { get; set; }

            /// <summary>
            /// Sets the <see cref="FontDescriptor.XHeight"/>.
            /// </summary>
            public decimal XHeight { get; set; }

            /// <summary>
            /// Sets the <see cref="FontDescriptor.StemVertical"/>.
            /// </summary>
            public decimal StemVertical { get; set; }

            /// <summary>
            /// Sets the <see cref="FontDescriptor.StemHorizontal"/>.
            /// </summary>
            public decimal StemHorizontal { get; set; }

            /// <summary>
            /// Sets the <see cref="FontDescriptor.AverageWidth"/>.
            /// </summary>
            public decimal AverageWidth { get; set; }

            /// <summary>
            /// Sets the <see cref="FontDescriptor.MaxWidth"/>.
            /// </summary>
            public decimal MaxWidth { get; set; }

            /// <summary>
            /// Sets the <see cref="FontDescriptor.MissingWidth"/>.
            /// </summary>
            public decimal MissingWidth { get; set; }

            /// <summary>
            /// Sets the <see cref="FontDescriptor.FontFile"/>.
            /// </summary>
            [CanBeNull]
            public DescriptorFontFile FontFile { get; set; }

            /// <summary>
            /// Sets the <see cref="FontDescriptor.CharSet"/>.
            /// </summary>
            [CanBeNull]
            public string CharSet { get; set; }

            /// <summary>
            /// Create a new <see cref="Builder"/>.
            /// </summary>
            public Builder(NameToken fontName, FontDescriptorFlags flags)
            {
                FontName = fontName;
                Flags = flags;
            }

            /// <summary>
            /// Create the <see cref="FontDescriptor"/> with values from this builder.
            /// </summary>
            public FontDescriptor Build()
            {
                return new FontDescriptor(this);
            }
        }
    }
}
