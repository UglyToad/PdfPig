namespace UglyToad.PdfPig.Parser.FileStructure;

using Core;
using Logging;
using System;
using System.Collections.Generic;
using Tokenization.Scanner;
using Tokens;

internal static class XrefTableParser
{
    public static XrefTable? TryReadTableAtOffset(
        long offset,
        IInputBytes bytes,
        ISeekableTokenScanner scanner,
        ILog log)
    {
        bytes.Seek(offset);

        if (!scanner.TryReadToken(out OperatorToken xrefOp))
        {
            return null;
        }

        if (!string.Equals("xref", xrefOp.Data, StringComparison.OrdinalIgnoreCase))
        {
            // Support xref not being followed by spaces or newlines, e.g. "xref5 0"
            if (xrefOp.Data.StartsWith("xref", StringComparison.OrdinalIgnoreCase))
            {
                var backtrack = xrefOp.Data.Length - "xref".Length;
                scanner.Seek(scanner.CurrentPosition - backtrack);
            }
            else
            {
                return null;
            }
        }

        const int freeSentinel = 0;
        const int occupiedSentinel = 1;

        var readNums = new List<long>();

        DictionaryToken? trailer = null;
        var readInLine = 0;
        var clearReadLine = false;
        var expectedEntryCount = 0;
        var mode = XrefTableReadMode.SubsectionHeader;
        while (scanner.MoveNext())
        {
            // If we were reading entries but have no more to consume, revert to looking for subsection headers.
            if (mode == XrefTableReadMode.Entry && expectedEntryCount == 0)
            {
                mode = XrefTableReadMode.SubsectionHeader;
            }

            readInLine++;
            var token = scanner.CurrentToken;
            if (token is NumericToken nt)
            {
                readNums.Add(nt.Long);

                // After reading 2 numbers in subsection mode set the mode to entry and read the expected number of "lines".
                if (mode == XrefTableReadMode.SubsectionHeader && readInLine == 2)
                {
                    mode = XrefTableReadMode.Entry;
                    expectedEntryCount = (int)nt.Long;
                    // Clear the readline count on the next number you read.
                    clearReadLine = true;
                }
                else if (mode == XrefTableReadMode.Entry && readInLine > 2)
                {
                    if (clearReadLine)
                    {
                        clearReadLine = false;
                        readInLine = 1;
                    }
                    else
                    {
                        // If we thought we were reading entries, but we have more than 3 numbers in a row, something is weird and the xref is invalid.
                        return null;
                    }
                }
            }
            else if (token is OperatorToken ot)
            {
                if (string.Equals("f", ot.Data, StringComparison.OrdinalIgnoreCase)
                    && readInLine == 3)
                {
                    // We read 2 numbers followed by "f", this is a free object line.
                    readNums.Add(freeSentinel);
                    readInLine = 0;
                    expectedEntryCount--;
                }
                else if (string.Equals("n", ot.Data, StringComparison.OrdinalIgnoreCase)
                         && readInLine == 3)
                {
                    // We read 2 numbers followed by "n", this is an occupied object line.
                    readNums.Add(occupiedSentinel);
                    readInLine = 0;
                    expectedEntryCount--;
                }
                else if (string.Equals(ot.Data, "trailer", StringComparison.OrdinalIgnoreCase))
                {
                    // On encountering the trailer read the expected dictionary.
                    if (scanner.TryReadToken(out DictionaryToken trailerDictionary))
                    {
                        trailer = trailerDictionary;
                    }
                    else
                    {
                        return null;
                    }
                }
                else if (mode == XrefTableReadMode.SubsectionHeader)
                {
                    // If we read a object number then we remove the object number from the list.
                    if (string.Equals(ot.Data, "obj", StringComparison.OrdinalIgnoreCase))
                    {
                        readNums.RemoveRange(readNums.Count - 2, 2);
                    }

                    break;
                }
                else
                {
                    return null;
                }
            }
            else if (token is CommentToken)
            {
                readInLine--;
            }
            else if (token is not CommentToken)
            {
                break;
            }
        }

        if (readNums.Count == 0)
        {
            return null;
        }

        var offsets = new Dictionary<IndirectReference, long>();
        var ix = 0;
        do
        {
            var firstNum = readNums[ix++];
            if (ix >= readNums.Count)
            {
                return null;
            }

            var count = readNums[ix++];
            
            for (var i = 0; i < count; i++)
            {
                if (ix >= readNums.Count)
                {
                    return null;
                }

                var objOffset = readNums[ix++];
                if (ix >= readNums.Count)
                {
                    return null;
                }

                var objGen = readNums[ix++];
                if (ix >= readNums.Count)
                {
                    return null;
                }

                var sentinel = readNums[ix++];

                if (sentinel == occupiedSentinel)
                {
                    offsets[new IndirectReference(firstNum + i, (int)objGen)] = objOffset;
                }
            }
        } while (ix < readNums.Count);

        return new XrefTable(offset, offsets, trailer);
    }

    private enum XrefTableReadMode
    {
        SubsectionHeader = 2,
        Entry = 3,

    }
}