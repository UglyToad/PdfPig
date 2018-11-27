namespace UglyToad.PdfPig.Tests.Writer
{
    using System;
    using System.IO;
    using Content;
    using PdfPig.Geometry;
    using PdfPig.Writer;
    using Xunit;

    public class PdfDocumentBuilderTests
    {
        [Fact]
        public void CanWriteSinglePageHelloWorld()
        {
            var builder = new PdfDocumentBuilder();

            var page = builder.AddPage(PageSize.A4);

            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Fonts", "TrueType");
            var file = Path.Combine(path, "Andada-Regular.ttf");

            var font = builder.AddTrueTypeFont(File.ReadAllBytes(file));

            page.AddText("Hello World!", 12, new PdfPoint(30, 50), font);

            Assert.NotEmpty(page.Operations);

            var b = builder.Build();

            Assert.NotEmpty(b);
        }
    }
}
