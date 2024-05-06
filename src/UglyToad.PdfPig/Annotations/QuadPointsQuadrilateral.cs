namespace UglyToad.PdfPig.Annotations
{
    using Core;

    /// <summary>
    /// A QuadPoints quadrilateral is four points defining the region for an annotation to use.
    /// An annotation may cover multiple quadrilaterals.
    /// </summary>
    public readonly struct QuadPointsQuadrilateral
    {
        private readonly PdfPoint[] points;

        /// <summary>
        /// The 4 points defining this quadrilateral.
        /// The PDF specification defines these as being in anti-clockwise order starting from the lower-left corner, however
        /// Adobe's implementation doesn't obey the specification and points seem to go in the order: top-left, top-right,
        /// bottom-left, bottom-right. See: https://stackoverflow.com/questions/9855814/pdf-spec-vs-acrobat-creation-quadpoints.
        /// </summary>
        public ReadOnlySpan<PdfPoint> Points => points;

        /// <summary>
        /// Create a new <see cref="QuadPointsQuadrilateral"/>.
        /// </summary>
        public QuadPointsQuadrilateral(PdfPoint[] points)
        {
            if (points is null)
            {
                throw new ArgumentNullException(nameof(points));
            }

            if (points.Length != 4)
            {
                throw new ArgumentException($"Quadpoints quadrilateral should only contain 4 points, instead got {points.Length} points.");
            }

            this.points = points;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"[ {Points[0]}, {Points[1]}, {Points[2]}, {Points[3]} ]";
        }
    }
}