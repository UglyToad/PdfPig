namespace UglyToad.PdfPig.Tests.Integration
{
    using System.Globalization;
    using Content;

    public class AssertablePositionData
    {
        public double X { get; set; }

        public double Y { get; set; }

        public double Width { get; set; }

        public string Text { get; set; }

        public double FontSize { get; set; }

        public string FontName { get; set; }

        public double Height { get; set; }

        public static AssertablePositionData Parse(string line)
        {
            var parts = line.Split('\t');

            if (parts.Length < 6)
            {
                throw new ArgumentException($"Expected 6 parts to the line, instead got {parts.Length}");
            }

            var height = parts.Length < 7 ? 0 : double.Parse(parts[6], CultureInfo.InvariantCulture);

            return new AssertablePositionData
            {
                X = double.Parse(parts[0], CultureInfo.InvariantCulture),
                Y = double.Parse(parts[1], CultureInfo.InvariantCulture),
                Width = double.Parse(parts[2], CultureInfo.InvariantCulture),
                Text = parts[3],
                FontSize = double.Parse(parts[4], CultureInfo.InvariantCulture),
                FontName = parts[5],
                Height = height
            };
        }

        public void AssertWithinTolerance(Letter letter, Page page, bool includeHeight = true)
        {
            Assert.Equal(Text, letter.Value);
            Assert.Equal(FontName, letter.FontName);
            Assert.Equal(X, letter.Location.X, 1);
            Assert.Equal(Width, letter.Width, 1);
            if (includeHeight)
            {
                Assert.Equal(Height, letter.BoundingBox.Height, 1);
            }
        }

        public override string ToString()
        {
            return $"{X} {Y} {Width} {Text} {FontSize} {FontName} {Height}";
        }
    }
}