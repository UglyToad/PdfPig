namespace UglyToad.PdfPig.Outline.Destinations
{
    /// <summary>
    /// The coordinates of the region to display for a <see cref="ExplicitDestination"/>.
    /// </summary>
    public class ExplicitDestinationCoordinates
    {
        /// <summary>
        /// An empty set of coordinates where no values have been set.
        /// </summary>
        public static ExplicitDestinationCoordinates Empty { get; } = new ExplicitDestinationCoordinates(null, null, null, null);

        /// <summary>
        /// The left side of the region to display.
        /// </summary>
        public decimal? Left { get; }

        /// <summary>
        /// The top edge of the region to display.
        /// </summary>
        public decimal? Top { get; }

        /// <summary>
        /// The right side of the region to display
        /// </summary>
        public decimal? Right { get; }

        /// <summary>
        /// The bottom edge of the region to display.
        /// </summary>
        public decimal? Bottom { get; }

        /// <summary>
        /// Create a new <see cref="ExplicitDestinationCoordinates"/>.
        /// </summary>
        public ExplicitDestinationCoordinates(decimal? left)
        {
            Left = left;
        }

        /// <summary>
        /// Create a new <see cref="ExplicitDestinationCoordinates"/>.
        /// </summary>
        public ExplicitDestinationCoordinates(decimal? left, decimal? top)
        {
            Left = left;
            Top = top;
        }

        /// <summary>
        /// Create a new <see cref="ExplicitDestinationCoordinates"/>.
        /// </summary>
        public ExplicitDestinationCoordinates(decimal? left, decimal? top, decimal? right, decimal? bottom)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }
    }
}