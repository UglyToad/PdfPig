namespace UglyToad.PdfPig.Actions
{
    /// <summary>
    /// Named action (PDF reference 8.5.3), executing an action predefined by the viewer application,
    /// such as <c>NextPage</c>, <c>PrevPage</c>, <c>FirstPage</c> or <c>LastPage</c>.
    /// </summary>
    public sealed class NamedAction : PdfAction
    {
        /// <summary>
        /// The name of the action to be performed (the <c>N</c> entry).
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name">The name of the action to be performed.</param>
        public NamedAction(string name) : base(ActionType.Named)
        {
            Name = name;
        }
    }
}
