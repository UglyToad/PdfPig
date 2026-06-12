namespace UglyToad.PdfPig.Actions
{
    /// <summary>
    /// Thread action (PDF reference 8.5.3), beginning reading an article thread, possibly in another
    /// document.
    /// </summary>
    public sealed class ThreadAction : PdfAction
    {
        /// <summary>
        /// The file containing the thread (the <c>F</c> entry), or <see langword="null"/> when the
        /// thread is in the current document.
        /// </summary>
        public string? FileName { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="fileName">The file containing the thread, or <see langword="null"/> for the current document.</param>
        public ThreadAction(string? fileName) : base(ActionType.Thread)
        {
            FileName = fileName;
        }
    }
}
