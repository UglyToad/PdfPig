namespace UglyToad.PdfPig.Tests
{
    using System.Collections.Generic;
    using System.Linq;
    using Xunit;

    public class PublicApiScannerTests
    {
        [Fact]
        public void OnlyExposedApiIsPublic()
        {
            var assembly = typeof(PdfDocument).Assembly;

            var types = assembly.GetTypes();

            var publicTypeNames = new List<string>();

            foreach (var type in types)
            {
                if (type.IsPublic)
                {
                    publicTypeNames.Add(type.FullName);
                }
            }

            var expected = new List<string>
            {
                "UglyToad.PdfPig.PdfDocument",
                "UglyToad.PdfPig.ParsingOptions",
                "UglyToad.PdfPig.Logging.ILog",
                "UglyToad.PdfPig.Geometry.PdfPoint",
                "UglyToad.PdfPig.Geometry.PdfRectangle",
                "UglyToad.PdfPig.Fonts.Exceptions.InvalidFontFormatException",
                "UglyToad.PdfPig.Exceptions.PdfDocumentFormatException",
                "UglyToad.PdfPig.Content.Letter",
                "UglyToad.PdfPig.Content.Page",
                "UglyToad.PdfPig.Content.PageSize",
                "UglyToad.PdfPig.Content.DocumentInformation"
            };

            Assert.Equal(expected.OrderBy(x => x), publicTypeNames.OrderBy(x => x));
        }
    }
}
