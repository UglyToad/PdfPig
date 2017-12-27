namespace UglyToad.Pdf.Content
{
    using Geometry;

    public class Letter
    {
        public string Value { get; }

        public PdfPoint Location { get; }

        public decimal Width { get; }

        public decimal FontSize { get; }

        public string FontName { get; }

        public Letter(string value, PdfPoint location, decimal width, decimal fontSize, string fontName)
        {
            Value = value;
            Location = location;
            Width = width;
            FontSize = fontSize;
            FontName = fontName;
        }
    }
}
