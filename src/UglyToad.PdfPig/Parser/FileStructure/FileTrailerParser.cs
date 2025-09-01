namespace UglyToad.PdfPig.Parser.FileStructure
{
    using System;
    using Core;
    using Tokenization.Scanner;
    using Tokens;

    /*
     * The trailer of a PDF file allows us to quickly find the cross-reference table and other special objects. 
     * Readers should read a PDF file from its end. 
     * The last line of the file should contain the end-of-file marker, %%EOF. 
     * The two preceding lines should be the keyword startxref and the byte offset of the cross-reference section from the start of the document.
     * The startxref line might be preceded by the trailer dictionary of the form:
     * trailer
     * <</key1 value1/key2 value2/key3 value3/key4 value4>>
     * startxref
     * byte-offset
     * %%EOF
     */

    internal static class FileTrailerParser
    {
        /// <summary>
        /// The %%EOF may be further back in the file.
        /// </summary>
        private const int EndOfFileSearchRange = 2048;

        internal static ReadOnlySpan<byte> StartXRefBytes => "startxref"u8;

        public static long GetFirstCrossReferenceOffset(IInputBytes bytes, ISeekableTokenScanner scanner, bool isLenientParsing)
        {
            if (bytes is null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }

            if (scanner is null)
            {
                throw new ArgumentNullException(nameof(scanner));
            }

            var fileLength = bytes.Length;

            var offsetFromEnd = fileLength < EndOfFileSearchRange ? (int)fileLength : EndOfFileSearchRange;

            var startXrefPosition = GetStartXrefPosition(bytes, offsetFromEnd);

            scanner.Seek(startXrefPosition);

            if (!scanner.TryReadToken(out OperatorToken startXrefToken) || startXrefToken.Data != "startxref")
            {
                throw new InvalidOperationException($"The start xref position we found was not correct. Found {startXrefPosition} but it was occupied by token {scanner.CurrentToken}.");
            }

            NumericToken? numeric = null;
            while (scanner.MoveNext())
            {
                if (scanner.CurrentToken is NumericToken token)
                {
                    numeric = token;
                    break;
                }

                if (!(scanner.CurrentToken is CommentToken))
                {
                    throw new PdfDocumentFormatException($"Found an unexpected token following 'startxref': {scanner.CurrentToken}.");
                }
            }

            if (numeric is null)
            {
                throw new PdfDocumentFormatException($"Could not find the numeric value following 'startxref'. Searching from position {startXrefPosition}.");
            }

            return numeric.Long;
        }

        private static long GetStartXrefPosition(IInputBytes bytes, int chunkSize)
        {
            // Initialize startpos to the end to get the loop below started
            var initialLengthRead = bytes.Length;
            var startPos = bytes.Length;

            do
            {
                // Make a sliding-window search region where each subsequent search will look further
                // back and not search in the already searched chunks. Make sure to search just beyond
                // the chunk to account for the possibility of startxref crossing chunk-boundaries.
                // The start-position is inclusive and the end-position is exclusive for the chunk.
                // Each search will look in an increasingly bigger chunk, doubling every time.
                var endPos = Math.Min(startPos + StartXRefBytes.Length, bytes.Length);
                startPos = Math.Max(0, endPos - chunkSize);
                chunkSize *= 2;

                // Some streams such as FileBufferingReadStream can misreport their length.
                var isDetectedStreamEnd = endPos == initialLengthRead;

                // Prepare to search this region; mark startXrefPos as "not found".
                bytes.Seek(startPos);
                var startXrefPos = -1L;
                var index = 0;

                // Starting scanning the file bytes.
                while ((bytes.CurrentOffset < endPos || isDetectedStreamEnd) && bytes.MoveNext())
                {
                    if (bytes.CurrentByte == StartXRefBytes[index])
                    {
                        // We might be reading "startxref".
                        if (++index == StartXRefBytes.Length)
                        {
                            // Set this "startxref" (position from the start of the document to the first 's').
                            startXrefPos = (int)bytes.CurrentOffset - StartXRefBytes.Length;

                            // Continue scanning to make sure we find the last startxref in case there are more
                            // that just one, which can be the case for incrementally updated PDFs with multiple
                            // generations of sections.
                            index = 0;
                        }
                    }
                    else
                    {
                        // Not a match for "startxref" so set index back to 0
                        index = 0;
                    }
                }

                // If we found a startxref then we're done.
                if (startXrefPos >= 0)
                {
                    return startXrefPos;
                }
            } while (startPos > 0); // Keep on searching until we've read from the very start.
            
            // No startxref position was found.
            throw new PdfDocumentFormatException($"Could not find the startxref");
        }
    }
}
