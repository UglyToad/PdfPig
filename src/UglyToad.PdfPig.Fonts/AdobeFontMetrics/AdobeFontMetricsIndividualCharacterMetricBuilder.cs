namespace UglyToad.PdfPig.Fonts.AdobeFontMetrics
{
    using Core;

    internal class AdobeFontMetricsIndividualCharacterMetricBuilder
    {
        public int CharacterCode { get; set; }

        public double WidthX { get; set; }
        public double WidthY { get; set; }

        public double WidthXDirection0 { get; set; }
        public double WidthYDirection0 { get; set; }

        public double WidthXDirection1 { get; set; }
        public double WidthYDirection1 { get; set; }

        public string Name { get; set; }

        public AdobeFontMetricsVector VVector { get; set; }

        public PdfRectangle BoundingBox { get; set; }

        public AdobeFontMetricsLigature Ligature { get; set; }

        public AdobeFontMetricsIndividualCharacterMetric Build()
        {
            return new AdobeFontMetricsIndividualCharacterMetric(CharacterCode, Name, new AdobeFontMetricsVector(WidthX, WidthY), 
                new AdobeFontMetricsVector(WidthXDirection0, WidthYDirection0), 
                new AdobeFontMetricsVector(WidthXDirection1, WidthYDirection1), 
                VVector,
                BoundingBox,
                Ligature);
        }
    }
}