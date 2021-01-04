namespace UglyToad.PdfPig.Writer
{
    using Core;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    /// <summary>
    /// Split a PDF document into page bundles.
    /// </summary>
    public static class PdfSplitter
    {
        /// <summary>
        /// Split a pdf in two
        /// </summary>
        /// <param name="file"></param>
        /// <param name="secondDocumentFirstPageIndex">1-based</param>
        /// <param name="output1"></param>
        /// <param name="output2"></param>
        public static void SplitTwoParts(Stream file, int secondDocumentFirstPageIndex, Stream output1, Stream output2)
        {
            if (secondDocumentFirstPageIndex <= 1)
            {
                throw new ArgumentException("secondDocumentFirstPageIndex value is not correct. This index is 1-based");
            }
            var removedPages = Enumerable.Range(1, secondDocumentFirstPageIndex - 1).ToArray();
            RemovePages(file, removedPages, output2, output1);
        }

        /// <summary>
        /// Generates a new pdf with pages removed from the original
        /// </summary>
        /// <param name="file"></param>
        /// <param name="removedPages"></param>
        /// <param name="output"></param>
        /// <param name="removedPagesOutput"></param>
        public static void RemovePages(Stream file, IReadOnlyCollection<int> removedPages, Stream output, Stream removedPagesOutput = null)
        {
            if (removedPagesOutput is null)
            {
                throw new ArgumentNullException(nameof(removedPagesOutput));
            }

            _ = file ?? throw new ArgumentNullException(nameof(file));
            _ = output ?? throw new ArgumentNullException(nameof(output));

            using (var stream = new StreamInputBytes(file))
            {
                if (removedPagesOutput is null)
                {
                    PdfRearranger.Rearrange(new[] { stream }, new PdfPageRemover(removedPages), output);
                }
                else
                {
                    var rearrangments = new[]
                    {
                    ((IPdfArrangement)new PdfPageRemover(removedPages), output),
                    (new PdfPick(removedPages), removedPagesOutput),
                };
                    PdfRearranger.RearrangeMany(new[] { stream }, rearrangments);
                }
            }
        }

        class PdfPageRemover : IPdfArrangement
        {
            private readonly IReadOnlyCollection<int> removedPages;

            public PdfPageRemover(IReadOnlyCollection<int> pages) => removedPages = pages;

            public IEnumerable<(int FileIndex, IReadOnlyCollection<int> PageIndices)> GetArrangements(Dictionary<int, int> pagesCountPerFileIndex)
            {
                var pages = new HashSet<int>(Enumerable.Range(1, pagesCountPerFileIndex[0]));
                pages.ExceptWith(removedPages);
                yield return (0, (IReadOnlyCollection<int>)pages);
            }
        }

        /// <summary>
        /// Split a pdf into several documents with a fixed number of pages
        /// </summary>
        /// <param name="file"></param>
        /// <param name="outputs"></param>
        /// <param name="pageCountPerFile"></param>
        public static void SplitEveryPage(Stream file, IEnumerable<Stream> outputs, int pageCountPerFile = 1)
        {
            _ = file ?? throw new ArgumentNullException(nameof(file));
            _ = outputs ?? throw new ArgumentNullException(nameof(outputs));

            using (var stream = new StreamInputBytes(file))
            {
                PdfRearranger.RearrangeMany(new[] { stream }, SplitEveryXPage(pageCountPerFile, outputs));
            }
        }

        static IEnumerable<(IPdfArrangement Arrangement, Stream Output)> SplitEveryXPage(int pageCountPerFile, IEnumerable<Stream> output)
        {
            var index = 1;
            var enumerator = output.GetEnumerator();
            while (enumerator.MoveNext())
            {
                var pages = Enumerable.Range(index, pageCountPerFile).ToArray();
                index += pageCountPerFile;
                yield return (new PdfPick(pages), enumerator.Current);
            }
        }

        /// <summary>
        /// Split a pdf file into several according to the requested <paramref name="pageBundles"/>
        /// </summary>
        /// <param name="file"></param>
        /// <param name="pageBundles"></param>
        public static void Split(Stream file, IReadOnlyCollection<(IReadOnlyCollection<int> Pages, Stream output)> pageBundles)
        {
            _ = file ?? throw new ArgumentNullException(nameof(file));
            var arrangements = pageBundles.Select(t => ((IPdfArrangement)new PdfPick(t.Pages), t.output)).ToArray();

            using (var stream = new StreamInputBytes(file))
            {
                PdfRearranger.RearrangeMany(new[] { stream }, arrangements);
            }
        }

        class PdfPick : IPdfArrangement
        {
            private readonly IReadOnlyCollection<int> pages;

            public PdfPick(IReadOnlyCollection<int> pages) => this.pages = pages;

            public IEnumerable<(int FileIndex, IReadOnlyCollection<int> PageIndices)> GetArrangements(Dictionary<int, int> pagesCountPerFileIndex)
            {
                var validPages = pages.Where(p => p <= pagesCountPerFileIndex[0]).ToArray();
                if (validPages.Length == 0)
                {
                    yield break;
                }

                yield return (0, validPages);
            }
        }
    }
}