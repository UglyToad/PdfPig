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
        FileHeaderOffset fileHeaderOffset,
        long xrefOffset,
        IInputBytes bytes,
        ISeekableTokenScanner scanner,
        ILog log)
    {
        if (xrefOffset >= bytes.Length || xrefOffset < 0)
        {
            return null;
        }

        bytes.Seek(xrefOffset);

        var correctionType = XrefOffsetCorrection.None;
        var correction = 0L;

        if (!TryReadXrefToken(scanner))
        {
            log.Debug($"Xref not found at {xrefOffset}, trying to recover");
            var recovered = TryRecoverOffset(fileHeaderOffset, xrefOffset, bytes, scanner);
            if (recovered == null)
            {
                return null;
            }

            log.Debug($"Xref found at {recovered.Value.correctOffset}");
            scanner.Seek(recovered.Value.correctOffset);
            if (!TryReadXrefToken(scanner))
            {
                return null;
            }

            correctionType = recovered.Value.correctionType;
            correction = recovered.Value.correctOffset - xrefOffset;
            xrefOffset = recovered.Value.correctOffset;
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
                    xrefOffset,
                    offsets,
                    trailer,
                    correctionType,
                    correction);
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

        return new XrefTable(xrefOffset, offsets, trailer, correctionType, correction);
    }

    private static bool TryReadXrefToken(ISeekableTokenScanner scanner)
    {
        if (!scanner.TryReadToken(out OperatorToken xrefOp))
        {
            return false;
        }

        if (string.Equals("xref", xrefOp.Data, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Support xref not being followed by spaces or newlines, e.g. "xref5 0"
        if (xrefOp.Data.StartsWith("xref", StringComparison.OrdinalIgnoreCase))
        {
            var backtrack = xrefOp.Data.Length - "xref".Length;
            scanner.Seek(scanner.CurrentPosition - backtrack);
            return true;
        }

        return false;
    }

    /// <summary>
    /// The provided offset can frequently be close but not quite correct.
    /// The 2 most common failure modes are that the PDF content starts at some
    /// non-zero offset in the file so all content is shifted by <param name="fileHeaderOffset"/> bytes
    /// or we're within a few bytes of the offset but not directly at it.
    /// </summary>
    private static (long correctOffset, XrefOffsetCorrection correctionType)? TryRecoverOffset(
        FileHeaderOffset fileHeaderOffset,
        long xrefOffset,
        IInputBytes bytes,
        ISeekableTokenScanner scanner)
    {
        // If the %PDF- version header appears at some offset in the file then treat everything as shifted.
        if (fileHeaderOffset.Value > 0)
        {
            scanner.Seek(xrefOffset + fileHeaderOffset.Value);
            if (TryReadXrefToken(scanner))
            {
                return (xrefOffset + fileHeaderOffset.Value, XrefOffsetCorrection.FileHeaderOffset);
            }
        }

        // Read a +/-10 chunk around the offset to see if we're close.
        var buffer = new byte[20];
        var offset = Math.Max(0, xrefOffset - 10);
        bytes.Seek(offset);

        var read = bytes.Read(buffer);

        if (read < buffer.Length)
        {
            return null;
        }

        var str = OtherEncodings.BytesAsLatin1String(buffer);

        var xrefIx = str.IndexOf("xref", StringComparison.OrdinalIgnoreCase);
        if (xrefIx < 0)
        {
            return null;
        }

        var actualOffset = offset + xrefIx;
        scanner.Seek(actualOffset);
        if (TryReadXrefToken(scanner))
        {
            return (actualOffset, XrefOffsetCorrection.Random);
        }

        return null;
    }

    private enum XrefTableReadMode
    {
        SubsectionHeader = 2,
        Entry = 3,
    }

}