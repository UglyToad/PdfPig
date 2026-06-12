namespace UglyToad.PdfPig.Actions
{
    /// <summary>
    /// Import-data action (PDF reference 8.6.4), importing form field values from a file.
    /// </summary>
    public sealed class ImportDataAction : PdfAction
    {
        /// <summary>
        /// The FDF file from which to import the field values (the <c>F</c> entry), or
        /// <see langword="null"/> if it is absent.
        /// </summary>
        public string? FileName { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="fileName">The FDF file from which to import the field values.</param>
        public ImportDataAction(string? fileName) : base(ActionType.ImportData)
        {
            FileName = fileName;
        }
    }
}
