namespace UglyToad.PdfPig.Tests.Integration
{
    public class IntegrationDocumentTests
    {
        private static readonly Lazy<string> DocumentFolder = new Lazy<string>(() => Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Integration", "Documents")));
        private static readonly HashSet<string> _documentsToIgnore =
        [
            "issue_671.pdf",
            "GHOSTSCRIPT-698363-0.pdf",
            "ErcotFacts.pdf"
        ];

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

                    Assert.NotNull(page.ExperimentalAccess.GetAnnotations().ToList());
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

                    Assert.NotNull(page.ExperimentalAccess.GetAnnotations().ToList());
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

                Assert.True(document.Structure.CrossReferenceTable.ObjectOffsets.Count > 0, "Cross reference table was empty.");
                foreach (var objectOffset in document.Structure.CrossReferenceTable.ObjectOffsets)
                {
                    var token = document.Structure.GetObject(objectOffset.Key);

                    Assert.NotNull(token);
                }
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

        public static IEnumerable<object[]> GetAllDocuments
        {
            get
            {
                var files = Directory.GetFiles(DocumentFolder.Value, "*.pdf");

                // Return the shortname so we can see it in the test explorer.
                return files.Where(x => !_documentsToIgnore.Any(i => x.EndsWith(i))).Select(x => new object[] { Path.GetFileName(x) });
            }
        }
    }
}
