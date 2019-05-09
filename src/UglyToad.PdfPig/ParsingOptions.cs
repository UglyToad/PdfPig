namespace UglyToad.PdfPig
{
    using Logging;

    /// <summary>
    /// Configures options used by the parser when reading PDF documents.
    /// </summary>
    public class ParsingOptions
    {
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
        /// The password to use to open the document if it is encrypted.
        /// </summary>
        public string Password { get; set; } = string.Empty;
    }
}