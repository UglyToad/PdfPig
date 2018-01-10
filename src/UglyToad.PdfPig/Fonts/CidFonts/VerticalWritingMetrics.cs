namespace UglyToad.PdfPig.Fonts.CidFonts
{
    using System.Collections.Generic;
    using Geometry;

    internal class VerticalWritingMetrics
    {
        public VerticalVectorComponents DefaultVerticalWritingMetrics { get; }

        public IReadOnlyDictionary<int, decimal> IndividualVerticalWritingDisplacements { get; }

        public IReadOnlyDictionary<int, PdfVector> IndividualVerticalWritingPositions { get; }

        public VerticalWritingMetrics(VerticalVectorComponents defaultVerticalWritingMetrics, IReadOnlyDictionary<int, decimal> individualVerticalWritingDisplacements, IReadOnlyDictionary<int, PdfVector> individualVerticalWritingPositions)
        {
            DefaultVerticalWritingMetrics = defaultVerticalWritingMetrics;
            IndividualVerticalWritingDisplacements = individualVerticalWritingDisplacements;
            IndividualVerticalWritingPositions = individualVerticalWritingPositions;
        }
    }
}
