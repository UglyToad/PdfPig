namespace UglyToad.PdfPig
{
    using Filters;
    using System.Collections.Generic;
    using Logging;
    using UglyToad.PdfPig.Graphics.Colors.Icc;

    /// <summary>
    /// Configures options used by the parser when reading PDF documents.
    /// </summary>
    public sealed class ParsingOptions
    {
        /// <summary>
        /// A default <see cref="ParsingOptions"/> with <see cref="UseLenientParsing"/> set to false.
        /// </summary>
        public static ParsingOptions LenientParsingOff { get; } = new ParsingOptions
        {
            UseLenientParsing = false
        };

        /// <summary>
        /// Should the parser apply clipping to paths?
        /// Defaults to <see langword="false"/>.
        /// <para>Bezier curves will be transformed into polylines if clipping is set to <see langword="true"/>.</para>
        /// </summary>
        public bool ClipPaths { get; set; } = false;

        /// <summary>
        /// Should the parser ignore issues where the document does not conform to the PDF specification?
        /// </summary>
        public bool UseLenientParsing { get; set; } = true;

        /// <summary>
        /// The <see cref="ILog"/> used to record messages raised by the parsing process.
        /// </summary>
        public ILog Logger { get; set; } = new NoOpLog();

        /// <summary>
        /// The password to use to open the document if it is encrypted. If you need to supply multiple passwords to test against
        /// you can use <see cref="Passwords"/>. The value of <see cref="Password"/> will be included in the list to test against.
        /// </summary>
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// All passwords to try when opening this document, will include any values set for <see cref="Password"/>.
        /// </summary>
        public List<string> Passwords { get; set; } = new List<string>();

        /// <summary>
        /// Skip extracting content where the font could not be found, will result in some letters being skipped/missed
        /// but will prevent the library throwing where the source PDF has some corrupted text. Also skips XObjects like
        /// forms and images when missing.
        /// </summary>
        public bool SkipMissingFonts { get; set; } = false;

        /// <summary>
        /// Gets or sets the maximum allowed stack depth.
        /// </summary>
        /// <remarks>This property can be used to limit the depth of recursive or nested operations to
        /// prevent stack overflows or excessive resource usage.</remarks>
        public int MaxStackDepth { get; set; } = 256;

        /// <summary>
        /// Filter provider to use while parsing the document. The <see cref="DefaultFilterProvider"/> will be used if set to <c>null</c>.
        /// </summary>
        public IFilterProvider? FilterProvider { get; set; } = null;

        /// <summary>
        /// Should the parser use the replacement text specified by marked-content <c>/ActualText</c> entries
        /// when extracting text. When enabled, content enclosed by an <c>/ActualText</c> sequence is extracted
        /// using that replacement text (see the PDF specification, 14.9.4 "Replacement text") instead of the
        /// enclosed glyphs' own Unicode values.
        /// Defaults to <see langword="false"/>.
        /// </summary>
        public bool UseActualText { get; set; } = false;

        /// <summary>
        /// Service used to convert <c>/ICCBased</c> color space samples to sRGB.
        /// When <c>null</c> (default), ICC-based color spaces fall back silently
        /// to their declared alternate color space.
        /// </summary>
        public IIccProfileService? IccProfileService { get; set; } = null;

        /// <summary>
        /// Should the parser colour-manage device colour spaces (DeviceGray, DeviceRGB, DeviceCMYK)
        /// through the document's (or page's) output intent <c>/DestOutputProfile</c> when one is present?
        /// <para>
        /// Per the PDF specification (14.11.5, "Output intents") the data in an output intent dictionary
        /// "shall be for informational purposes only, and PDF processors are free to disregard it", and
        /// there is "no expectation" that device colours are automatically converted to the target space
        /// (such conversion is "undesirable" in some workflows). Enabling this treats the output intent as
        /// the calibration of the device's native colour space for preview/proofing (see 8.6.5.7,
        /// "Implicit conversion of CIE-based colour spaces"); disabling it leaves device colours to their
        /// built-in conversion.
        /// </para>
        /// <para>
        /// Defaults to <see langword="false"/>: the specification sets "no expectation" that device colours
        /// are automatically converted to the output-intent target and notes such conversion is
        /// "undesirable" in some workflows, so it is opt-in. A previewing/proofing consumer (such as a
        /// renderer) can enable it. Requires <see cref="IccProfileService"/> to be set (the output intent
        /// profile is parsed through it).
        /// </para>
        /// </summary>
        public bool UseOutputIntentColorManagement { get; set; } = false;
    }
}