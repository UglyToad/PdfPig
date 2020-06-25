namespace UglyToad.PdfPig
{
    using System.Collections.Generic;
    using Logging;

    /// <summary>
    /// Configures options used by the parser when reading PDF documents.
    /// </summary>
    public class ParsingOptions
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
        /// Should the parser suppress duplicate overlapping text (used to create fake bold).
        /// Defaults to <see langword="true"/>.
        /// </summary>
        public bool SuppressDuplicateOverlappingText { get; set; } = true;

        /// <summary>
        /// Should the parser ignore issues where the document does not conform to the PDF specification?
        /// </summary>
        public bool UseLenientParsing { get; set; } = true;

        private ILog logger = new NoOpLog();
        /// <summary>
        /// The <see cref="ILog"/> used to record messages raised by the parsing process.
        /// </summary>
        public ILog Logger
        {
            get => logger ?? new NoOpLog();
            set => logger = value;
        }

        /// <summary>
        /// The password to use to open the document if it is encrypted. If you need to supply multiple passwords to test against
        /// you can use <see cref="Passwords"/>. The value of <see cref="Password"/> will be included in the list to test against.
        /// </summary>
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// All passwords to try when opening this document, will include any values set for <see cref="Password"/>.
        /// </summary>
        public List<string> Passwords { get; set; } = new List<string>();
    }
}