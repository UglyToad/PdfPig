namespace UglyToad.PdfPig.Fonts
{
    using Geometry;
    using Parser;

    internal class IndividualCharacterMetric
    {
        public int CharacterCode { get; set; }

        public decimal WidthX { get; set; }
        public decimal WidthY { get; set; }

        public decimal WidthXDirection0 { get; set; }
        public decimal WidthYDirection0 { get; set; }

        public decimal WidthXDirection1 { get; set; }
        public decimal WidthYDirection1 { get; set; }

        public string Name { get; set; }

        public PdfVector VVector { get; set; }

        public PdfRectangle BoundingBox { get; set; }

        public Ligature Ligature { get; set; }
    }
}