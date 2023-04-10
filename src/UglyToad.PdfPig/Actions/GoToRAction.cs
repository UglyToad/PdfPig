namespace UglyToad.PdfPig.Actions;

using Outline.Destinations;

/// <summary>
/// GoToR action, to go to a destination in a remote PDF
/// </summary>
public class GoToRAction : AbstractGoToAction
{
    /// <summary>
    /// Filename of the remote PDF
    /// </summary>
    public string Filename { get; }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="destination">Destination within the remote PDF</param>
    /// <param name="filename">Filename of the remote PDF</param>
    public GoToRAction(ExplicitDestination destination, string filename) : base(ActionType.GoToR, destination)
    {
        Filename = filename;
    }
}