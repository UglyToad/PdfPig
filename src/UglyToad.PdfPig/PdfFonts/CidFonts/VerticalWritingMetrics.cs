namespace UglyToad.PdfPig.PdfFonts.CidFonts
{
    using System.Collections.Generic;
    using Geometry;
    using Util.JetBrains.Annotations;

    /// <summary>
    /// Glyphs from fonts which support vertical writing mode define displacement and position vectors.
    /// The position vector specifies how the horizontal writing origin is transformed into the vertical writing origin.
    /// The displacement vector specifies how far to move vertically before drawing the next glyph.
    /// </summary>
    internal class VerticalWritingMetrics
    {
        /// <summary>
        /// The default position and displacement vectors where not overridden.
        /// </summary>
        public VerticalVectorComponents DefaultVerticalWritingMetrics { get; }

        /// <summary>
        /// Overrides displacement vector y components for glyphs specified by CID code.
        /// </summary>
        [NotNull]
        public IReadOnlyDictionary<int, double> IndividualVerticalWritingDisplacements { get; }

        /// <summary>
        /// Overrides position vector (x and y) components for glyphs specified by CID code.
        /// </summary>
        [NotNull]
        public IReadOnlyDictionary<int, PdfVector> IndividualVerticalWritingPositions { get; }

        /// <summary>
        /// Create new <see cref="VerticalWritingMetrics"/>.
        /// </summary>
        public VerticalWritingMetrics(VerticalVectorComponents defaultVerticalWritingMetrics, 
            [CanBeNull] IReadOnlyDictionary<int, double> individualVerticalWritingDisplacements, 
            [CanBeNull] IReadOnlyDictionary<int, PdfVector> individualVerticalWritingPositions)
        {
            DefaultVerticalWritingMetrics = defaultVerticalWritingMetrics;
            IndividualVerticalWritingDisplacements = individualVerticalWritingDisplacements
                                                     ?? new Dictionary<int, double>(0);
            IndividualVerticalWritingPositions = individualVerticalWritingPositions
                                                 ?? new Dictionary<int, PdfVector>(0);
        }

        /// <summary>
        /// Get the position vector used to convert horizontal glyph origin to vertical origin.
        /// </summary>
        public PdfVector GetPositionVector(int characterIdentifier, double glyphWidth)
        {
            if (IndividualVerticalWritingPositions.TryGetValue(characterIdentifier, out var vector))
            {
                return vector;
            }

            return DefaultVerticalWritingMetrics.GetPositionVector(glyphWidth);
        }

        /// <summary>
        /// Get the displacement vector used to move the origin to the next glyph location after drawing.
        /// </summary>
        public PdfVector GetDisplacementVector(int characterIdentifier)
        {
            if (IndividualVerticalWritingDisplacements.TryGetValue(characterIdentifier, out var displacementY))
            {
                return new PdfVector(0, displacementY);
            }

            return DefaultVerticalWritingMetrics.GetDisplacementVector();
        }
    }
}
