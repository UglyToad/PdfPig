namespace UglyToad.PdfPig.Fonts
{
    using Geometry;
    using Parser;

    internal class IndividualCharacterMetric
    {
        public int CharacterCode { get; set; }

        public double WidthX { get; set; }
        public double WidthY { get; set; }

        public double WidthXDirection0 { get; set; }
        public double WidthYDirection0 { get; set; }

        public double WidthXDirection1 { get; set; }
        public double WidthYDirection1 { get; set; }

        public string Name { get; set; }

        public PdfVector VVector { get; set; }

        public PdfRectangle BoundingBox { get; set; }

        public Ligature Ligature { get; set; }
    }
}