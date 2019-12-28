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

            WriteFile(nameof(CanWriteSinglePageHelloWorld), result);

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

            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Fonts", "TrueType");
            var file = Path.Combine(path, "Andada-Regular.ttf");

            var font = builder.AddTrueTypeFont(File.ReadAllBytes(file));

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

                for (int i = 0; i < page1.Letters.Count; i++)
                {
                    var readerLetter = page1.Letters[i];
                    var writerLetter = letters[i];

                    Assert.Equal(readerLetter.Value, writerLetter.Value);
                    Assert.Equal(readerLetter.Location, writerLetter.Location);
                    Assert.Equal(readerLetter.FontSize, writerLetter.FontSize);
                    Assert.Equal(readerLetter.GlyphRectangle.Width, writerLetter.GlyphRectangle.Width);
                    Assert.Equal(readerLetter.GlyphRectangle.Height, writerLetter.GlyphRectangle.Height);
                    Assert.Equal(readerLetter.GlyphRectangle.BottomLeft, writerLetter.GlyphRectangle.BottomLeft);
                }
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

                for (int i = 0; i < letters.Count; i++)
                {
                    var readerLetter = page1.Letters[i];
                    var writerLetter = letters[i];

                    Assert.Equal(readerLetter.Value, writerLetter.Value);
                    Assert.Equal(readerLetter.Location, writerLetter.Location);
                    Assert.Equal(readerLetter.FontSize, writerLetter.FontSize);
                    Assert.Equal(readerLetter.GlyphRectangle.Width, writerLetter.GlyphRectangle.Width);
                    Assert.Equal(readerLetter.GlyphRectangle.Height, writerLetter.GlyphRectangle.Height);
                    Assert.Equal(readerLetter.GlyphRectangle.BottomLeft, writerLetter.GlyphRectangle.BottomLeft);
                }
            }
        }

        [Fact]
        public void CanWriteSinglePageWithAccentedCharacters()
        {
            var builder = new PdfDocumentBuilder();
            var page = builder.AddPage(PageSize.A4);
            
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Fonts", "TrueType");
            var file = Path.Combine(path, "Roboto-Regular.ttf");

            var font = builder.AddTrueTypeFont(File.ReadAllBytes(file));

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

            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Fonts", "TrueType");
            var file = Path.Combine(path, "Roboto-Regular.ttf");

            var font = builder.AddTrueTypeFont(File.ReadAllBytes(file));

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

            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Fonts", "TrueType");
            var file = Path.Combine(path, "Roboto-Regular.ttf");

            var font = builder.AddTrueTypeFont(File.ReadAllBytes(file));

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

        private static void WriteFile(string name, byte[] bytes)
        {
            try
            {
                if (!Directory.Exists("Builder"))
                {
                    Directory.CreateDirectory("Builder");
                }

                var output = Path.Combine("Builder", $"{name}.pdf");

                File.WriteAllBytes(output, bytes);
            }
            catch
            {
                // ignored.
            }
        }
    }
}
