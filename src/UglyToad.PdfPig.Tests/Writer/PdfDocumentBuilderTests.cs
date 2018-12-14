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

            var page = builder.AddPage(PageSize.A4);

            var font = builder.AddStandard14Font(Standard14Font.Helvetica);

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

            page.DrawLine(new PdfPoint(25, 70), new PdfPoint(100, 70), 3);

            page.DrawRectangle(new PdfPoint(30, 100), 250, 100, 0.5m);
            page.DrawRectangle(new PdfPoint(30, 200), 250, 100, 0.5m);

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

            WriteFile(nameof(CanWriteSinglePageWithAccentedCharacters), builder.Build());
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
