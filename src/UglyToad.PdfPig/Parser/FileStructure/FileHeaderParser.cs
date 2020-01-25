namespace UglyToad.PdfPig.Parser.FileStructure
{
    using System;
    using System.Globalization;
    using Content;
    using Core;
    using Logging;
    using Tokenization.Scanner;
    using Tokens;
    using Util.JetBrains.Annotations;

    /// <summary>
    /// Used to retrieve the version header from the PDF file.
    /// </summary>
    /// <remarks>
    /// The first line of a PDF file should be a header consisting of the 5 characters %PDF– followed by a version number of the form 1.N, where N is a digit between 0 and 7.
    /// A conforming reader should accept files with any of the following headers:
    /// %PDF–1.0
    /// %PDF–1.1
    /// %PDF–1.2
    /// %PDF–1.3
    /// %PDF–1.4
    /// %PDF–1.5
    /// %PDF–1.6
    /// %PDF–1.7
    /// This parser allows versions up to 1.9.
    /// For versions equal or greater to PDF 1.4, the optional Version entry in the document’s catalog dictionary should be used instead of the header version.
    /// </remarks>
    internal static class FileHeaderParser
    {
        [NotNull]
        public static HeaderVersion Parse([NotNull]ISeekableTokenScanner scanner, bool isLenientParsing, ILog log)
        {
            if (scanner == null)
            {
                throw new ArgumentNullException(nameof(scanner));
            }

            // Read the first token
            if (!scanner.MoveNext())
            {
                throw new PdfDocumentFormatException($"Could not read the first token in the document at position {scanner.CurrentPosition}.");
            }

            var comment = scanner.CurrentToken as CommentToken;

            const int junkTokensTolerance = 25;
            var attempts = 0;
            while (comment == null)
            {
                if (attempts == junkTokensTolerance)
                {
                    throw new PdfDocumentFormatException("Could not find the version header comment at the start of the document.");
                }

                if (!scanner.MoveNext())
                {
                    throw new PdfDocumentFormatException("Could not find the version header comment at the start of the document.");
                }

                comment = scanner.CurrentToken as CommentToken;

                attempts++;
            }

            if (comment.Data.IndexOf("PDF-1.", StringComparison.OrdinalIgnoreCase) != 0 && comment.Data.IndexOf("FDF-1.", StringComparison.OrdinalIgnoreCase) != 0)
            {
                return HandleMissingVersion(comment, isLenientParsing, log);
            }

            const int toDecimalStartLength = 4;

            if (!decimal.TryParse(comment.Data.Substring(toDecimalStartLength), 
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out var version))
            {
                return HandleMissingVersion(comment, isLenientParsing, log);
            }

            scanner.Seek(0);

            var result = new HeaderVersion(version, comment.Data);

            return result;
        }

        private static HeaderVersion HandleMissingVersion(CommentToken comment, bool isLenientParsing, ILog log)
        {
            if (isLenientParsing)
            {
                log.Warn($"Did not find a version header of the correct format, defaulting to 1.4 since lenient. Header was: {comment.Data}.");

                return new HeaderVersion(1.4m, "PDF-1.4");
            }

            throw new PdfDocumentFormatException($"The comment which should have provided the version was in the wrong format: {comment.Data}.");
        }
    }
}
