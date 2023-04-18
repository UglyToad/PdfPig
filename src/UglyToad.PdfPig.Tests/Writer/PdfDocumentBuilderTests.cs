﻿namespace UglyToad.PdfPig.Tests.Writer
{
    using System.IO;
    using System.Linq;
    using Content;
    using Integration;
    using PdfPig.Core;
    using PdfPig.Fonts.Standard14Fonts;
    using PdfPig.Tokens;
    using PdfPig.Writer;
    using System.Collections.Generic;
    using Tests.Fonts.TrueType;
    using Xunit;
    using System;
    using UglyToad.PdfPig.Graphics.Operations.InlineImages;
    using UglyToad.PdfPig.Outline;
    using UglyToad.PdfPig.Outline.Destinations;

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
        public void CanFastAddPageAndInheritProps()
        {
            var first = IntegrationHelpers.GetDocumentPath("inherited_mediabox.pdf");
            var contents = File.ReadAllBytes(first);


            byte[] results = null;
            using (var existing = PdfDocument.Open(contents, ParsingOptions.LenientParsingOff))
            using (var output = new PdfDocumentBuilder())
            {
                output.AddPage(existing, 1);
                results = output.Build();
            }

            using (var rewritted = PdfDocument.Open(results, ParsingOptions.LenientParsingOff))
            {
                var pg = rewritted.GetPage(1);
                Assert.Equal(200, pg.MediaBox.Bounds.Width);
                Assert.Equal(100, pg.MediaBox.Bounds.Height);
            }
        }

        [Fact]
        public void CanFastAddPageWithStreamSubtype()
        {
            var first = IntegrationHelpers.GetDocumentPath("steam_in_page_dict.pdf");
            var contents = File.ReadAllBytes(first);


            byte[] results = null;
            using (var existing = PdfDocument.Open(contents, ParsingOptions.LenientParsingOff))
            using (var output = new PdfDocumentBuilder())
            {
                output.AddPage(existing, 1);
                results = output.Build();
            }

            using (var rewritted = PdfDocument.Open(results, ParsingOptions.LenientParsingOff))
            {
                // really just checking for no exception...
                var pg = rewritted.GetPage(1);
                Assert.NotNull(pg.Content);
            }
        }

        [Fact]
        public void CanFastAddPageAndStripLinkAnnots()
        {
            var first = IntegrationHelpers.GetDocumentPath("outline.pdf");
            var contents = File.ReadAllBytes(first);

            var annotCount = 0;
            byte[] results = null;
            using (var existing = PdfDocument.Open(contents, ParsingOptions.LenientParsingOff))
            using (var output = new PdfDocumentBuilder())
            {
                output.AddPage(existing, 1);
                results = output.Build();
                var pg = existing.GetPage(1);
                var annots = pg.ExperimentalAccess.GetAnnotations().ToList();
                annotCount = annots.Count;
                Assert.Contains(annots, x => x.Type == Annotations.AnnotationType.Link);
            }

            using (var rewritten = PdfDocument.Open(results, ParsingOptions.LenientParsingOff))
            {
                var pg = rewritten.GetPage(1);
                var annots = pg.ExperimentalAccess.GetAnnotations().ToList();
                Assert.Equal(annotCount - 1, annots.Count);
                Assert.DoesNotContain(annots, x => x.Type == Annotations.AnnotationType.Link);
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

                Assert.Equal(new[] { "Hello", "World!" }, page1.GetWords().Select(x => x.Text));
            }
        }

        [Fact]
        public void CanWriteSinglePageInvisibleHelloWorld()
        {
            var builder = new PdfDocumentBuilder();

            PdfPageBuilder page = builder.AddPage(PageSize.A4);

            PdfDocumentBuilder.AddedFont font = builder.AddStandard14Font(Standard14Font.Helvetica);

            page.SetTextRenderingMode(TextRenderingMode.Neither);

            page.AddText("Hello World!", 12, new PdfPoint(25, 520), font);

            var b = builder.Build();

            WriteFile(nameof(CanWriteSinglePageInvisibleHelloWorld), b);

            using (var document = PdfDocument.Open(b))
            {
                var page1 = document.GetPage(1);

                Assert.Equal(new[] { "Hello", "World!" }, page1.GetWords().Select(x => x.Text));
            }
        }

        [Fact]
        public void CanWriteSinglePageMixedRenderingMode()
        {
            var builder = new PdfDocumentBuilder();

            PdfPageBuilder page = builder.AddPage(PageSize.A4);

            PdfDocumentBuilder.AddedFont font = builder.AddStandard14Font(Standard14Font.Helvetica);

            page.AddText("Hello World!", 12, new PdfPoint(25, 520), font);

            page.SetTextRenderingMode(TextRenderingMode.Neither);

            page.AddText("Invisible!", 12, new PdfPoint(25, 500), font);

            page.SetTextRenderingMode(TextRenderingMode.Fill);

            page.AddText("Filled again!", 12, new PdfPoint(25, 480), font);

            var b = builder.Build();

            WriteFile(nameof(CanWriteSinglePageMixedRenderingMode), b);

            using (var document = PdfDocument.Open(b))
            {
                var page1 = document.GetPage(1);

                Assert.Equal(new[] { "Hello", "World!", "Invisible!", "Filled", "again!" }, page1.GetWords().Select(x => x.Text));
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

            Assert.NotEmpty(page.CurrentStream.Operations);

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

            Assert.NotEmpty(page.CurrentStream.Operations);

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

            Assert.NotEmpty(page.CurrentStream.Operations);

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

            Assert.NotEmpty(page.CurrentStream.Operations);

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
        [InlineData(PdfAStandard.A3B)]
        [InlineData(PdfAStandard.A3A)]
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

        [Fact]
        public void CanCreateDocumentWithFilledRectangle()
        {
            var builder = new PdfDocumentBuilder();
            var page = builder.AddPage(PageSize.A4);

            page.SetTextAndFillColor(255, 0, 0);
            page.SetStrokeColor(0, 0, 255);

            page.DrawRectangle(new PdfPoint(20, 100), 200, 100, 1.5m, true);

            var file = builder.Build();
            WriteFile(nameof(CanCreateDocumentWithFilledRectangle), file);
        }

        [Fact]
        public void CanGeneratePageWithMultipleStream()
        {
            var builder = new PdfDocumentBuilder();

            var page = builder.AddPage(PageSize.A4);

            var file = TrueTypeTestHelper.GetFileBytes("Andada-Regular.ttf");

            var font = builder.AddTrueTypeFont(file);

            var letters = page.AddText("Hello", 12, new PdfPoint(30, 50), font);

            Assert.NotEmpty(page.CurrentStream.Operations);

            page.NewContentStreamAfter();

            page.AddText("World!", 12, new PdfPoint(50, 50), font);

            Assert.NotEmpty(page.CurrentStream.Operations);


            var b = builder.Build();

            WriteFile(nameof(CanGeneratePageWithMultipleStream), b);

            Assert.NotEmpty(b);

            using (var document = PdfDocument.Open(b))
            {
                var page1 = document.GetPage(1);

                Assert.Equal("HelloWorld!", page1.Text);

                var h = page1.Letters[0];

                Assert.Equal("H", h.Value);
                Assert.Equal("Andada-Regular", h.FontName);
            }
        }

        [Fact]
        public void CanCopyPage()
        {

            byte[] b;
            {
                var builder = new PdfDocumentBuilder();

                var page1 = builder.AddPage(PageSize.A4);

                var file = TrueTypeTestHelper.GetFileBytes("Andada-Regular.ttf");

                var font = builder.AddTrueTypeFont(file);

                page1.AddText("Hello", 12, new PdfPoint(30, 50), font);

                Assert.NotEmpty(page1.CurrentStream.Operations);


                using (var readDocument = PdfDocument.Open(IntegrationHelpers.GetDocumentPath("bold-italic.pdf")))
                {
                    var rpage = readDocument.GetPage(1);

                    var page2 = builder.AddPage(PageSize.A4);
                    page2.CopyFrom(rpage);
                }

                b = builder.Build();
                Assert.NotEmpty(b);
            }

            WriteFile(nameof(CanCopyPage), b);

            using (var document = PdfDocument.Open(b))
            {
                Assert.Equal(2, document.NumberOfPages);

                var page1 = document.GetPage(1);

                Assert.Equal("Hello", page1.Text);

                var page2 = document.GetPage(2);

                Assert.Equal("Lorem ipsum dolor sit amet, consectetur adipiscing elit. ", page2.Text);
            }
        }

        [Fact]
        public void CanAddHelloWorldToSimplePage()
        {
            var path = IntegrationHelpers.GetDocumentPath("Single Page Simple - from open office.pdf");
            var doc = PdfDocument.Open(path);
            var builder = new PdfDocumentBuilder();

            var page = builder.AddPage(doc, 1);

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

            Assert.NotEmpty(page.CurrentStream.Operations);

            var b = builder.Build();

            WriteFile(nameof(CanAddHelloWorldToSimplePage), b);

            Assert.NotEmpty(b);

            using (var document = PdfDocument.Open(b))
            {
                var page1 = document.GetPage(1);

                Assert.Equal("I am a simple pdf.Hello World!", page1.Text);

                var h = page1.Letters[18];

                Assert.Equal("H", h.Value);
                Assert.Equal("Andada-Regular", h.FontName);

                var comparer = new DoubleComparer(0.01);
                var pointComparer = new PointComparer(comparer);

                for (int i = 0; i < letters.Count; i++)
                {
                    var readerLetter = page1.Letters[i + 18];
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
        public void CanMerge2SimpleDocumentsReversed_Builder()
        {
            var one = IntegrationHelpers.GetDocumentPath("Single Page Simple - from open office.pdf");
            var two = IntegrationHelpers.GetDocumentPath("Single Page Simple - from inkscape.pdf");


            using (var docOne = PdfDocument.Open(one))
            using (var docTwo = PdfDocument.Open(two))
            {
                var builder = new PdfDocumentBuilder();
                builder.AddPage(docOne, 1);
                builder.AddPage(docTwo, 1);
                var result = builder.Build();
                PdfMergerTests.CanMerge2SimpleDocumentsAssertions(new MemoryStream(result), "I am a simple pdf.", "Write something inInkscape", false);
            }
        }

        [Fact]
        public void CanMerge2SimpleDocuments_Builder()
        {
            var one = IntegrationHelpers.GetDocumentPath("Single Page Simple - from inkscape.pdf");
            var two = IntegrationHelpers.GetDocumentPath("Single Page Simple - from open office.pdf");

            using (var docOne = PdfDocument.Open(one))
            using (var docTwo = PdfDocument.Open(two))
            using (var builder = new PdfDocumentBuilder())
            {

                builder.AddPage(docOne, 1);
                builder.AddPage(docTwo, 1);
                var result = builder.Build();
                PdfMergerTests.CanMerge2SimpleDocumentsAssertions(new MemoryStream(result), "Write something inInkscape", "I am a simple pdf.", false);
            }


        }

        [Fact]
        public void CanDedupObjectsFromSameDoc_Builder()
        {
            var one = IntegrationHelpers.GetDocumentPath("Multiple Page - from Mortality Statistics.pdf");

            using (var doc = PdfDocument.Open(one))
            {
                var builder = new PdfDocumentBuilder();
                builder.AddPage(doc, 1);
                builder.AddPage(doc, 1);

                var result = builder.Build();

                using (var document = PdfDocument.Open(result, ParsingOptions.LenientParsingOff))
                {
                    Assert.Equal(2, document.NumberOfPages);
                    Assert.True(document.Structure.CrossReferenceTable.ObjectOffsets.Count <= 29,
                        "Expected object count to be lower than 30"); // 45 objects with duplicates, 29 with correct re-use
                }
            }
        }

        [Fact]
        public void CanDedupObjectsFromDifferentDoc_HashBuilder()
        {
            var one = IntegrationHelpers.GetDocumentPath("Multiple Page - from Mortality Statistics.pdf");
            using (var doc = PdfDocument.Open(one))
            using (var doc2 = PdfDocument.Open(one))
            using (var builder = new PdfDocumentBuilder(new MemoryStream(), true, PdfWriterType.ObjectInMemoryDedup))
            {
                builder.AddPage(doc, 1);
                builder.AddPage(doc2, 1);

                var result = builder.Build();

                using (var document = PdfDocument.Open(result, ParsingOptions.LenientParsingOff))
                {
                    Assert.Equal(2, document.NumberOfPages);
                    Assert.True(document.Structure.CrossReferenceTable.ObjectOffsets.Count <= 29,
                        "Expected object count to be lower than 30"); // 45 objects with duplicates, 29 with correct re-use
                }
            }
        }

        [Fact]
        public void CanCreatePageTree()
        {
            var count = 25 * 25 * 25 + 1;
            using (var builder = new PdfDocumentBuilder())
            {
                for (var i = 0; i < count; i++)
                {
                    builder.AddPage(PageSize.A4);
                }
                var result = builder.Build();
                WriteFile(nameof(CanCreatePageTree), result);

                using (var document = PdfDocument.Open(result, ParsingOptions.LenientParsingOff))
                {
                    Assert.Equal(count, document.NumberOfPages);
                }
            }
        }

        [Fact]
        public void CanWriteEmptyContentStream()
        {
            using (var builder = new PdfDocumentBuilder())
            {
                builder.AddPage(PageSize.A4);
                var result = builder.Build();
                using (var document = PdfDocument.Open(result, ParsingOptions.LenientParsingOff))
                {
                    Assert.Equal(1, document.NumberOfPages);
                    var pg = document.GetPage(1);
                    // single empty page should result in single content stream
                    Assert.NotNull(pg.Dictionary.Data[NameToken.Contents] as IndirectReferenceToken);
                }
            }
        }

        [Fact]
        public void CanWriteSingleContentStream()
        {
            using (var builder = new PdfDocumentBuilder())
            {
                var pb = builder.AddPage(PageSize.A4);
                pb.DrawLine(new PdfPoint(1, 1), new PdfPoint(2, 2));
                var result = builder.Build();
                using (var document = PdfDocument.Open(result, ParsingOptions.LenientParsingOff))
                {
                    Assert.Equal(1, document.NumberOfPages);
                    var pg = document.GetPage(1);
                    // single empty page should result in single content stream
                    Assert.NotNull(pg.Dictionary.Data[NameToken.Contents] as IndirectReferenceToken);
                }
            }
        }

        [Fact]
        public void CanWriteAndIgnoreEmptyContentStream()
        {
            using (var builder = new PdfDocumentBuilder())
            {
                var pb = builder.AddPage(PageSize.A4);
                pb.DrawLine(new PdfPoint(1, 1), new PdfPoint(2, 2));
                pb.NewContentStreamAfter();
                var result = builder.Build();
                using (var document = PdfDocument.Open(result, ParsingOptions.LenientParsingOff))
                {
                    Assert.Equal(1, document.NumberOfPages);
                    var pg = document.GetPage(1);
                    // empty stream should be ignored and resulting single stream should be written
                    Assert.NotNull(pg.Dictionary.Data[NameToken.Contents] as IndirectReferenceToken);
                }
            }
        }

        [Fact]
        public void CanWriteMultipleContentStream()
        {
            using (var builder = new PdfDocumentBuilder())
            {
                var pb = builder.AddPage(PageSize.A4);
                pb.DrawLine(new PdfPoint(1, 1), new PdfPoint(2, 2));
                pb.NewContentStreamAfter();
                pb.DrawLine(new PdfPoint(1, 1), new PdfPoint(2, 2));
                var result = builder.Build();
                using (var document = PdfDocument.Open(result, ParsingOptions.LenientParsingOff))
                {
                    Assert.Equal(1, document.NumberOfPages);
                    var pg = document.GetPage(1);
                    // multiple streams should be written to array
                    var streams = pg.Dictionary.Data[NameToken.Contents] as ArrayToken;
                    Assert.NotNull(streams);
                    Assert.Equal(2, streams.Length);
                }
            }
        }

        [InlineData("Single Page Simple - from google drive.pdf")]
        [InlineData("Old Gutnish Internet Explorer.pdf")]
        [InlineData("68-1990-01_A.pdf")]
        [InlineData("Multiple Page - from Mortality Statistics.pdf")]
        [Theory]
        public void CopiedPagesResultInSameData(string name)
        {
            var docPath = IntegrationHelpers.GetDocumentPath(name);

            using (var doc = PdfDocument.Open(docPath, ParsingOptions.LenientParsingOff))
            using (var builder = new PdfDocumentBuilder())
            {
                var count1 = GetCounts(doc);

                for (var i = 1; i <= doc.NumberOfPages; i++)
                {
                    builder.AddPage(doc, i);
                }
                var result = builder.Build();
                WriteFile(nameof(CopiedPagesResultInSameData) + "_" + name, result);

                using (var doc2 = PdfDocument.Open(result, ParsingOptions.LenientParsingOff))
                {
                    var count2 = GetCounts(doc2);
                    Assert.Equal(count1.Item1, count2.Item1);
                    Assert.Equal(count1.Item2, count2.Item2);
                }
            }

            (int, double) GetCounts(PdfDocument toCount)
            {
                int letters = 0;
                double location = 0;
                foreach (var page in toCount.GetPages())
                {
                    foreach (var letter in page.Letters)
                    {
                        unchecked { letters += 1; }
                        unchecked
                        {
                            location += letter.Location.X;
                            location += letter.Location.Y;
                            location += letter.Font.Name.Length;
                        }
                    }
                }

                return (letters, location);
            }
        }

        [Fact]
        public void CanUseCustomTokenWriter()
        {
            var docPath = IntegrationHelpers.GetDocumentPath("68-1990-01_A.pdf");
            var tw = new TestTokenWriter();

            using (var doc = PdfDocument.Open(docPath))
            using (var ms = new MemoryStream())
            using (var builder = new PdfDocumentBuilder(ms, tokenWriter: tw))
            {
                for (var i = 1; i <= doc.NumberOfPages; i++)
                {
                    builder.AddPage(doc, i);
                }

                builder.Build();
            }

            Assert.Equal(0, tw.Objects); // No objects in sample file
            Assert.True(tw.Tokens > 1000); // Roughly 1065
            Assert.True(tw.WroteCrossReferenceTable);
        }

        [Fact]
        public void CanCopyInLineImage()
        {
            var docPath = IntegrationHelpers.GetDocumentPath("ssm2163.pdf");

            using (var docOrig = PdfDocument.Open(docPath))
            {

                // Copy original document with inline images into pdf bytes for opening and checking.
                PdfDocumentBuilder pdfBuilder = new PdfDocumentBuilder();
                var numberOfPages = docOrig.NumberOfPages;
                for (int pageNumber = 1; pageNumber <= numberOfPages; pageNumber++)
                {
                    var sourcePage = docOrig.GetPage(pageNumber);
                    pdfBuilder.AddPage(sourcePage.Width, sourcePage.Height).CopyFrom(sourcePage);
                }
                var pdfBytes = pdfBuilder.Build();


                using (var docCopy = PdfDocument.Open(pdfBytes))
                {
                    var pageNum = 7;
                    var origPage = docOrig.GetPage(pageNum);
                    var copyPage = docCopy.GetPage(pageNum);

                    var opsOrig = origPage.Operations.Where(v => v.Operator == BeginInlineImageData.Symbol).Select(v => (BeginInlineImageData)v).ToArray();
                    var opCopy = copyPage.Operations.Where(v => v.Operator == BeginInlineImageData.Symbol).Select(v => (BeginInlineImageData)v).ToArray();

                    var dictOrig = opCopy.Select(v => v.Dictionary).ToArray();
                    var dictCopy = opCopy.Select(v => v.Dictionary).ToArray();

                    var exampleCopiedDictionary = dictCopy.FirstOrDefault();

                    Assert.NotNull(exampleCopiedDictionary);
                    Assert.True(exampleCopiedDictionary.Count > 0);
                }
            }
        }

        [Fact]
        public void CanCreateDocumentWithOutline()
        {
            var builder = new PdfDocumentBuilder();
            builder.Bookmarks = new Bookmarks(new BookmarkNode[]
            {
                new DocumentBookmarkNode(
                    "1", 0, new ExplicitDestination(1, ExplicitDestinationType.XyzCoordinates, ExplicitDestinationCoordinates.Empty),
                    new[]
                    {
                        new DocumentBookmarkNode("1.1", 0, new ExplicitDestination(2, ExplicitDestinationType.FitPage, ExplicitDestinationCoordinates.Empty), Array.Empty<BookmarkNode>()),
                    }),
                new DocumentBookmarkNode(
                    "2", 0, new ExplicitDestination(3, ExplicitDestinationType.FitRectangle, ExplicitDestinationCoordinates.Empty),
                    new[]
                    {
                        new DocumentBookmarkNode("2.1", 0, new ExplicitDestination(4, ExplicitDestinationType.FitBoundingBox, ExplicitDestinationCoordinates.Empty), Array.Empty<BookmarkNode>()),
                        new DocumentBookmarkNode("2.2", 0, new ExplicitDestination(5, ExplicitDestinationType.FitBoundingBoxHorizontally, ExplicitDestinationCoordinates.Empty), Array.Empty<BookmarkNode>()),
                        new DocumentBookmarkNode("2.3", 0, new ExplicitDestination(6, ExplicitDestinationType.FitBoundingBoxVertically, ExplicitDestinationCoordinates.Empty), Array.Empty<BookmarkNode>()),
                        new DocumentBookmarkNode("2.4", 0, new ExplicitDestination(7, ExplicitDestinationType.FitHorizontally, ExplicitDestinationCoordinates.Empty), Array.Empty<BookmarkNode>()),
                        new DocumentBookmarkNode("2.5", 0, new ExplicitDestination(8, ExplicitDestinationType.FitVertically, ExplicitDestinationCoordinates.Empty), Array.Empty<BookmarkNode>()),
                    }),
                new UriBookmarkNode("3", 0, "https://github.com", Array.Empty<BookmarkNode>()),
            });

            var font = builder.AddStandard14Font(Standard14Font.Helvetica);

            foreach (var node in builder.Bookmarks.GetNodes())
            {
                builder.AddPage(PageSize.A4).AddText(node.Title, 12, new PdfPoint(25, 800), font);
            }

            var file = builder.Build();
            WriteFile(nameof(CanCreateDocumentWithOutline), file);
            using (var document = PdfDocument.Open(file))
            {
                Assert.True(document.TryGetBookmarks(out var bookmarks));

                Assert.Equal(
                    new[] { "1", "1.1", "2", "2.1", "2.2", "2.3", "2.4", "2.5", "3" },
                    bookmarks.GetNodes().Select(node => node.Title));

                Assert.Equal(
                    new[] { 0, 1, 0, 1, 1, 1, 1, 1, 0 },
                    bookmarks.GetNodes().Select(node => node.Level));

                Assert.Equal(
                    new[] { false, true, false, true, true, true, true, true, true },
                    bookmarks.GetNodes().Select(node => node.IsLeaf));

                Assert.Equal(
                    new[] { "https://github.com" },
                    bookmarks.GetNodes().OfType<UriBookmarkNode>().Select(node => node.Uri));

                Assert.Equal(
                    new[] 
                    {
                        ExplicitDestinationType.XyzCoordinates,
                        ExplicitDestinationType.FitPage,
                        ExplicitDestinationType.FitRectangle,
                        ExplicitDestinationType.FitBoundingBox,
                        ExplicitDestinationType.FitBoundingBoxHorizontally,
                        ExplicitDestinationType.FitBoundingBoxVertically,
                        ExplicitDestinationType.FitHorizontally,
                        ExplicitDestinationType.FitVertically,
                    },
                    bookmarks.GetNodes().OfType<DocumentBookmarkNode>().Select(node => node.Destination.Type));
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

    public class TestTokenWriter : ITokenWriter
    {
        public int Tokens { get; private set; }
        public int Objects { get; private set; }
        public bool WroteCrossReferenceTable { get; private set; }

        public void WriteToken(IToken token, Stream outputStream)
        {
            Tokens++;
        }

        public void WriteObject(long objectNumber, int generation, byte[] data, Stream outputStream)
        {
            Objects++;
        }

        public void WriteCrossReferenceTable(IReadOnlyDictionary<IndirectReference, long> objectOffsets,
            IndirectReference catalogToken,
            Stream outputStream,
            IndirectReference? documentInformationReference)
        {
            WroteCrossReferenceTable = true;
        }
    }
}
