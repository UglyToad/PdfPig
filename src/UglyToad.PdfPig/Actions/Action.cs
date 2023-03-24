namespace UglyToad.PdfPig.Actions
{
    /// <summary>
    /// Actions (PDF reference 8.5)
    /// </summary>
    public class Action
    {
        /// <summary>
        /// Type of action
        /// </summary>
        public ActionType Type { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="type"></param>
        protected Action(ActionType type)
        {
            Type = type;
        }
    }
}
