namespace UglyToad.PdfPig.Outline.Destinations
{
    /// <summary>
    /// The display type for opening an <see cref="ExplicitDestination"/>.
    /// </summary>
    public enum ExplicitDestinationType : byte
    {
        /// <summary>
        /// Display the page with the given top left coordinates and 
        /// zoom level.
        /// </summary>
        XyzCoordinates = 0,
        /// <summary>
        /// Fit the entire page within the window.
        /// </summary>
        FitPage = 1,
        /// <summary>
        /// Fit the entire page width within the window.
        /// </summary>
        FitHorizontally = 2,
        /// <summary>
        /// Fit the entire page height within the window.
        /// </summary>
        FitVertically = 3,
        /// <summary>
        /// Fit the rectangle specified by the <see cref="ExplicitDestinationCoordinates"/>
        /// within the window.
        /// </summary>
        FitRectangle = 4,
        /// <summary>
        /// Fit the page's bounding box within the window.
        /// </summary>
        FitBoundingBox = 5,
        /// <summary>
        /// Fit the page's bounding box width within the window.
        /// </summary>
        FitBoundingBoxHorizontally = 6,
        /// <summary>
        /// Fit the page's bounding box height within the window.
        /// </summary>
        FitBoundingBoxVertically = 7
    }
}