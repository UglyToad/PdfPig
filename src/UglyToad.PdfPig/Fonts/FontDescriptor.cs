namespace UglyToad.PdfPig.Fonts
{
    using Geometry;
    using Tokens;
    using Util.JetBrains.Annotations;

    /// <summary>
    /// Specifies metrics and attributes of a simple font or CID Font
    /// for the whole font rather than per-glyph.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Provides information to enable consumer applications to
    /// find a substitute font when the font is unavailable.
    /// </para>
    /// <para>
    /// Font descriptors are not used with Type 0 fonts.
    /// </para>
    /// <para>
    /// A font descriptor is a dictionary used to specify various attributes.
    /// </para>
    /// </remarks>
    internal class FontDescriptor
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
        public string FontFamily { get; set; }

        /// <summary>
        /// The font stretch value.
        /// </summary>
        /// <remarks>Optional</remarks>
        public FontStretch Stretch { get; set; } = FontStretch.Normal;

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
        public decimal FontWeight { get; set; } = 400;

        /// <summary>
        /// Defines various font characteristics. See <see cref="FontFlags"/>.
        /// </summary>
        /// <remarks>Required</remarks>
        public FontFlags Flags { get; }

        /// <summary>
        /// A rectangle in glyph coordinates which represents the smallest
        /// rectangle containing all glyphs of the font.
        /// </summary>
        /// <remarks>Required (Except Type 3)</remarks>
        public PdfRectangle BoundingBox { get; set; }

        /// <summary>
        /// The angle in degrees counter-clockwise from vertical of the vertical
        /// lines of the font.
        /// The value is negative for fonts sloping right (italic fonts).
        /// </summary>
        /// <example>9 o'clock is represented by 90 degrees. 3 o'clock is -90 degrees.</example>
        /// <remarks>Required</remarks>
        public decimal ItalicAngle { get; set; }

        /// <summary>
        /// The maximum height above the baseline for any glyph from this font (except for accents).
        /// </summary>
        /// <remarks>Required (Except Type 3)</remarks>
        public decimal Ascent { get; set; }

        /// <summary>
        /// The maximum depth below the baseline for any glyph in the font. This is negative.
        /// </summary>
        /// <remarks>Required (Except Type 3)</remarks>
        public decimal Descent { get; set; }

        /// <summary>
        /// The spacing between consecutive lines of text. Default 0.
        /// </summary>
        /// <remarks>Optional</remarks>
        public decimal Leading { get; set; }

        /// <summary>
        /// The vertical distance of the top of flat capital letters from the baseline.
        /// </summary>
        /// <remarks>Required (Where Latin Characters, Except Type 3)</remarks>
        public decimal CapHeight { get; set; }

        /// <summary>
        /// The x height of the font. The vertical distance of the top of flat non-ascending
        /// lowercase letters (e.g. x) from the baseline. Default 0.
        /// </summary>
        /// <remarks>Optional</remarks>
        public decimal XHeight { get; set; }

        /// <summary>
        /// The horizontal thickness of vertical stems of glyphs.
        /// </summary>
        /// <remarks>Required (Except Type 3)</remarks>
        public decimal StemVertical { get; set; }

        /// <summary>
        /// The vertical thickness of horizontal stems of glyphs. Default 0.
        /// </summary>
        /// <remarks>Optional</remarks>
        public decimal StemHorizontal { get; set; }

        /// <summary>
        /// The average glyph width in the font. Default 0.
        /// </summary>
        /// <remarks>Optional</remarks>
        public decimal AverageWidth { get; set; }

        /// <summary>
        /// The maximum glyph width in the font. Default 0.
        /// </summary>
        /// <remarks>Optional</remarks>
        public decimal MaxWidth { get; set; }

        /// <summary>
        /// The width for character codes whose widths are not present in the Widths
        /// array of the font dictionary. Default 0.
        /// </summary>
        /// <remarks>Optional</remarks>
        public decimal MissingWidth { get; set; }

        /// <summary>
        /// The bytes of the font program.
        /// </summary>
        /// <remarks>Optional</remarks>
        [CanBeNull]
        public DescriptorFontFile FontFile { get; set; }

        /// <summary>
        /// The character names defined in a font subset.
        /// </summary>
        /// <remarks>Optional</remarks>
        [CanBeNull]
        public string CharSet { get; set; }

        public FontDescriptor(NameToken name, FontFlags flags)
        {
            FontName = name;
            Flags = flags;
        }
    }
}
