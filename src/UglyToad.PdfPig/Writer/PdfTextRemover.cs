using System;
using System.Collections.Generic;
using System.IO;

namespace UglyToad.PdfPig.Writer
{
    /// <summary>
    /// Class to remove text from PDFs, useful as a preprocessing step for Optical Character Recognition (OCR).
    /// Note that this should not be used to redact content from PDFs, this is not a secure or reliable way to redact text.
    /// </summary>
    /// <remarks>
    /// This is being made internal for release of the next major version subject to some refinements.
    /// It can be re-enabled for nightly versions of 0.1.9.
    /// </remarks>
    public static class PdfTextRemover
    {
        /// <summary>
        /// Return PDF without text as bytes
        /// <param name="filePath">Path to PDF</param>
        /// <param name="pagesBundle">List of pages to emit; if null all pages are emitted</param>
        /// </summary>
        public static byte[] RemoveText(string filePath, IReadOnlyList<int> pagesBundle = null)
        {
            using (var output = new MemoryStream())
            {
                RemoveText(output, filePath, pagesBundle);
                return output.ToArray();
            }
        }

        /// <summary>
        /// Write PDF without text to the output stream. The caller must manage disposing the output stream.
        /// <param name="output">Must be writable</param>
        /// <param name="filePath">Path to PDF</param>
        /// <param name="pagesBundle">List of pages to emit; if null all pages are emitted</param>
        /// </summary>
        public static void RemoveText(Stream output, string filePath, IReadOnlyList<int> pagesBundle = null)
        {
            using (var stream = File.OpenRead(filePath))
            {
                RemoveText(stream, output, pagesBundle);
            }
        }

        /// <summary>
        /// Remove text from the PDF (passed in as a byte array) and return it as a new byte array
        /// <param name="file">PDF document (as byte array)</param>
        /// <param name="pagesBundle">List of pages to emit; if null all pages are emitted</param>
        /// <returns>PDF without text (as a byte array)</returns>
        /// </summary>
        public static byte[] RemoveText(byte[] file, IReadOnlyList<int> pagesBundle = null)
        {
            _ = file ?? throw new ArgumentNullException(nameof(file));

            using (var output = new MemoryStream())
            {
                RemoveText(PdfDocument.Open(file), output, pagesBundle);
                return output.ToArray();
            }
        }

        /// <summary>
        /// Remove text from the PDF in the input stream and write it to the output stream.
        /// The caller must manage disposing the stream. The created PdfDocument will not dispose the stream.
        /// <param name="stream">Streams for the file contents, this must support reading and seeking.</param>
        /// <param name="output">Must be writable</param>
        /// <param name="pagesBundle">List of pages to emit; if null all pages are emitted</param>
        /// </summary>
        public static void RemoveText(Stream stream, Stream output, IReadOnlyList<int> pagesBundle = null)
        {
            _ = stream ?? throw new ArgumentNullException(nameof(stream));
            _ = output ?? throw new ArgumentNullException(nameof(output));

            RemoveText(PdfDocument.Open(stream), output, pagesBundle);
        }

        /// <summary>
        /// Remove text from the PDF and write it to the output stream.
        /// The caller must manage disposing the stream. The created PdfDocument will not dispose the stream.
        /// <param name="file">PDF document</param>
        /// <param name="output">Must be writable</param>
        /// <param name="pagesBundle">List of pages to emit; if null all pages are emitted</param>
        /// </summary>
        public static void RemoveText(PdfDocument file, Stream output, IReadOnlyList<int> pagesBundle = null)
        {
            using (var document = new PdfDocumentBuilder(output, false, PdfWriterType.Default, file.Version, tokenWriter: new NoTextTokenWriter()))
            {
                if (pagesBundle == null)
                {
                    for (var i = 1; i <= file.NumberOfPages; i++)
                    {
                        document.AddPage(file, i);
                    }
                } 
                else
                {
                    foreach (var i in pagesBundle)
                    {
                        document.AddPage(file, i);
                    }
                }
            }
        }
    }
}
