namespace UglyToad.PdfPig.Tests.Integration
{
    using System.Globalization;
    using System.Text;
    using PdfPig.Geometry;

    public class IntegrationDocumentTests
    {
        private static readonly Lazy<string> DocumentFolder = new Lazy<string>(() => Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Integration", "Documents")));
        private static readonly HashSet<string> _documentsToIgnore =
        [
            "issue_671.pdf",
            "GHOSTSCRIPT-698363-0.pdf",
            "ErcotFacts.pdf",
            "cmap-parsing-exception.pdf"
        ];


        [Theory]
        [MemberData(nameof(GetAllDocuments))]
        public void CheckGlyphLooseBoundingBoxes(string documentName)
        {
            // Add the full path back on, we removed it so we could see it in the test explorer.
            documentName = Path.Combine(DocumentFolder.Value, documentName);

            using (var document = PdfDocument.Open(documentName, new ParsingOptions { UseLenientParsing = true }))
            {
                for (var i = 0; i < document.NumberOfPages; i++)
                {
                    var page = document.GetPage(i + 1);
                    foreach (var letter in page.Letters)
                    {
                        var bbox = letter.BoundingBox;
                        if (bbox.Height > 0)
                        {
                            if (letter.GlyphRectangleLoose.Height <= 0)
                            {
                                _ = letter.GetFont().GetAscent();
                            }
                            
                            Assert.True(letter.GlyphRectangleLoose.Height > 0, $"Page {i + 1}");
                        }
                    }
                }
            }
        }

        [Theory]
        [MemberData(nameof(GetAllDocuments))]
        public void CanReadAllPages(string documentName)
        {
            // Add the full path back on, we removed it so we could see it in the test explorer.
            documentName = Path.Combine(DocumentFolder.Value, documentName);

            using (var document = PdfDocument.Open(documentName, new ParsingOptions { UseLenientParsing = false }))
            {
                for (var i = 0; i < document.NumberOfPages; i++)
                {
                    var page = document.GetPage(i + 1);

                    Assert.NotNull(page.GetAnnotations().ToArray());
                }
            }
        }

        [Theory]
        [MemberData(nameof(GetAllDocuments))]
        public void CanUseStreamForFirstPage(string documentName)
        {
            // Add the full path back on, we removed it so we could see it in the test explorer.
            documentName = Path.Combine(DocumentFolder.Value, documentName);

            var bytes = File.ReadAllBytes(documentName);

            using (var memoryStream = new MemoryStream(bytes))
            using (var document = PdfDocument.Open(memoryStream, new ParsingOptions { UseLenientParsing = false }))
            {
                for (var i = 0; i < document.NumberOfPages; i++)
                {
                    var page = document.GetPage(i + 1);

                    Assert.NotNull(page.GetAnnotations().ToArray());
                }
            }
        }

        [Theory]
        [MemberData(nameof(GetAllDocuments))]
        public void CanTokenizeAllAccessibleObjects(string documentName)
        {
            documentName = Path.Combine(DocumentFolder.Value, documentName);

            using (var document = PdfDocument.Open(documentName, new ParsingOptions { UseLenientParsing = false }))
            {
                Assert.NotNull(document.Structure.Catalog);

                //Assert.True(document.Structure.CrossReferenceTable.ObjectOffsets.Count > 0, "Cross reference table was empty.");
                //foreach (var objectOffset in document.Structure.CrossReferenceTable.ObjectOffsets)
                //{
                //    var token = document.Structure.GetObject(objectOffset.Key);

                //    Assert.NotNull(token);
                //}
            }
        }

        [Theory]
        [MemberData(nameof(GetAllDocuments))]
        public void CanAccessImagesOnEveryPage(string documentName)
        {
            documentName = Path.Combine(DocumentFolder.Value, documentName);

            using (var document = PdfDocument.Open(documentName, new ParsingOptions { UseLenientParsing = false }))
            {
                for (var i = 0; i < document.NumberOfPages; i++)
                {
                    var page = document.GetPage(i + 1);

                    var images = page.GetImages();

                    Assert.NotNull(images);

                    foreach (var image in images)
                    {
                        Assert.True(image.WidthInSamples > 0, $"Image had width of zero on page {i + 1}.");
                        Assert.True(image.HeightInSamples > 0, $"Image had height of zero on page {i + 1}.");
                    }
                }
            }
        }

        [SkippableFact]
        public void CanOpenLargeSparseDocumentAndReadPage()
        {
            var path = Path.Combine(Path.GetTempPath(), $"pdfpig-large-{Guid.NewGuid():N}.pdf");

            try
            {
                try
                {
                    CreateSparsePdfWithLargeXrefOffset(path, (long)int.MaxValue + 4096);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
                {
                    Skip.If(true, $"Could not create a temporary sparse large PDF in this environment: {ex.Message}");
                }

                using var document = PdfDocument.Open(path, ParsingOptions.LenientParsingOff);

                Assert.Equal(200, document.NumberOfPages);

                var page = document.GetPage(200);
                var text = string.Concat(page.Letters.Select(x => x.Value));

                Assert.Contains("Sparse page 200 sentinel", text);
            }
            finally
            {
                try
                {
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                    }
                }
                catch
                {
                    // Best-effort cleanup for a temporary sparse file.
                }
            }
        }

        public static IEnumerable<object[]> GetAllDocuments
        {
            get
            {
                var files = Directory.GetFiles(DocumentFolder.Value, "*.pdf");

                // Return the shortname so we can see it in the test explorer.
                return files.Where(x => !_documentsToIgnore.Any(i => x.EndsWith(i))).Select(x => new object[] { Path.GetFileName(x) });
            }
        }

        private static void CreateSparsePdfWithLargeXrefOffset(string path, long xrefOffset)
        {
            using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None);

            const int pageCount = 200;
            const int firstPageObjectNumber = 4;
            const int firstContentObjectNumber = firstPageObjectNumber + pageCount;
            var objectCount = firstContentObjectNumber + pageCount - 1;
            var objectOffsets = new long[objectCount + 1];

            WriteAscii(stream, "%PDF-1.4\n");

            objectOffsets[1] = stream.Position;
            WriteAscii(stream, "1 0 obj\n");
            WriteAscii(stream, "<< /Type /Catalog /Pages 2 0 R >>\n");
            WriteAscii(stream, "endobj\n");

            objectOffsets[2] = stream.Position;
            WriteAscii(stream, "2 0 obj\n");
            WriteAscii(stream, $"<< /Type /Pages /Count {pageCount.ToString(CultureInfo.InvariantCulture)} /Kids [");
            for (var i = 0; i < pageCount; i++)
            {
                WriteAscii(stream, $"{(firstPageObjectNumber + i).ToString(CultureInfo.InvariantCulture)} 0 R ");
            }

            WriteAscii(stream, "] >>\n");
            WriteAscii(stream, "endobj\n");

            objectOffsets[3] = stream.Position;
            WriteAscii(stream, "3 0 obj\n");
            WriteAscii(stream, "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>\n");
            WriteAscii(stream, "endobj\n");

            for (var i = 0; i < pageCount; i++)
            {
                var pageNumber = i + 1;
                var pageObjectNumber = firstPageObjectNumber + i;
                var contentObjectNumber = firstContentObjectNumber + i;

                objectOffsets[pageObjectNumber] = stream.Position;
                WriteAscii(stream, $"{pageObjectNumber.ToString(CultureInfo.InvariantCulture)} 0 obj\n");
                WriteAscii(stream, $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 3 0 R >> >> /Contents {contentObjectNumber.ToString(CultureInfo.InvariantCulture)} 0 R >>\n");
                WriteAscii(stream, "endobj\n");

                var pageText = pageNumber == 200 ? "Sparse page 200 sentinel" : $"Sparse page {pageNumber.ToString(CultureInfo.InvariantCulture)}";
                var content = $"BT /F1 12 Tf 72 720 Td ({pageText}) Tj ET\n";

                objectOffsets[contentObjectNumber] = stream.Position;
                WriteAscii(stream, $"{contentObjectNumber.ToString(CultureInfo.InvariantCulture)} 0 obj\n");
                WriteAscii(stream, $"<< /Length {Encoding.ASCII.GetByteCount(content).ToString(CultureInfo.InvariantCulture)} >>\n");
                WriteAscii(stream, "stream\n");
                WriteAscii(stream, content);
                WriteAscii(stream, "endstream\n");
                WriteAscii(stream, "endobj\n");
            }

            stream.SetLength(xrefOffset);
            stream.Position = xrefOffset;

            WriteAscii(stream, "xref\n");
            WriteAscii(stream, $"0 {(objectCount + 1).ToString(CultureInfo.InvariantCulture)}\n");
            WriteAscii(stream, "0000000000 65535 f \n");
            for (var i = 1; i < objectOffsets.Length; i++)
            {
                WriteAscii(stream, $"{objectOffsets[i].ToString("D10", CultureInfo.InvariantCulture)} 00000 n \n");
            }

            WriteAscii(stream, "trailer\n");
            WriteAscii(stream, $"<< /Size {(objectCount + 1).ToString(CultureInfo.InvariantCulture)} /Root 1 0 R >>\n");
            WriteAscii(stream, "startxref\n");
            WriteAscii(stream, xrefOffset.ToString(CultureInfo.InvariantCulture));
            WriteAscii(stream, "\n%%EOF\n");
        }

        private static void WriteAscii(Stream stream, string value)
        {
            var bytes = Encoding.ASCII.GetBytes(value);
            stream.Write(bytes, 0, bytes.Length);
        }
    }
}
