namespace UglyToad.PdfPig.Parser.FileStructure;

using Core;
using Logging;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
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

        const int objRowSentinel = -1;
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
            if (mode == XrefTableReadMode.Entry && expectedEntryCount <= 0)
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

                    readNums.Insert(readNums.Count - 3, objRowSentinel);
                }
                else if (string.Equals("n", ot.Data, StringComparison.OrdinalIgnoreCase)
                         && readInLine == 3)
                {
                    // We read 2 numbers followed by "n", this is an occupied object line.
                    readNums.Add(occupiedSentinel);
                    readInLine = 0;
                    expectedEntryCount--;

                    readNums.Insert(readNums.Count - 3, objRowSentinel);
                }
                else if (string.Equals(ot.Data, "trailer", StringComparison.OrdinalIgnoreCase))
                {
                    // On encountering the trailer read the expected dictionary.
                    if (scanner.TryReadToken(out DictionaryToken trailerDictionary))
                    {
                        trailer = trailerDictionary;
                        break;
                    }

                    return null;
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

        var offsets = new Dictionary<IndirectReference, long>();
        if (readNums.Count == 0)
        {
            if (trailer != null)
            {
                return new XrefTable(
                    offset,
                    offsets,
                    trailer);
            }

            return null;
        }

        var buff = new long[4];

        var objNum = -1L;
        var ix = 0;

        bool TryReadBuff(int len)
        {
            for (var i = 0; i < len; i++)
            {
                if (ix >= readNums.Count)
                {
                    return false;
                }

                buff[i] = readNums[ix++];
            }

            return true;
        }

        do
        {
            if (!TryReadBuff(2))
            {
                return null;
            }

            var first = buff[0];
            var second = buff[1];

            if (first != objRowSentinel)
            {
                objNum = first;
            }
            else
            {
                if (objNum == -1)
                {
                    return null;
                }

                second = 1;
                ix -= 2;
            }

            for (var i = 0; i < second; i++)
            {
                if (!TryReadBuff(4))
                {
                    return null;
                }

                var sentinel = buff[0];
                var objOffset = buff[1];
                var gen = buff[2];
                var type = buff[3];

                if (sentinel != objRowSentinel)
                {
                    return null;
                }

                if (type == occupiedSentinel)
                {
                    var indirectRef = new IndirectReference(objNum, (int)gen);
                    offsets[indirectRef] = objOffset;
                }

                objNum++;
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