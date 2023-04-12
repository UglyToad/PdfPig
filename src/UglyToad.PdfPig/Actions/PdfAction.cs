namespace UglyToad.PdfPig.Actions
{
    /// <summary>
    /// Actions (PDF reference 8.5)
    /// </summary>
    public class PdfAction
    {
        /// <summary>
        /// Type of action
        /// </summary>
        public ActionType Type { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        protected PdfAction(ActionType type)
        {
            Type = type;
        }
    }
}
