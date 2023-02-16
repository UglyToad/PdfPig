namespace UglyToad.PdfPig.Writer
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Filters;
    using Logging;
    using System.Linq;

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
        public static byte[] Merge(string file1, string file2, IReadOnlyList<int> file1Selection = null, IReadOnlyList<int> file2Selection = null, PdfAStandard archiveStandard = PdfAStandard.None)
        {
            using (var output = new MemoryStream())
            {
                Merge(file1, file2, output, file1Selection, file2Selection, archiveStandard);
                return output.ToArray();
            }
        }

        /// <summary>
        /// Merge two PDF documents together with the pages from <paramref name="file1"/> followed by <paramref name="file2"/> into the output stream.
        /// </summary>
        public static void Merge(string file1, string file2, Stream output, IReadOnlyList<int> file1Selection = null, IReadOnlyList<int> file2Selection = null, PdfAStandard archiveStandard = PdfAStandard.None)
        {
            _ = file1 ?? throw new ArgumentNullException(nameof(file1));
            _ = file2 ?? throw new ArgumentNullException(nameof(file2));

            using (var stream1 = File.OpenRead(file1))
            {
                using (var stream2 = File.OpenRead(file2))
                {
                    Merge(new[] { stream1, stream2 }, output, new[] { file1Selection, file2Selection }, archiveStandard);
                }
            }
        }

        /// <summary>
        /// Merge multiple PDF documents together with the pages in the order the file paths are provided.
        /// </summary>
        public static byte[] Merge(params string[] filePaths)
        {
            return Merge(PdfAStandard.None, filePaths);
        }

        /// <summary>
        /// Merge multiple PDF documents together with the pages in the order the file paths are provided.
        /// </summary>
        public static byte[] Merge(PdfAStandard archiveStandard, params string[] filePaths)
        {
            using (var output = new MemoryStream())
            {
                Merge(output, archiveStandard, filePaths);
                return output.ToArray();
            }
        }

        /// <summary>
        /// Merge multiple PDF documents together with the pages in the order the file paths are provided into the output stream
        /// </summary>
        public static void Merge(Stream output, params string[] filePaths) 
        {
            Merge(output, PdfAStandard.None, filePaths);
        }

        /// <summary>
        /// Merge multiple PDF documents together with the pages in the order the file paths are provided into the output stream
        /// </summary>
        public static void Merge(Stream output, PdfAStandard archiveStandard, params string[] filePaths)
        {
            var streams = new List<Stream>(filePaths.Length);
            try
            {
                for (var i = 0; i < filePaths.Length; i++)
                {
                    var filePath = filePaths[i] ?? throw new ArgumentNullException(nameof(filePaths), $"Null filepath at index {i}.");
                    streams.Add(File.OpenRead(filePath));
                }

                Merge(streams, output, null, archiveStandard);
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
        public static byte[] Merge(IReadOnlyList<byte[]> files, IReadOnlyList<IReadOnlyList<int>> pagesBundle = null, PdfAStandard archiveStandard = PdfAStandard.None)
        {
            _ = files ?? throw new ArgumentNullException(nameof(files));

            using (var output = new MemoryStream())
            {
                Merge(files.Select(f => PdfDocument.Open(f)).ToArray(), output, pagesBundle, archiveStandard);
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
        /// </summary>
        public static void Merge(IReadOnlyList<Stream> streams, Stream output, IReadOnlyList<IReadOnlyList<int>> pagesBundle = null, PdfAStandard archiveStandard = PdfAStandard.None)
        {
            _ = streams ?? throw new ArgumentNullException(nameof(streams));
            _ = output ?? throw new ArgumentNullException(nameof(output));

            Merge(streams.Select(f => PdfDocument.Open(f)).ToArray(), output, pagesBundle, archiveStandard);
        }

        private static void Merge(IReadOnlyList<PdfDocument> files, Stream output, IReadOnlyList<IReadOnlyList<int>> pagesBundle, PdfAStandard archiveStandard = PdfAStandard.None)
        {
            var maxVersion = files.Select(x=>x.Version).Max();
            using (var document = new PdfDocumentBuilder(output, false, PdfWriterType.Default, maxVersion))
            {
                document.ArchiveStandard = archiveStandard;
                foreach (var fileIndex in Enumerable.Range(0, files.Count))
                {
                    var existing = files[fileIndex];
                    IReadOnlyList<int> pages = null;
                    if (pagesBundle != null && fileIndex < pagesBundle.Count)
                    {
                        pages = pagesBundle[fileIndex];
                    }

                    if (pages == null)
                    {
                        for (var i = 1; i <= existing.NumberOfPages; i++)
                        {
                            document.AddPage(existing, i);
                        }
                    } else
                    {
                        foreach (var i in pages)
                        {
                            document.AddPage(existing, i);
                        }
                    }
                }
            }
        }

    }
}