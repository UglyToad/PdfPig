namespace UglyToad.PdfPig.Tests.Writer
{
    using System.IO;
    using System.Linq;
    using Content;
    using Integration;
    using PdfPig.Core;
    using PdfPig.Fonts.Standard14Fonts;
    using PdfPig.Writer;
    using Tests.Fonts.TrueType;
    using Xunit;

    public class PdfDocumentBuilderTests
    {
        [Fact]
        public void CanWriteSingleBlankPage()
        {
            var result = CreateSingleBlankPage();

            WriteFile(nameof(CanWriteSinglePageHelloWorld), result);

            Assert.NotEmpty(result);

            var str = OtherEncodings.BytesAsLatin1String(result);
            Assert.StartsWith("%PDF", str);
            Assert.EndsWith("%%EOF", str);
        }

        [Fact]
        public void CanCreateSingleCustomPageSize()
        {
            var builder = new PdfDocumentBuilder();

            var page = builder.AddPage(120, 250);

            var font = builder.AddStandard14Font(Standard14Font.Helvetica);

            page.AddText("Small page.", 12, new PdfPoint(25, 200), font);

            var bytes = builder.Build();

            WriteFile(nameof(CanCreateSingleCustomPageSize), bytes);

            using (var document = PdfDocument.Open(bytes, ParsingOptions.LenientParsingOff))
            {
                Assert.Equal(1, document.NumberOfPages);

                var page1 = document.GetPage(1);

                Assert.Equal(120, page1.Width);
                Assert.Equal(250, page1.Height);

                Assert.Equal("Small page.", page1.Text);
            }
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

            PdfPageBuilder page = builder.AddPage(PageSize.A4);

            PdfDocumentBuilder.AddedFont font = builder.AddStandard14Font(Standard14Font.Helvetica);
            
            page.AddText("Hello World!", 12, new PdfPoint(25, 520), font);

            var b = builder.Build();

            WriteFile(nameof(CanWriteSinglePageStandard14FontHelloWorld), b);

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

            page.DrawLine(new PdfPoint(30, 520), new PdfPoint(360, 520));
            page.DrawLine(new PdfPoint(360, 520), new PdfPoint(360, 250));

            page.SetStrokeColor(250, 132, 131);
            page.DrawLine(new PdfPoint(25, 70), new PdfPoint(100, 70), 3);
            page.ResetColor();
            page.DrawRectangle(new PdfPoint(30, 200), 250, 100, 0.5m);
            page.DrawRectangle(new PdfPoint(30, 100), 250, 100, 0.5m);

            var file = TrueTypeTestHelper.GetFileBytes("Andada-Regular.ttf");

            var font = builder.AddTrueTypeFont(file);

            var letters = page.AddText("Hello World!", 12, new PdfPoint(30, 50), font);

            Assert.NotEmpty(page.Operations);

            var b = builder.Build();

            WriteFile(nameof(CanWriteSinglePageHelloWorld), b);

            Assert.NotEmpty(b);

            using (var document = PdfDocument.Open(b))
            {
                var page1 = document.GetPage(1);

                Assert.Equal("Hello World!", page1.Text);

                var h = page1.Letters[0];

                Assert.Equal("H", h.Value);
                Assert.Equal("Andada-Regular", h.FontName);

                var comparer = new DoubleComparer(0.01);
                var pointComparer = new PointComparer(comparer);

                for (int i = 0; i < page1.Letters.Count; i++)
                {
                    var readerLetter = page1.Letters[i];
                    var writerLetter = letters[i];

                    Assert.Equal(readerLetter.Value, writerLetter.Value);
                    Assert.Equal(readerLetter.Location, writerLetter.Location, pointComparer);
                    Assert.Equal(readerLetter.FontSize, writerLetter.FontSize, comparer);
                    Assert.Equal(readerLetter.GlyphRectangle.Width, writerLetter.GlyphRectangle.Width, comparer);
                    Assert.Equal(readerLetter.GlyphRectangle.Height, writerLetter.GlyphRectangle.Height, comparer);
                    Assert.Equal(readerLetter.GlyphRectangle.BottomLeft, writerLetter.GlyphRectangle.BottomLeft, pointComparer);
                }
            }
        }

        [Fact]
        public void CanWriteRobotoAccentedCharacters()
        {
            var builder = new PdfDocumentBuilder();

            builder.DocumentInformation.Title = "Hello Roboto!";

            var page = builder.AddPage(PageSize.A4);
            
            var font = builder.AddTrueTypeFont(TrueTypeTestHelper.GetFileBytes("Roboto-Regular.ttf"));

            page.AddText("eé", 12, new PdfPoint(30, 520), font);

            Assert.NotEmpty(page.Operations);

            var b = builder.Build();

            WriteFile(nameof(CanWriteRobotoAccentedCharacters), b);

            Assert.NotEmpty(b);

            using (var document = PdfDocument.Open(b))
            {
                var page1 = document.GetPage(1);

                Assert.Equal("eé", page1.Text);
            }
        }

        [Fact]
        public void WindowsOnlyCanWriteSinglePageAccentedCharactersSystemFont()
        {
            var builder = new PdfDocumentBuilder();

            builder.DocumentInformation.Title = "Hello Windows!";

            var page = builder.AddPage(PageSize.A4);

            var file = @"C:\Windows\Fonts\Calibri.ttf";

            if (!File.Exists(file))
            {
                return;
            }

            byte[] bytes;
            try
            {
                bytes = File.ReadAllBytes(file);
            }
            catch
            {
                return;
            }

            var font = builder.AddTrueTypeFont(bytes);

            page.AddText("eé", 12, new PdfPoint(30, 520), font);

            Assert.NotEmpty(page.Operations);

            var b = builder.Build();

            WriteFile(nameof(WindowsOnlyCanWriteSinglePageAccentedCharactersSystemFont), b);

            Assert.NotEmpty(b);

            using (var document = PdfDocument.Open(b))
            {
                var page1 = document.GetPage(1);

                Assert.Equal("eé", page1.Text);
            }
        }

        [Fact]
        public void WindowsOnlyCanWriteSinglePageHelloWorldSystemFont()
        {
            var builder = new PdfDocumentBuilder();

            builder.DocumentInformation.Title = "Hello Windows!";

            var page = builder.AddPage(PageSize.A4);

            var file = @"C:\Windows\Fonts\BASKVILL.TTF";

            if (!File.Exists(file))
            {
                return;
            }

            byte[] bytes;
            try
            {
                bytes = File.ReadAllBytes(file);
            }
            catch
            {
                return;
            }

            var font = builder.AddTrueTypeFont(bytes);

            var letters = page.AddText("Hello World!", 16, new PdfPoint(30, 520), font);
            page.AddText("This is some further text continuing to write", 12, new PdfPoint(30, 500), font);

            Assert.NotEmpty(page.Operations);

            var b = builder.Build();

            WriteFile(nameof(WindowsOnlyCanWriteSinglePageHelloWorldSystemFont), b);

            Assert.NotEmpty(b);

            using (var document = PdfDocument.Open(b))
            {
                var page1 = document.GetPage(1);

                Assert.StartsWith("Hello World!", page1.Text);

                var h = page1.Letters[0];

                Assert.Equal("H", h.Value);
                Assert.Equal("BaskOldFace", h.FontName);

                var comparer = new DoubleComparer(0.01);
                var pointComparer = new PointComparer(comparer);

                for (int i = 0; i < letters.Count; i++)
                {
                    var readerLetter = page1.Letters[i];
                    var writerLetter = letters[i];

                    Assert.Equal(readerLetter.Value, writerLetter.Value);
                    Assert.Equal(readerLetter.Location, writerLetter.Location, pointComparer);
                    Assert.Equal(readerLetter.FontSize, writerLetter.FontSize, comparer);
                    Assert.Equal(readerLetter.GlyphRectangle.Width, writerLetter.GlyphRectangle.Width, comparer);
                    Assert.Equal(readerLetter.GlyphRectangle.Height, writerLetter.GlyphRectangle.Height, comparer);
                    Assert.Equal(readerLetter.GlyphRectangle.BottomLeft, writerLetter.GlyphRectangle.BottomLeft, pointComparer);
                }
            }
        }

        [Fact]
        public void CanWriteSinglePageWithAccentedCharacters()
        {
            var builder = new PdfDocumentBuilder();
            var page = builder.AddPage(PageSize.A4);
            
            var file = TrueTypeTestHelper.GetFileBytes("Roboto-Regular.ttf");

            var font = builder.AddTrueTypeFont(file);

            page.AddText("é (lower case, upper case É).", 9, 
                new PdfPoint(30, page.PageSize.Height - 50), font);

            var bytes = builder.Build();
            WriteFile(nameof(CanWriteSinglePageWithAccentedCharacters), bytes);

            using (var document = PdfDocument.Open(bytes))
            {
                var page1 = document.GetPage(1);

                Assert.Equal("é (lower case, upper case É).", page1.Text);
            }
        }

        [Fact]
        public void CanWriteTwoPageDocument()
        {
            var builder = new PdfDocumentBuilder();
            var page1 = builder.AddPage(PageSize.A4);
            var page2 = builder.AddPage(PageSize.A4);
            
            var font = builder.AddTrueTypeFont(TrueTypeTestHelper.GetFileBytes("Roboto-Regular.ttf"));

            var topLine = new PdfPoint(30, page1.PageSize.Height - 60);
            var letters = page1.AddText("Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor", 9, topLine, font);
            page1.AddText("incididunt ut labore et dolore magna aliqua.", 9, new PdfPoint(30, topLine.Y - letters.Max(x => x.GlyphRectangle.Height) - 5), font);

            var page2Letters = page2.AddText("The very hungry caterpillar ate all the apples in the garden.", 12, topLine, font);
            var left = (decimal)page2Letters[0].GlyphRectangle.Left;
            var bottom = (decimal)page2Letters.Min(x => x.GlyphRectangle.Bottom);
            var right = (decimal)page2Letters[page2Letters.Count - 1].GlyphRectangle.Right;
            var top = (decimal)page2Letters.Max(x => x.GlyphRectangle.Top);
            page2.SetStrokeColor(10, 250, 69);
            page2.DrawRectangle(new PdfPoint(left, bottom), right - left, top - bottom);

            var bytes = builder.Build();
            WriteFile(nameof(CanWriteTwoPageDocument), bytes);

            using (var document = PdfDocument.Open(bytes))
            {
                var page1Out = document.GetPage(1);

                Assert.StartsWith("Lorem ipsum dolor sit", page1Out.Text);

                var page2Out = document.GetPage(2);

                Assert.StartsWith("The very hungry caterpillar", page2Out.Text);
            }
        }
        
        [Fact]
        public void CanWriteSinglePageWithCzechCharacters()
        {
            var builder = new PdfDocumentBuilder();
            var page = builder.AddPage(PageSize.A4);
            
            var font = builder.AddTrueTypeFont(TrueTypeTestHelper.GetFileBytes("Roboto-Regular.ttf"));

            page.AddText("Hello: řó", 9,
                new PdfPoint(30, page.PageSize.Height - 50), font);

            var bytes = builder.Build();
            WriteFile(nameof(CanWriteSinglePageWithCzechCharacters), bytes);

            using (var document = PdfDocument.Open(bytes))
            {
                var page1 = document.GetPage(1);

                Assert.Equal("Hello: řó", page1.Text);
            }
        }

        [Fact]
        public void CanWriteSinglePageWithJpeg()
        {
            var builder = new PdfDocumentBuilder();
            var page = builder.AddPage(PageSize.A4);

            var font = builder.AddStandard14Font(Standard14Font.Helvetica);

            page.AddText("Smile", 12, new PdfPoint(25, page.PageSize.Height - 52), font);

            var img = IntegrationHelpers.GetDocumentPath("smile-250-by-160.jpg", false);

            var expectedBounds = new PdfRectangle(25, page.PageSize.Height - 300, 200, page.PageSize.Height - 200);

            var imageBytes = File.ReadAllBytes(img);

            page.AddJpeg(imageBytes, expectedBounds);
            
            var bytes = builder.Build();
            WriteFile(nameof(CanWriteSinglePageWithJpeg), bytes);

            using (var document = PdfDocument.Open(bytes))
            {
                var page1 = document.GetPage(1);

                Assert.Equal("Smile", page1.Text);

                var image = Assert.Single(page1.GetImages());

                Assert.NotNull(image);

                Assert.Equal(expectedBounds.BottomLeft, image.Bounds.BottomLeft);
                Assert.Equal(expectedBounds.TopRight, image.Bounds.TopRight);

                Assert.Equal(imageBytes, image.RawBytes);
            }
        }

        [Fact]
        public void CanWrite2PagesSharingJpeg()
        {
            var builder = new PdfDocumentBuilder();
            var page = builder.AddPage(PageSize.A4);

            var font = builder.AddStandard14Font(Standard14Font.Helvetica);

            page.AddText("Smile", 12, new PdfPoint(25, page.PageSize.Height - 52), font);

            var img = IntegrationHelpers.GetDocumentPath("smile-250-by-160.jpg", false);

            var expectedBounds1 = new PdfRectangle(25, page.PageSize.Height - 300, 200, page.PageSize.Height - 200);

            var imageBytes = File.ReadAllBytes(img);

            var expectedBounds2 = new PdfRectangle(25, 600, 75, 650);

            var jpeg = page.AddJpeg(imageBytes, expectedBounds1);
            page.AddJpeg(jpeg, expectedBounds2);

            var expectedBounds3 = new PdfRectangle(30, 500, 130, 550);

            var page2 = builder.AddPage(PageSize.A4);
            page2.AddJpeg(jpeg, expectedBounds3);

            var bytes = builder.Build();
            WriteFile(nameof(CanWrite2PagesSharingJpeg), bytes);

            using (var document = PdfDocument.Open(bytes))
            {
                var page1 = document.GetPage(1);

                Assert.Equal("Smile", page1.Text);

                var page1Images = page1.GetImages().ToList();
                Assert.Equal(2, page1Images.Count);

                var image1 = page1Images[0];
                Assert.Equal(expectedBounds1, image1.Bounds);

                var image2 = page1Images[1];
                Assert.Equal(expectedBounds2, image2.Bounds);

                var page2Doc = document.GetPage(2);

                var image3 = Assert.Single(page2Doc.GetImages());

                Assert.NotNull(image3);

                Assert.Equal(expectedBounds3, image3.Bounds);

                Assert.Equal(imageBytes, image1.RawBytes);
                Assert.Equal(imageBytes, image2.RawBytes);
                Assert.Equal(imageBytes, image3.RawBytes);
            }
        }

        [Theory]
        [InlineData(PdfAStandard.A1B)]
        [InlineData(PdfAStandard.A1A)]
        [InlineData(PdfAStandard.A2B)]
        [InlineData(PdfAStandard.A2A)]
        public void CanGeneratePdfAFile(PdfAStandard standard)
        {
            var builder = new PdfDocumentBuilder
            {
                ArchiveStandard = standard
            };

            var page = builder.AddPage(PageSize.A4);

            var imgBytes = File.ReadAllBytes(IntegrationHelpers.GetDocumentPath("smile-250-by-160.jpg", false));
            page.AddJpeg(imgBytes, new PdfRectangle(50, 70, 150, 130));

            var font = builder.AddTrueTypeFont(TrueTypeTestHelper.GetFileBytes("Roboto-Regular.ttf"));

            page.AddText($"Howdy PDF/{standard}!", 10, new PdfPoint(25, 700), font);

            var bytes = builder.Build();

            WriteFile(nameof(CanGeneratePdfAFile) + standard, bytes);

            using (var pdf = PdfDocument.Open(bytes, ParsingOptions.LenientParsingOff))
            {
                Assert.Equal(1, pdf.NumberOfPages);

                Assert.True(pdf.TryGetXmpMetadata(out var xmp));

                Assert.NotNull(xmp.GetXDocument());
            }
        }

        [Fact]
        public void CanWriteSinglePageWithPng()
        {
            var builder = new PdfDocumentBuilder();
            var page = builder.AddPage(PageSize.A4);

            var font = builder.AddStandard14Font(Standard14Font.Helvetica);

            page.AddText("Piggy", 12, new PdfPoint(25, page.PageSize.Height - 52), font);

            var img = IntegrationHelpers.GetDocumentPath("pdfpig.png", false);

            var expectedBounds = new PdfRectangle(25, page.PageSize.Height - 300, 200, page.PageSize.Height - 200);

            var imageBytes = File.ReadAllBytes(img);

            page.AddPng(imageBytes, expectedBounds);

            var bytes = builder.Build();
            WriteFile(nameof(CanWriteSinglePageWithPng), bytes);

            using (var document = PdfDocument.Open(bytes))
            {
                var page1 = document.GetPage(1);

                Assert.Equal("Piggy", page1.Text);

                var image = Assert.Single(page1.GetImages());

                Assert.NotNull(image);

                Assert.Equal(expectedBounds.BottomLeft, image.Bounds.BottomLeft);
                Assert.Equal(expectedBounds.TopRight, image.Bounds.TopRight);

                Assert.True(image.TryGetPng(out var png));
                Assert.NotNull(png);

                WriteFile(nameof(CanWriteSinglePageWithPng) + "out", png, "png");
            }
        }

        [Fact]
        public void CanCreateDocumentInformationDictionaryWithNonAsciiCharacters()
        {
            const string littlePig = "маленький поросенок";
            var builder = new PdfDocumentBuilder();
            builder.DocumentInformation.Title = littlePig;
            var page = builder.AddPage(PageSize.A4);
            var font = builder.AddTrueTypeFont(TrueTypeTestHelper.GetFileBytes("Roboto-Regular.ttf"));
            page.AddText(littlePig, 12, new PdfPoint(120, 600), font);

            var file = builder.Build();
            WriteFile(nameof(CanCreateDocumentInformationDictionaryWithNonAsciiCharacters), file);
            using (var document = PdfDocument.Open(file))
            {
                Assert.Equal(littlePig, document.Information.Title);
            }
        }

        private static void WriteFile(string name, byte[] bytes, string extension = "pdf")
        {
            try
            {
                if (!Directory.Exists("Builder"))
                {
                    Directory.CreateDirectory("Builder");
                }

                var output = Path.Combine("Builder", $"{name}.{extension}");

                File.WriteAllBytes(output, bytes);
            }
            catch
            {
                // ignored.
            }
        }
    }
}
