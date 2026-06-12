namespace UglyToad.PdfPig.Actions
{
    /// <summary>
    /// Launch action (PDF reference 8.5.3), launching an application, usually to open or print a file.
    /// </summary>
    public sealed class LaunchAction : PdfAction
    {
        /// <summary>
        /// The application to be launched or the document to be opened or printed (the <c>F</c> entry),
        /// or <see langword="null"/> if it is absent or only platform-specific entries are present.
        /// </summary>
        public string? FileName { get; }

        /// <summary>
        /// Specifies whether to open the destination document in a new window, in the same window,
        /// or in accordance with the current user preference.
        /// </summary>
        public OpenMode OpenInNewWindow { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="fileName">The application to be launched or the document to be opened or printed.</param>
        /// <param name="openInNewWindow">How the destination document should be opened.</param>
        public LaunchAction(string? fileName, OpenMode openInNewWindow) : base(ActionType.Launch)
        {
            FileName = fileName;
            OpenInNewWindow = openInNewWindow;
        }
    }
}
