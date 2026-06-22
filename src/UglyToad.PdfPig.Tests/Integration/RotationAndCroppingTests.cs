namespace UglyToad.PdfPig.Tests.Integration
{
    public class RotationAndCroppingTests
    {
        [Fact]
        public void CroppedPageHasCorrectTextCoordinates()
        {
            var path = IntegrationHelpers.GetDocumentPath("SPARC - v9 Architecture Manual");

            using (var document = PdfDocument.Open(path))
            {
                var page = document.GetPage(1);
                Assert.Equal(612, page.Width);  // Due to cropping
                Assert.Equal(792, page.Height); // Due to cropping
                var minX = page.Letters.Select(l => l.BoundingBox.Left).Min();
                var maxX = page.Letters.Select(l => l.BoundingBox.Right).Max();
                Assert.Equal(74, minX, 0);  // If cropping is not applied correctly, these values will be off
                Assert.Equal(540, maxX, 0); // If cropping is not applied correctly, these values will be off
                // The page is cropped at 
                Assert.NotNull(page.Content);
            }
        }

        [Fact]
        public void WrongPathCount()
        {
            using (var document = PdfDocument.Open(IntegrationHelpers.GetDocumentPath("Publication_of_award_of_Bids_for_Transport_Sector__August_2016.pdf"),
                new ParsingOptions()
                {
                    ClipPaths = true
                }))
            {
                var page = document.GetPage(1);
                Assert.Equal(612, page.Height);
                Assert.Equal(224, page.Paths.Count);
            }
        }

        [Fact]
        public void Issue665()
        {
            using (var document = PdfDocument.Open(IntegrationHelpers.GetDocumentPath("SmallCropbox.pdf")))
            {
                var page = document.GetPage(1);
                Assert.Equal(270, page.Rotation.Value); // Clockwise
                Assert.Equal(680, (int)page.Height);
                Assert.Equal(433, (int)page.Width);
                Assert.Equal(Content.PageSize.Custom, page.Size);
                Assert.Equal(2429, page.Letters.Count);

                // The media box and crop box are reported in unrotated default user space, as defined
                // in the document. The page is rotated 270 degrees clockwise, so the visible page
                // dimensions (Width/Height above) have their width and height swapped relative to the boxes.
                var cropBox = page.CropBox.Bounds;
                Assert.Equal(0, cropBox.Rotation);
                Assert.Equal(433, (int)cropBox.Height);
                Assert.Equal(680, (int)cropBox.Width);
                Assert.Equal(0, (int)cropBox.Bottom);
                Assert.Equal(0, (int)cropBox.Left);
                Assert.Equal(680, (int)cropBox.Right);
                Assert.Equal(433, (int)cropBox.Top);

                var mediaBox = page.MediaBox.Bounds;
                Assert.Equal(0, mediaBox.Rotation);
                Assert.Equal(433, (int)mediaBox.Height);
                Assert.Equal(680, (int)mediaBox.Width);
                Assert.Equal(0, (int)mediaBox.Bottom);
                Assert.Equal(0, (int)mediaBox.Left);
                Assert.Equal(680, (int)mediaBox.Right);
                Assert.Equal(433, (int)mediaBox.Top);
            }
        }

        [Fact]
        public void CropBoxExtendingBeyondMediaBoxIsClippedToMediaBox()
        {
            // ISO 32000-2:2020, 14.11.2 "Page boundaries": if the bounds of the crop box extend
            // outside of the bounds of the media box, a processor shall treat the crop box as its
            // intersection with the media box.
            var bytes = BuildSinglePagePdf("/MediaBox [0 0 100 100] /CropBox [10 10 200 200]");

            using (var document = PdfDocument.Open(bytes, new ParsingOptions() { UseLenientParsing = true }))
            {
                var page = document.GetPage(1);

                // Crop box is clipped to the media box on the top/right, unchanged on the bottom/left.
                Assert.Equal(new UglyToad.PdfPig.Core.PdfPoint(10, 10), page.CropBox.Bounds.BottomLeft);
                Assert.Equal(new UglyToad.PdfPig.Core.PdfPoint(100, 100), page.CropBox.Bounds.TopRight);
                Assert.Equal(0, page.CropBox.Bounds.Rotation);

                // Media box is reported unchanged.
                Assert.Equal(new UglyToad.PdfPig.Core.PdfPoint(0, 0), page.MediaBox.Bounds.BottomLeft);
                Assert.Equal(new UglyToad.PdfPig.Core.PdfPoint(100, 100), page.MediaBox.Bounds.TopRight);
            }
        }

        private static byte[] BuildSinglePagePdf(string pageBoxEntries)
        {
            using var ms = new System.IO.MemoryStream();
            var offsets = new long[4];

            void Write(string s)
            {
                var b = System.Text.Encoding.ASCII.GetBytes(s);
                ms.Write(b, 0, b.Length);
            }

            Write("%PDF-1.7\n");

            offsets[1] = ms.Position;
            Write("1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");

            offsets[2] = ms.Position;
            Write("2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n");

            offsets[3] = ms.Position;
            Write($"3 0 obj\n<< /Type /Page /Parent 2 0 R /Resources << >> {pageBoxEntries} >>\nendobj\n");

            var xref = ms.Position;
            Write("xref\n0 4\n");
            Write("0000000000 65535 f \n");
            for (int i = 1; i <= 3; i++)
            {
                Write($"{offsets[i].ToString("D10", System.Globalization.CultureInfo.InvariantCulture)} 00000 n \n");
            }

            Write("trailer\n<< /Size 4 /Root 1 0 R >>\nstartxref\n");
            Write(xref.ToString(System.Globalization.CultureInfo.InvariantCulture));
            Write("\n%%EOF\n");

            return ms.ToArray();
        }
    }
}
