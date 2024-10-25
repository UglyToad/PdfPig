namespace UglyToad.PdfPig.Outline.Destinations
{
    /// <summary>
    /// A destination location in the same file.
    /// </summary>
    public class ExplicitDestination
    {
        /// <summary>
        /// The page number (1-based) of the destination.
        /// <para>A value of <c>0</c> means no page destination was available (i.e. an invalid explicit destination).</para>
        /// </summary>
        public int PageNumber { get; }

        /// <summary>
        /// The display type of the destination.
        /// </summary>
        public ExplicitDestinationType Type { get; }

        /// <summary>
        /// The display coordinates of the destination.
        /// </summary>
        public ExplicitDestinationCoordinates Coordinates { get; }

        /// <summary>
        /// Create a new <see cref="ExplicitDestination"/>.
        /// </summary>
        public ExplicitDestination(int pageNumber, ExplicitDestinationType type, ExplicitDestinationCoordinates coordinates)
        {
            PageNumber = pageNumber;
            Type = type;
            Coordinates = coordinates;
        }
    }
}