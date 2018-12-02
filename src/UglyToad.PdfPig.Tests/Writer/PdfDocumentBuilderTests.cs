namespace UglyToad.PdfPig.Tests.Writer
{
    using System;
    using System.IO;
    using System.Linq;
    using Content;
    using PdfPig.Fonts;
    using PdfPig.Geometry;
    using PdfPig.Util;
    using PdfPig.Writer;
    using Xunit;

    public class PdfDocumentBuilderTests
    {
        [Fact]
        public void CanWriteSingleBlankPage()
        {
            var result = CreateSingleBlankPage();

            Assert.NotEmpty(result);

            var str = OtherEncodings.BytesAsLatin1String(result);
            Assert.StartsWith("%PDF", str);
            Assert.EndsWith("%%EOF", str);
        }

        [Fact]
        public void CanReadSingleBlankPage()
        {
            var result = CreateSingleBlankPage();

            using (var document = PdfDocument.Open(result, new ParsingOptions { UseLenientParsing = false }))
            {
                Assert.Equal(1, document.NumberOfPages);

                var page = document.GetPage(1);

                Assert.Equal(PageSize.A4, page.Size);

                Assert.Empty(page.Letters);

                Assert.NotNull(document.Structure.Catalog);

                foreach (var offset in document.Structure.CrossReferenceTable.ObjectOffsets)
                {
                    var obj = document.Structure.GetObject(offset.Key);

                    Assert.NotNull(obj);
                }
            }
        }

        private static byte[] CreateSingleBlankPage()
        {
            var builder = new PdfDocumentBuilder();

            builder.AddPage(PageSize.A4);

            var result = builder.Build();

            return result;
        }

        [Fact]
        public void CanWriteSinglePageStandard14FontHelloWorld()
        {
            var builder = new PdfDocumentBuilder();

            var page = builder.AddPage(PageSize.A4);

            var font = builder.AddStandard14Font(Standard14Font.Helvetica);

            page.AddText("Hello World!", 12, new PdfPoint(25, 520), font);

            var b = builder.Build();

            using (var document = PdfDocument.Open(b))
            {
                var page1 = document.GetPage(1);

                Assert.Equal(new[] {"Hello", "World!"}, page1.GetWords().Select(x => x.Text));
            }
        }

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
