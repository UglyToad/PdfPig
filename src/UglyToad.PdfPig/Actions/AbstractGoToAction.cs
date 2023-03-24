namespace UglyToad.PdfPig.Actions;

using Outline.Destinations;

/// <summary>
/// Abstract class for GoTo-type actions (GoTo, GoToE, GoToR) that have a destination
/// </summary>
public abstract class AbstractGoToAction : Action
{
    /// <summary>
    /// Destination for the GoTo-type action
    /// </summary>
    public ExplicitDestination Destination { get; }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="type"></param>
    /// <param name="destination"></param>
    protected AbstractGoToAction(ActionType type, ExplicitDestination destination) : base(type)
    {
        Destination = destination;
    }
}