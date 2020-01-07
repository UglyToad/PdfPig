namespace UglyToad.PdfPig.Parser.FileStructure
{
    using System;
    using System.Collections.Generic;
    using Core;
    using Exceptions;
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

    internal class FileTrailerParser
    {
        /// <summary>
        /// Acrobat viewers require the EOF to be in the last 1024 bytes instead of at the end.
        /// </summary>
        private const int EndOfFileSearchRange = 1024;

        private static readonly byte[] StartXRefBytes =
        {
            (byte) 's',
            (byte) 't',
            (byte) 'a',
            (byte) 'r',
            (byte) 't',
            (byte) 'x',
            (byte) 'r',
            (byte) 'e',
            (byte) 'f'
        };
        
        public long GetFirstCrossReferenceOffset(IInputBytes bytes, ISeekableTokenScanner scanner, bool isLenientParsing)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }

            if (scanner == null)
            {
                throw new ArgumentNullException(nameof(scanner));
            }

            var fileLength = bytes.Length;

            var offsetFromEnd = fileLength < EndOfFileSearchRange ? (int)fileLength : EndOfFileSearchRange;

            var startPosition = fileLength - offsetFromEnd;

            bytes.Seek(startPosition);

            var startXrefPosition = GetStartXrefPosition(bytes, offsetFromEnd);

            scanner.Seek(startXrefPosition);

            if (!scanner.TryReadToken(out OperatorToken startXrefToken) || startXrefToken.Data != "startxref")
            {
                throw new InvalidOperationException($"The start xref position we found was not correct. Found {startXrefPosition} but it was occupied by token {scanner.CurrentToken}.");
            }

            NumericToken numeric = null;
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

            if (numeric == null)
            {
                throw new PdfDocumentFormatException($"Could not find the numeric value following 'startxref'. Searching from position {startXrefPosition}.");
            }

            return numeric.Long;
        }

        private static long GetStartXrefPosition(IInputBytes bytes, int offsetFromEnd)
        {
            var startXrefs = new List<int>();

            var index = 0;
            var offset = 0;
            
            // Starting scanning the last 1024 bytes.
            while (bytes.MoveNext())
            {
                offset++;
                if (bytes.CurrentByte == StartXRefBytes[index])
                {
                    // We might be reading "startxref".
                    index++;
                }
                else
                {
                    index = 0;
                }

                if (index == StartXRefBytes.Length)
                {
                    // Add this "startxref" (position from the end of the document to the first 's').
                    startXrefs.Add(offsetFromEnd - (offset - StartXRefBytes.Length));

                    // Continue scanning in case there are further "startxref"s. Not sure if this ever happens.
                    index = 0;
                }
            }

            if (startXrefs.Count == 0)
            {
                throw new PdfDocumentFormatException("Could not find the startxref within the last 1024 characters.");
            }

            return bytes.Length - startXrefs[startXrefs.Count - 1];
        }
    }
}
