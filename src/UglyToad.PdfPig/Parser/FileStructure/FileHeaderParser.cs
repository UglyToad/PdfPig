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
    /// For versions equal or greater to PDF 1.4, the optional Version entry in the document's catalog dictionary should be used instead of the header version.
    /// </remarks>
    internal static class FileHeaderParser
    {
        [NotNull]
        public static HeaderVersion Parse([NotNull] ISeekableTokenScanner scanner, IInputBytes inputBytes, bool isLenientParsing, ILog log)
        {
            if (scanner == null)
            {
                throw new ArgumentNullException(nameof(scanner));
            }

            var startPosition = scanner.CurrentPosition;

            const int junkTokensTolerance = 30;
            var attempts = 0;
            CommentToken comment;
            do
            {
                if (attempts == junkTokensTolerance || !scanner.MoveNext())
                {
                    if (!TryBruteForceVersionLocation(startPosition, inputBytes, out var version))
                    {
                        throw new PdfDocumentFormatException("Could not find the version header comment at the start of the document.");
                    }

                    scanner.Seek(startPosition);
                    return version;
                }

                comment = scanner.CurrentToken as CommentToken;

                attempts++;
            } while (comment == null);

            return GetHeaderVersionAndResetScanner(comment, scanner, isLenientParsing, log);
        }

        private static HeaderVersion GetHeaderVersionAndResetScanner(CommentToken comment, ISeekableTokenScanner scanner, bool isLenientParsing, ILog log)
        {
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

            var atEnd = scanner.CurrentPosition == scanner.Length;
            var rewind = atEnd ? 1 : 2;

            var commentOffset = scanner.CurrentPosition - comment.Data.Length - rewind;

            scanner.Seek(0);

            var result = new HeaderVersion(version, comment.Data, commentOffset);

            return result;
        }

        private static bool TryBruteForceVersionLocation(long startPosition, IInputBytes inputBytes, out HeaderVersion headerVersion)
        {
            headerVersion = null;

            inputBytes.Seek(startPosition);

            // %PDF-x.y or %FDF-x.y
            const int versionLength = 8;
            const int bufferLength = 64;

            // Slide a window of bufferLength bytes across the file allowing for the fact the version could get split by
            // the window (so always ensure an overlap of versionLength bytes between the end of the previous and start of the next buffer).
            var buffer = new byte[bufferLength];

            var currentOffset = startPosition;
            int readLength;
            do
            {
                readLength = inputBytes.Read(buffer, bufferLength);

                var content = OtherEncodings.BytesAsLatin1String(buffer);

                var pdfIndex = content.IndexOf("%PDF-", StringComparison.OrdinalIgnoreCase);
                var fdfIndex = content.IndexOf("%FDF-", StringComparison.OrdinalIgnoreCase);
                var actualIndex = pdfIndex >= 0 ? pdfIndex : fdfIndex;

                if (actualIndex >= 0 && content.Length - actualIndex >= versionLength)
                {
                    var numberPart = content.Substring(actualIndex + 5, 3);
                    if (decimal.TryParse(
                            numberPart,
                            NumberStyles.Number,
                            CultureInfo.InvariantCulture,
                            out var version))
                    {
                        var afterCommentSymbolIndex = actualIndex + 1;

                        headerVersion = new HeaderVersion(
                            version,
                            content.Substring(afterCommentSymbolIndex, versionLength - 1),
                            currentOffset + actualIndex);

                        inputBytes.Seek(startPosition);

                        return true;
                    }
                }

                currentOffset += readLength - versionLength;
                inputBytes.Seek(currentOffset);
            } while (readLength == bufferLength);

            return false;
        }

        private static HeaderVersion HandleMissingVersion(CommentToken comment, bool isLenientParsing, ILog log)
        {
            if (isLenientParsing)
            {
                log.Warn($"Did not find a version header of the correct format, defaulting to 1.4 since lenient. Header was: {comment.Data}.");

                return new HeaderVersion(1.4m, "PDF-1.4", 0);
            }

            throw new PdfDocumentFormatException($"The comment which should have provided the version was in the wrong format: {comment.Data}.");
        }
    }
}
