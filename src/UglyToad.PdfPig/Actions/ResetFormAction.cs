namespace UglyToad.PdfPig.Actions
{
    using System.Collections.Generic;

    /// <summary>
    /// Reset-form action (PDF reference 8.6.4), resetting selected interactive form fields to their
    /// default values.
    /// </summary>
    public sealed class ResetFormAction : PdfAction
    {
        /// <summary>
        /// The fully qualified names of the form fields to include in or exclude from the reset,
        /// depending on the <see cref="Flags"/>. Empty when the <c>Fields</c> entry is absent, in which
        /// case all fields in the document are reset.
        /// </summary>
        public IReadOnlyList<string> Fields { get; }

        /// <summary>
        /// A set of flags specifying various characteristics of the action (the <c>Flags</c> entry).
        /// Bit 1 (Include/Exclude) determines whether <see cref="Fields"/> lists the fields to reset
        /// or the fields to exclude from resetting.
        /// </summary>
        public int Flags { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="fields">The fully qualified names of the form fields.</param>
        /// <param name="flags">The action flags.</param>
        public ResetFormAction(IReadOnlyList<string> fields, int flags) : base(ActionType.ResetForm)
        {
            Fields = fields;
            Flags = flags;
        }
    }
}
