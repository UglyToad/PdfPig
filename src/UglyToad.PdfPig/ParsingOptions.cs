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
        /// Whether to load image byte data when enumerating images on a page.
        /// When <see langword="true"/> (the default), image bytes are available via <see cref="Content.IPdfImage.RawMemory"/>
        /// and <see cref="Content.IPdfImage.TryGetBytesAsMemory(out System.Memory{byte})"/>.
        /// When <see langword="false"/>, image metadata (width, height, bounding box, color space, etc.)
        /// is still available but image byte data is not retained, reducing memory usage
        /// for documents with large or numerous images.
        /// </summary>
        public bool EagerlyLoadImageBytes { get; set; } = true;
    }
}