namespace UglyToad.PdfPig.Writer
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Filters;
    using Logging;
    using System.Linq;
    using UglyToad.PdfPig.Actions;
    using UglyToad.PdfPig.Outline.Destinations;

    /// <summary>
    /// Merges PDF documents into each other.
    /// </summary>
    public static class PdfMerger
    {
        private static readonly ILog Log = new NoOpLog();

        private static readonly IFilterProvider FilterProvider = DefaultFilterProvider.Instance;

        /// <summary>
        /// Merge two PDF documents together with the pages from <paramref name="file1"/> followed by <paramref name="file2"/>.
        /// </summary>
        public static byte[] Merge(string file1, string file2, IReadOnlyList<int> file1Selection = null, IReadOnlyList<int> file2Selection = null, PdfAStandard archiveStandard = PdfAStandard.None, PdfDocumentBuilder.DocumentInformationBuilder docInfoBuilder = null)
        {
            using (var output = new MemoryStream())
            {
                Merge(file1, file2, output, file1Selection, file2Selection, archiveStandard, docInfoBuilder);
                return output.ToArray();
            }
        }

        /// <summary>
        /// Merge two PDF documents together with the pages from <paramref name="file1"/> followed by <paramref name="file2"/> into the output stream.
        /// </summary>
        public static void Merge(string file1, string file2, Stream output, IReadOnlyList<int> file1Selection = null, IReadOnlyList<int> file2Selection = null, PdfAStandard archiveStandard = PdfAStandard.None, PdfDocumentBuilder.DocumentInformationBuilder docInfoBuilder = null)
        {
            _ = file1 ?? throw new ArgumentNullException(nameof(file1));
            _ = file2 ?? throw new ArgumentNullException(nameof(file2));

            using (var stream1 = File.OpenRead(file1))
            {
                using (var stream2 = File.OpenRead(file2))
                {
                    Merge(new[] { stream1, stream2 }, output, new[] { file1Selection, file2Selection }, archiveStandard, docInfoBuilder);
                }
            }
        }

        /// <summary>
        /// Merge multiple PDF documents together with the pages in the order the file paths are provided.
        /// </summary>
        public static byte[] Merge(params string[] filePaths)
        {
            return Merge(PdfAStandard.None, null, filePaths);
        }

        /// <summary>
        /// Merge multiple PDF documents together with the pages in the order the file paths are provided.
        /// </summary>
        public static byte[] Merge(PdfAStandard archiveStandard, PdfDocumentBuilder.DocumentInformationBuilder docInfoBuilder, params string[] filePaths)
        {
            using (var output = new MemoryStream())
            {
                Merge(output, archiveStandard, docInfoBuilder, filePaths);
                return output.ToArray();
            }
        }

        /// <summary>
        /// Merge multiple PDF documents together with the pages in the order the file paths are provided into the output stream
        /// </summary>
        public static void Merge(Stream output, params string[] filePaths)
        {
            Merge(output, PdfAStandard.None, null, filePaths);
        }

        /// <summary>
        /// Merge multiple PDF documents together with the pages in the order the file paths are provided into the output stream
        /// </summary>
        public static void Merge(Stream output, PdfAStandard archiveStandard, PdfDocumentBuilder.DocumentInformationBuilder docInfoBuilder, params string[] filePaths)
        {
            var streams = new List<Stream>(filePaths.Length);
            try
            {
                for (var i = 0; i < filePaths.Length; i++)
                {
                    var filePath = filePaths[i] ?? throw new ArgumentNullException(nameof(filePaths), $"Null filepath at index {i}.");
                    streams.Add(File.OpenRead(filePath));
                }

                Merge(streams, output, null, archiveStandard, docInfoBuilder);
            }
            finally
            {
                foreach (var stream in streams)
                {
                    stream.Dispose();
                }
            }
        }

        /// <summary>
        /// Merge the set of PDF documents.
        /// </summary>
        public static byte[] Merge(IReadOnlyList<byte[]> files, IReadOnlyList<IReadOnlyList<int>> pagesBundle = null, PdfAStandard archiveStandard = PdfAStandard.None, PdfDocumentBuilder.DocumentInformationBuilder docInfoBuilder = null)
        {
            _ = files ?? throw new ArgumentNullException(nameof(files));

            using (var output = new MemoryStream())
            {
                Merge(files.Select(f => PdfDocument.Open(f)).ToArray(), output, pagesBundle, archiveStandard, docInfoBuilder);
                return output.ToArray();
            }
        }

        /// <summary>
        /// Merge the set of PDF documents into the output stream
        /// The caller must manage disposing the stream. The created PdfDocument will not dispose the stream.
        /// <param name="streams">
        /// A list of streams for the files contents, this must support reading and seeking.
        /// </param>
        /// <param name="output">Must be writable</param>
        /// <param name="pagesBundle"></param>
        /// <param name="archiveStandard"></param>
        /// <param name="docInfoBuilder"></param>
        /// </summary>
        public static void Merge(IReadOnlyList<Stream> streams, Stream output, IReadOnlyList<IReadOnlyList<int>> pagesBundle = null, PdfAStandard archiveStandard = PdfAStandard.None, PdfDocumentBuilder.DocumentInformationBuilder docInfoBuilder = null)
        {
            _ = streams ?? throw new ArgumentNullException(nameof(streams));
            _ = output ?? throw new ArgumentNullException(nameof(output));

            Merge(streams.Select(f => PdfDocument.Open(f)).ToArray(), output, pagesBundle, archiveStandard, docInfoBuilder);
        }

        private static void Merge(IReadOnlyList<PdfDocument> files, Stream output, IReadOnlyList<IReadOnlyList<int>> pagesBundle, PdfAStandard archiveStandard = PdfAStandard.None, PdfDocumentBuilder.DocumentInformationBuilder docInfoBuilder = null)
        {
            var maxVersion = files.Select(x => x.Version).Max();
            using (var document = new PdfDocumentBuilder(output, false, PdfWriterType.Default, maxVersion))
            {
                document.ArchiveStandard = archiveStandard;
                if (docInfoBuilder != null)
                {
                    document.IncludeDocumentInformation = true;
                    document.DocumentInformation = docInfoBuilder;
                }
                foreach (var fileIndex in Enumerable.Range(0, files.Count))
                {
                    var existing = files[fileIndex];
                    IReadOnlyList<int> pages = null;
                    if (pagesBundle != null && fileIndex < pagesBundle.Count)
                    {
                        pages = pagesBundle[fileIndex];
                    }

                    var basePageNumber = document.Pages.Count;

                    if (pages == null)
                    {
                        for (var i = 1; i <= existing.NumberOfPages; i++)
                        {
                            document.AddPage(existing, i, link => CopyLink(link, n => basePageNumber + n));
                        }
                    }
                    else
                    {
                        var pageNumbers = new Dictionary<int, int>();
                        for (var i = 0; i < pages.Count; i++)
                        {
                            pageNumbers[pages[i]] = basePageNumber + i + 1;
                        }

                        foreach (var i in pages)
                        {
                            document.AddPage(existing, i, link => CopyLink(link, n =>
                            {
                                if (pageNumbers.TryGetValue(n, out var pageNumber))
                                {
                                    return pageNumber;
                                }
                                return null;
                            }));
                        }
                    }
                }
            }

            PdfAction CopyLink(PdfAction action, Func<int, int?> getPageNumber)
            {
                if (!(action is AbstractGoToAction link))
                {
                    // copy the link if it is not a link to PDF documents
                    return action;
                }

                var newPageNumber = getPageNumber(link.Destination.PageNumber);
                if (newPageNumber == null)
                {
                    // ignore the link if the target page does not exist in the PDF document
                    return null;
                }

                var newDestination = new ExplicitDestination(newPageNumber.Value, link.Destination.Type, link.Destination.Coordinates);

                switch (action)
                {
                    case GoToAction goToAction:
                        return new GoToAction(newDestination);

                    case GoToEAction goToEAction:
                        return new GoToEAction(newDestination, goToEAction.FileSpecification);

                    case GoToRAction goToRAction:
                        return new GoToRAction(newDestination, goToRAction.Filename);

                    default:
                        return action;
                }
            }
        }
    }
}
