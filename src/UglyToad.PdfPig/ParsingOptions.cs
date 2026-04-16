namespace UglyToad.PdfPig
{
    using Filters;
    using System.Collections.Generic;
    using Logging;

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
        /// Whether to defer loading stream byte data until it is first accessed.
        /// When <see langword="true"/>, stream data (images, fonts, etc.) is not read from
        /// the PDF file during initial parsing but loaded on demand when accessed. This can
        /// reduce memory usage for large documents where not all pages or images are needed.
        /// Requires the underlying PDF stream to remain open and seekable for the lifetime
        /// of the <see cref="PdfDocument"/>.
        /// Defaults to <see langword="false"/> for backwards compatibility.
        /// </summary>
        public bool LazyLoading { get; set; } = false;

        /// <summary>
        /// Controls which content types are extracted when processing pages.
        /// Defaults to <see cref="PdfCapabilities.All"/>.
        /// Set to e.g. <see cref="PdfCapabilities.Text"/> to skip images and paths.
        /// </summary>
        public PdfCapabilities Capabilities { get; set; } = PdfCapabilities.All;
    }
}