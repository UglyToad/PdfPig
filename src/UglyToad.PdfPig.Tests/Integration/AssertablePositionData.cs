namespace UglyToad.PdfPig.Tests.Integration
{
    using System;
    using Content;
    using Xunit;

    public class AssertablePositionData
    {
        public decimal X { get; set; }

        public decimal Y { get; set; }

        public decimal Width { get; set; }

        public string Text { get; set; }

        public decimal FontSize { get; set; }

        public string FontName { get; set; }

        public decimal Height { get; set; }

        public static AssertablePositionData Parse(string line)
        {
            var parts = line.Split('\t');

            if (parts.Length < 6)
            {
                throw new ArgumentException($"Expected 6 parts to the line, instead got {parts.Length}");
            }

            var height = parts.Length < 7 ? 0 : decimal.Parse(parts[6]);

            return new AssertablePositionData
            {
                X = decimal.Parse(parts[0]),
                Y = decimal.Parse(parts[1]),
                Width = decimal.Parse(parts[2]),
                Text = parts[3],
                FontSize = decimal.Parse(parts[4]),
                FontName = parts[5],
                Height = height
            };
        }

        public void AssertWithinTolerance(Letter letter, Page page, bool includeHeight = true)
        {
            Assert.Equal(Text, letter.Value);
            Assert.Equal(FontName, letter.FontName);
            Assert.Equal(X, letter.Position.X, 1);
            Assert.Equal(Width, letter.Width, 1);
            if (includeHeight)
            {
                Assert.Equal(Height, letter.GlyphRectangle.Height, 1);
            }
        }

        public override string ToString()
        {
            return $"{X} {Y} {Width} {Text} {FontSize} {FontName} {Height}";
        }
    }
}