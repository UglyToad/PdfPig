namespace UglyToad.Pdf.Tests
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
                "UglyToad.Pdf.PdfDocument",
                "UglyToad.Pdf.ParsingOptions",
                "UglyToad.Pdf.Logging.ILog",
                "UglyToad.Pdf.Geometry.PdfPoint",
                "UglyToad.Pdf.Fonts.Exceptions.InvalidFontFormatException",
                "UglyToad.Pdf.Exceptions.PdfDocumentFormatException",
                "UglyToad.Pdf.Content.Letter",
                "UglyToad.Pdf.Content.Page",
                "UglyToad.Pdf.Content.PageSize",
                "UglyToad.Pdf.Content.DocumentInformation"
            };

            Assert.Equal(expected.OrderBy(x => x), publicTypeNames.OrderBy(x => x));
        }
    }
}
