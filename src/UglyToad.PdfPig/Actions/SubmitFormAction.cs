namespace UglyToad.PdfPig.Actions
{
    using System.Collections.Generic;

    /// <summary>
    /// Submit-form action (PDF reference 8.6.4), transmitting the names and values of selected
    /// interactive form fields to a uniform resource locator.
    /// </summary>
    public sealed class SubmitFormAction : PdfAction
    {
        /// <summary>
        /// The URL of the script at the Web server that will process the submission (the <c>F</c> entry),
        /// or <see langword="null"/> if it is absent.
        /// </summary>
        public string? FileName { get; }

        /// <summary>
        /// The fully qualified names of the form fields to include in or exclude from the submission,
        /// depending on the <see cref="Flags"/>. Empty when the <c>Fields</c> entry is absent, in which
        /// case all fields in the document are submitted.
        /// </summary>
        public IReadOnlyList<string> Fields { get; }

        /// <summary>
        /// A set of flags specifying various characteristics of the action (the <c>Flags</c> entry).
        /// </summary>
        public int Flags { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="fileName">The URL that will process the submission.</param>
        /// <param name="fields">The fully qualified names of the form fields.</param>
        /// <param name="flags">The action flags.</param>
        public SubmitFormAction(string? fileName, IReadOnlyList<string> fields, int flags) : base(ActionType.SubmitForm)
        {
            FileName = fileName;
            Fields = fields;
            Flags = flags;
        }
    }
}
