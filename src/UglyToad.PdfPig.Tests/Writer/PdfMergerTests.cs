namespace UglyToad.PdfPig.Tests.Writer
{
    using Integration;
    using PdfPig.Writer;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Xunit;

    public class PdfMergerTests
    {
        [Fact]
        public void CanMerge2SimpleDocuments()
        {
            var one = IntegrationHelpers.GetDocumentPath("Single Page Simple - from inkscape.pdf");
            var two = IntegrationHelpers.GetDocumentPath("Single Page Simple - from open office.pdf");

            var result = PdfMerger.Merge(one, two);
            CanMerge2SimpleDocumentsAssertions(new MemoryStream(result), "Write something inInkscape", "I am a simple pdf.");
        }

        [Fact]
        public void CanMerge2SimpleDocumentsIntoStream()
        {
            var one = IntegrationHelpers.GetDocumentPath("Single Page Simple - from inkscape.pdf");
            var two = IntegrationHelpers.GetDocumentPath("Single Page Simple - from open office.pdf");

            using (var outputStream = GetSelfDestructingNewFileStream("merge2"))
            {
                if (outputStream is null)
                {
                    return;//we can't create a file in this test session
                }

                PdfMerger.Merge(one, two, outputStream);
                CanMerge2SimpleDocumentsAssertions(outputStream, "Write something inInkscape", "I am a simple pdf.");
            }
        }

        [Fact]
        public void CanMerge2SimpleDocumentsReversed()
        {
            var one = IntegrationHelpers.GetDocumentPath("Single Page Simple - from open office.pdf");
            var two = IntegrationHelpers.GetDocumentPath("Single Page Simple - from inkscape.pdf");

            var result = PdfMerger.Merge(one, two);
            CanMerge2SimpleDocumentsAssertions(new MemoryStream(result), "I am a simple pdf.", "Write something inInkscape");
        }

        private void CanMerge2SimpleDocumentsAssertions(Stream stream, string page1Text, string page2Text)
        {
            stream.Position = 0;
            using (var document = PdfDocument.Open(stream, ParsingOptions.LenientParsingOff))
            {
                Assert.Equal(2, document.NumberOfPages);
                Assert.Equal(1.5m, document.Version);

                var page1 = document.GetPage(1);
                Assert.Equal(page1Text, page1.Text);

                var page2 = document.GetPage(2);
                Assert.Equal(page2Text, page2.Text);
            }
        }

        [Fact]
        public void RootNodePageCount()
        {
            var one = IntegrationHelpers.GetDocumentPath("Single Page Simple - from open office.pdf");
            var two = IntegrationHelpers.GetDocumentPath("Single Page Simple - from inkscape.pdf");

            var result = PdfMerger.Merge(one, two);

            using (var document = PdfDocument.Open(result, ParsingOptions.LenientParsingOff))
            {
                Assert.Equal(2, document.NumberOfPages);
            }

            var oneBytes = File.ReadAllBytes(one);

            var result2 = PdfMerger.Merge(new[] { result, oneBytes });

            using (var document = PdfDocument.Open(result2, ParsingOptions.LenientParsingOff))
            {
                Assert.Equal(3, document.NumberOfPages);
            }
        }

        [Fact]
        public void ObjectCountLower()
        {
            var one = IntegrationHelpers.GetDocumentPath("Single Page Simple - from inkscape.pdf");
            var two = IntegrationHelpers.GetDocumentPath("Single Page Simple - from inkscape.pdf");

            var result = PdfMerger.Merge(one, two);

            using (var document = PdfDocument.Open(result, ParsingOptions.LenientParsingOff))
            {
                Assert.Equal(2, document.NumberOfPages);
                Assert.True(document.Structure.CrossReferenceTable.ObjectOffsets.Count <= 24,
                    "Expected object count to be lower than 24");
            }
        }

        [Fact]
        public void DedupsObjectsFromSameDoc()
        {
            var one = IntegrationHelpers.GetDocumentPath("Multiple Page - from Mortality Statistics.pdf");

            var result = PdfMerger.Merge(new List<byte[]> { File.ReadAllBytes(one) }, new List<IReadOnlyList<int>> { new List<int> { 1, 2}  });

            using (var document = PdfDocument.Open(result, ParsingOptions.LenientParsingOff))
            {
                Assert.Equal(2, document.NumberOfPages);
                Assert.True(document.Structure.CrossReferenceTable.ObjectOffsets.Count <= 29,
                    "Expected object count to be lower than 30"); // 45 objects with duplicates, 29 with correct re-use
            }
        }

        [Fact]
        public void CanMergeWithObjectStream()
        {
            var first = IntegrationHelpers.GetDocumentPath("Single Page Simple - from google drive.pdf");
            var second = IntegrationHelpers.GetDocumentPath("Multiple Page - from Mortality Statistics.pdf");

            var result = PdfMerger.Merge(first, second);

            WriteFile(nameof(CanMergeWithObjectStream), result);

            using (var document = PdfDocument.Open(result, ParsingOptions.LenientParsingOff))
            {
                Assert.Equal(7, document.NumberOfPages);

                foreach (var page in document.GetPages())
                {
                    Assert.NotNull(page.Text);
                }
            }
        }

        [Fact]
        public void CanMergeWithSelection()
        {
            var first = IntegrationHelpers.GetDocumentPath("Multiple Page - from Mortality Statistics.pdf");
            var result = PdfMerger.Merge(new [] { File.ReadAllBytes(first) }, new [] { new[] {2, 1, 4, 3, 6, 5} });

            WriteFile(nameof(CanMergeWithSelection), result);

            using (var document = PdfDocument.Open(result, ParsingOptions.LenientParsingOff))
            {
                Assert.Equal(6, document.NumberOfPages);

                foreach (var page in document.GetPages())
                {
                    Assert.NotNull(page.Text);
                }
            }
        }

        [Fact]
        public void CanMergeMultipleWithSelection()
        {
            var first = IntegrationHelpers.GetDocumentPath("Multiple Page - from Mortality Statistics.pdf");
            var second = IntegrationHelpers.GetDocumentPath("Old Gutnish Internet Explorer.pdf");
            var result = PdfMerger.Merge(new[] { File.ReadAllBytes(first), File.ReadAllBytes(second) }, new[] { new[] { 2, 1, 4, 3, 6, 5 }, new []{ 3, 2, 1 } });

            WriteFile(nameof(CanMergeMultipleWithSelection), result);

            using (var document = PdfDocument.Open(result, ParsingOptions.LenientParsingOff))
            {
                Assert.Equal(9, document.NumberOfPages);

                foreach (var page in document.GetPages())
                {
                    Assert.NotNull(page.Text);
                }
            }
        }

        private static void WriteFile(string name, byte[] bytes)
        {
            try
            {
                if (!Directory.Exists("Merger"))
                {
                    Directory.CreateDirectory("Merger");
                }

                var output = Path.Combine("Merger", $"{name}.pdf");

                File.WriteAllBytes(output, bytes);
            }
            catch
            {
                // ignored.
            }
        }

        private static FileStream GetSelfDestructingNewFileStream(string name)
        {
            try
            {
                if (!Directory.Exists("Merger"))
                {
                    Directory.CreateDirectory("Merger");
                }

                var output = Path.Combine("Merger", $"{name}.pdf");
                return File.Create(output, 4096, FileOptions.DeleteOnClose);
            }
            catch
            {
                return null;
            }
        }

        [Fact]
        public void NoStackoverflow()
        {
            try
            {
                var bytes = PdfMerger.Merge(IntegrationHelpers.GetDocumentPath("68-1990-01_A.pdf"));
                using (var document = PdfDocument.Open(bytes, ParsingOptions.LenientParsingOff))
                {
                    Assert.Equal(45, document.NumberOfPages);
                }
            }
            catch (StackOverflowException)
            {
                Assert.True(false);
            }
        }

        [Fact]
        public void CanSplitDocumentsInPages()
        {
            var one = IntegrationHelpers.GetDocumentPath("68-1990-01_A.pdf");

            var outputStreams = Enumerable.Range(1, 45).Select(r => ((Stream)new MemoryStream(), (IPdfArrangement)new PdfPick { Index = r })).ToList();
            using (var input = File.OpenRead(one))
            {
                PdfMerger.MergeMany(new[] { input }, outputStreams);

                foreach (var (output, _) in outputStreams)
                {
                    output.Position = 0;
                    var document = PdfDocument.Open(output);

                    Assert.Equal(1, document.NumberOfPages);
                }
            }
        }

        class PdfPick : IPdfArrangement
        {
            public int Index { get; set; }

            public IEnumerable<(int FileIndex, IReadOnlyCollection<int> PageIndices)> GetArrangements(Dictionary<int, int> pagesCountPerFileIndex)
            {
                return new[] { (FileIndex: 0, PageIndices: (IReadOnlyCollection<int>)new[] { Index }) };
            }
        }

        class PdfRange : IPdfArrangement
        {
            /// <summary>
            /// Included
            /// </summary>
            public int? StartIndex { get; set; }

            /// <summary>
            /// Excluded
            /// </summary>
            public int? EndIndex { get; set; }

            public IEnumerable<(int FileIndex, IReadOnlyCollection<int> PageIndices)> GetArrangements(Dictionary<int, int> pagesCountPerFileIndex)
            {
                var start = StartIndex ?? 1;
                var end = EndIndex ?? (pagesCountPerFileIndex[0] + 1);
                var count = end - start;
                return new[] { (FileIndex: 0, PageIndices: (IReadOnlyCollection<int>)Enumerable.Range(start, count).ToArray()) };
            }
        }

        [Fact]
        public void CanSplitDocumentsInTwo()
        {
            var one = IntegrationHelpers.GetDocumentPath("Pig Production Handbook.pdf");

            var output1 = new MemoryStream();
            var output2 = new MemoryStream();
            using (var input = File.OpenRead(one))
            {
                PdfMerger.MergeMany(new[] { input }, new[] { ((Stream)output1, (IPdfArrangement)new PdfRange { EndIndex = 7 }), (output2, new PdfRange { StartIndex = 7 }) });

                output1.Position = 0;
                var document1 = PdfDocument.Open(output1);
                Assert.Equal(6, document1.NumberOfPages);

                output2.Position = 0;
                var document2 = PdfDocument.Open(output2);
                Assert.Equal(80, document2.NumberOfPages);
            }
        }
    }
}
