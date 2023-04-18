namespace UglyToad.PdfPig.Actions
{
    using Outline.Destinations;

    /// <summary>
    /// GoTo action (with a destination inside the current document)
    /// </summary>
    public class GoToAction : AbstractGoToAction
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public GoToAction(ExplicitDestination destination) : base(ActionType.GoTo, destination)
        {
        }
    }
}
