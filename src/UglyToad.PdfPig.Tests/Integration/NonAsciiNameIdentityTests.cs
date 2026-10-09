namespace UglyToad.PdfPig.Tests.Integration
{
    using System.Globalization;
    using System.Text;
    using PdfPig.Outline;
    using PdfPig.Writer;

    public class NonAsciiNameIdentityTests
    {
        private static byte[] Pdf(params string[] objects)
        {
            using var output = new MemoryStream();
            void Write(string text)
            {
                var bytes = Encoding.ASCII.GetBytes(text);
                output.Write(bytes, 0, bytes.Length);
            }
            Write("%PDF-1.7\n");
            var offsets = new List<long> { 0 };
            for (var i = 0; i < objects.Length; i++)
            {
                offsets.Add(output.Position);
                Write($"{i + 1} 0 obj\n{objects[i]}\nendobj\n");
            }
            var xref = output.Position;
            Write($"xref\n0 {offsets.Count}\n0000000000 65535 f \n");
            foreach (var offset in offsets.Skip(1))
            {
                Write(offset.ToString("D10", CultureInfo.InvariantCulture) + " 00000 n \n");
            }
            Write($"trailer\n<< /Size {offsets.Count} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF\n");
            return output.ToArray();
        }

        [Fact]
        public void NameDictionaryDestinationsWithEqualTextRemainDistinct()
        {
            var bytes = Pdf(
                "<< /Type /Catalog /Pages 2 0 R /Outlines 4 0 R /Dests << /#C3#A9 [3 0 R /Fit] /#E9 [6 0 R /Fit] >> >>",
                "<< /Type /Pages /Kids [3 0 R 6 0 R] /Count 2 >>",
                "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 100 100] >>",
                "<< /Type /Outlines /First 5 0 R /Last 7 0 R /Count 2 >>",
                "<< /Title (UTF8) /Parent 4 0 R /Next 7 0 R /Dest /#C3#A9 >>",
                "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 100 100] >>",
                "<< /Title (Latin1) /Parent 4 0 R /Prev 5 0 R /Dest /#E9 >>");
            using var document = PdfDocument.Open(bytes);
            Assert.True(document.TryGetBookmarks(out var bookmarks));
            Assert.Equal(new[] { 1, 2 }, bookmarks.GetNodes().OfType<DocumentBookmarkNode>()
                .Select(n => n.Destination.PageNumber).ToArray());
        }

        [Theory]
        [InlineData("/#C3#A9", "<E9>")]
        [InlineData("/#E5#AE#8B#E4#BD#93", "<FEFF5B8B4F53>")]
        [InlineData("<E9>", "<E9>")]
        public void NameTreeDestinationsUseDecodedText(string reference, string key)
        {
            var bytes = Pdf(
                $"<< /Type /Catalog /Pages 2 0 R /Outlines 4 0 R /Names << /Dests << /Names [{key} [3 0 R /Fit]] >> >> >>",
                "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
                "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 100 100] >>",
                "<< /Type /Outlines /First 5 0 R /Last 5 0 R >>",
                $"<< /Title (Target) /Parent 4 0 R /Dest {reference} >>");
            using var document = PdfDocument.Open(bytes);
            Assert.True(document.TryGetBookmarks(out var bookmarks));
            Assert.Equal(1, Assert.Single(bookmarks.GetNodes().OfType<DocumentBookmarkNode>()).Destination.PageNumber);
        }

        [Fact]
        public void LetterFontNameRemainsReadableUtf8Text()
        {
            const string content = "BT /F1 12 Tf 10 50 Td (A) Tj ET";
            var bytes = Pdf(
                "<< /Type /Catalog /Pages 2 0 R >>",
                "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
                "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 100 100] /Resources << /Font << /F1 5 0 R >> >> /Contents 4 0 R >>",
                $"<< /Length {content.Length} >>\nstream\n{content}\nendstream",
                "<< /Type /Font /Subtype /Type3 /Name /#E5#AE#8B#E4#BD#93 /FontBBox [0 0 500 700] /FontMatrix [.001 0 0 .001 0 0] /FirstChar 65 /LastChar 65 /Widths [500] /Encoding << /Type /Encoding /Differences [65 /A] >> /CharProcs << /A 6 0 R >> >>",
                "<< /Length 20 >>\nstream\n500 0 0 0 500 700 d1\nendstream");
            using var document = PdfDocument.Open(bytes);
            Assert.Equal("\u5b8b\u4f53", Assert.Single(document.GetPage(1).Letters).FontName);
        }

        [Fact]
        public void AnnotationAppearanceStatesKeepByteIdentity()
        {
            var bytes = Pdf(
                "<< /Type /Catalog /Pages 2 0 R >>",
                "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
                "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 100 100] /Annots [4 0 R] >>",
                "<< /Type /Annot /Subtype /Widget /Rect [0 0 10 10] /AS /#C3#A9 /AP << /N << /#C3#A9 5 0 R /#E9 6 0 R >> >> >>",
                "<< /Type /XObject /Subtype /Form /BBox [0 0 10 10] /Length 0 >>\nstream\n\nendstream",
                "<< /Type /XObject /Subtype /Form /BBox [0 0 20 20] /Length 0 >>\nstream\n\nendstream");
            using var document = PdfDocument.Open(bytes);
            var annotation = Assert.Single(document.GetPage(1).GetAnnotations());
            var appearance = Assert.IsType<PdfPig.Annotations.AppearanceStream>(annotation.NormalAppearance);
            Assert.Equal(2, appearance.GetStates.Count);
            Assert.Equal("\u00e9", annotation.AppearanceState);
            var utf8 = PdfPig.Tokens.NameToken.Create("\u00e9");
            var latin1 = PdfPig.Tokens.NameToken.Create(new byte[] { 0xE9 }.AsSpan());
            Assert.Same(utf8, annotation.AppearanceStateName);
            Assert.Contains(utf8, appearance.StateNames);
            Assert.Contains(latin1, appearance.StateNames);
            Assert.Equal(new[] { "\u00e9", "\u00e9" }, appearance.GetStates);
            Assert.Same(appearance.Get(utf8), appearance.Get(annotation.AppearanceStateName));
            Assert.Same(appearance.Get(utf8), appearance.Get(annotation.AppearanceState));
            Assert.NotSame(appearance.Get(utf8), appearance.Get(latin1));
        }

        [Fact]
        public void ResourceNamesSurvivePageCopyAndMerge()
        {
            const string content = "BT /#C3#A9 12 Tf 10 50 Td (A) Tj /#E9 12 Tf (B) Tj ET";
            var bytes = Pdf(
                "<< /Type /Catalog /Pages 2 0 R >>",
                "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
                "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 100 100] /Resources << /Font << /#C3#A9 5 0 R /#E9 6 0 R >> >> /Contents 4 0 R >>",
                $"<< /Length {content.Length} >>\nstream\n{content}\nendstream",
                "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
                "<< /Type /Font /Subtype /Type1 /BaseFont /Courier >>");
            using var source = PdfDocument.Open(bytes);
            var sourceLetters = source.GetPage(1).Letters;
            Assert.Equal(new[] { "Helvetica", "Courier" }, sourceLetters.Select(l => l.FontName).ToArray());
            using var builder = new PdfDocumentBuilder();
            builder.AddPage(source, 1);
            using var copied = PdfDocument.Open(builder.Build());
            Assert.Equal(sourceLetters.Select(l => l.FontName), copied.GetPage(1).Letters.Select(l => l.FontName));
            using var merge = new PdfDocumentBuilder();
            var target = merge.AddPage(source, 1);
            target.CopyFrom(source.GetPage(1));
            using var merged = PdfDocument.Open(merge.Build());
            Assert.Equal(new[] { "Helvetica", "Courier", "Helvetica", "Courier" },
                merged.GetPage(1).Letters.Select(l => l.FontName).ToArray());
        }
    }
}
