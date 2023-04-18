namespace UglyToad.PdfPig.Actions
{
    using Outline.Destinations;

    /// <summary>
    /// GoToE action (to go to a destination inside a file embedded within the PDF)
    /// </summary>
    public class GoToEAction : AbstractGoToAction
    {
        /// <summary>
        /// File specification of the embedded file
        /// </summary>
        public string FileSpecification { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="destination">Destination within the embedded file</param>
        /// <param name="fileSpecification">Specification of the embedded file</param>
        public GoToEAction(ExplicitDestination destination, string fileSpecification) : base(ActionType.GoToE, destination)
        {
            FileSpecification = fileSpecification;
        }
    }
}