namespace UglyToad.PdfPig.Actions
{
    using System.Collections.Generic;

    /// <summary>
    /// Hide action (PDF reference 8.5.3), hiding or showing one or more annotations on the screen by
    /// setting or clearing their Hidden flags.
    /// </summary>
    public sealed class HideAction : PdfAction
    {
        /// <summary>
        /// The fully qualified names of the form fields whose associated annotations are to be hidden
        /// or shown (the <c>T</c> entry, when it is a text string or an array of text strings). Empty
        /// when the targets are specified as annotation references rather than field names.
        /// </summary>
        public IReadOnlyList<string> Fields { get; }

        /// <summary>
        /// Whether to hide (<see langword="true"/>) or show (<see langword="false"/>) the annotations.
        /// Default value: <see langword="true"/>.
        /// </summary>
        public bool Hide { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="fields">The fully qualified names of the targeted form fields.</param>
        /// <param name="hide">Whether to hide (<see langword="true"/>) or show (<see langword="false"/>) the annotations.</param>
        public HideAction(IReadOnlyList<string> fields, bool hide) : base(ActionType.Hide)
        {
            Fields = fields;
            Hide = hide;
        }
    }
}
