namespace UglyToad.PdfPig.Actions
{
    /// <summary>
    /// JavaScript action (PDF reference 8.5.3), causing a script to be compiled and executed by the
    /// JavaScript interpreter.
    /// </summary>
    public sealed class JavaScriptAction : PdfAction
    {
        /// <summary>
        /// The JavaScript script to be executed (the <c>JS</c> entry).
        /// </summary>
        public string JavaScript { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="javaScript">The JavaScript script to be executed.</param>
        public JavaScriptAction(string javaScript) : base(ActionType.JavaScript)
        {
            JavaScript = javaScript;
        }
    }
}
