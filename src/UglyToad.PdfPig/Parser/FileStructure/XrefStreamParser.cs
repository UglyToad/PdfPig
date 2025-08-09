namespace UglyToad.PdfPig.Parser.FileStructure;

using Core;
using Filters;
using Logging;
using System.Linq;
using Tokenization.Scanner;
using Tokens;
using Util;

internal static class XrefStreamParser
{
    public static XrefStream? TryReadStreamAtOffset(
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

        var offsetCorrectionType = XrefOffsetCorrection.None;
        var offsetCorrection = 0L;

        bytes.Seek(xrefOffset);
        if (!TryReadStreamObjAt(xrefOffset, scanner, out var dictToken)
            || dictToken == null)
        {
            log.Debug($"Did not find the stream at {xrefOffset} attempting correction");
            var recovered = TryRecoverOffset(fileHeaderOffset, xrefOffset, scanner);

            if (recovered == null
                || !TryReadStreamObjAt(recovered.Value.correctOffset, scanner, out var streamDict)
                || streamDict == null)
            {
                return null;
            }

            dictToken = streamDict;

            offsetCorrection = recovered.Value.correctOffset - xrefOffset;
            offsetCorrectionType = recovered.Value.correctionType;
            xrefOffset = recovered.Value.correctOffset;
        }

        if (!dictToken.TryGet(NameToken.Type, out NameToken dictType)
            || dictType != NameToken.Xref)
        {
            return null;
        }

        if (!dictToken.TryGet(NameToken.W, out ArrayToken dictArray))
        {
            return null;
        }

        try
        {
            var streamData = ReadStreamTolerant(bytes);

            if (!streamData.to.HasValue)
            {
                return null;
            }

            var dataLen = streamData.to.Value - streamData.from;

            if (dataLen <= 0)
            {
                return null;
            }

            bytes.Seek(streamData.from);

            var data = new byte[dataLen];
            var readCount = bytes.Read(data);

            if (readCount != dataLen)
            {
                return null;
            }

            var stream = new StreamToken(dictToken, data);

            var decoded = stream.Decode(DefaultFilterProvider.Instance).Span;

            var fieldSizes = new XrefFieldSize(dictArray);

            var lineCount = decoded.Length / fieldSizes.LineLength;

            var objectNumbers = GetObjectNumbers(dictToken);

            var lineNumber = 0;
            Span<byte> lineBuffer = fieldSizes.LineLength <= 64
                ? stackalloc byte[fieldSizes.LineLength]
                : new byte[fieldSizes.LineLength];

            var numbers = new List<(long obj, int gen, int off)>();

            foreach (var objectNumber in objectNumbers)
            {
                if (lineNumber >= lineCount)
                {
                    break;
                }

                var byteOffset = lineNumber * fieldSizes.LineLength;

                for (var i = 0; i < fieldSizes.LineLength; i++)
                {
                    lineBuffer[i] = decoded[byteOffset + i];
                }

                int type;
                if (fieldSizes.Field1Size == 0)
                {
                    type = 1;
                }
                else
                {
                    type = 0;

                    for (var i = 0; i < fieldSizes.Field1Size; i++)
                    {
                        type += (lineBuffer[i] & 0x00ff) << ((fieldSizes.Field1Size - i - 1) * 8);
                    }
                }

                ReadNextStreamObject(type, objectNumber, fieldSizes, numbers, lineBuffer);

                lineNumber++;
            }

            return new XrefStream(
                xrefOffset,
                numbers.ToDictionary(x => new IndirectReference(x.obj, x.gen), x => (long)x.off),
                dictToken,
                offsetCorrectionType,
                offsetCorrection);
        }
        catch (Exception ex)
        {
            log.Error($"Failed to parse the XRef stream at {xrefOffset}", ex);
            return null;
        }
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
        ISeekableTokenScanner scanner)
    {
        // If the %PDF- version header appears at some offset in the file then treat everything as shifted.
        if (fileHeaderOffset.Value > 0)
        {
            if (TryReadStreamObjAt(xrefOffset + fileHeaderOffset.Value, scanner, out _))
            {
                return (xrefOffset + fileHeaderOffset.Value, XrefOffsetCorrection.FileHeaderOffset);
            }
        }

        return null;
    }

    private static void ReadNextStreamObject(
        int type,
        long objectNumber,
        XrefFieldSize fieldSizes,
        List<(long, int, int)> results,
        ReadOnlySpan<byte> lineBuffer)
    {
        switch (type)
        {
            case 0:
                // Ignore free objects.
                break;
            case 1:
                // Non object stream entries.
                var offset = 0;
                for (var i = 0; i < fieldSizes.Field2Size; i++)
                {
                    offset += (lineBuffer[i + fieldSizes.Field1Size] & 0x00ff) << ((fieldSizes.Field2Size - i - 1) * 8);
                }
                var genNum = 0;
                for (var i = 0; i < fieldSizes.Field3Size; i++)
                {
                    genNum += (lineBuffer[i + fieldSizes.Field1Size + fieldSizes.Field2Size] & 0x00ff) << ((fieldSizes.Field3Size - i - 1) * 8);
                }

                results.Add((objectNumber, genNum, offset));

                break;
            case 2:
                /*
                 * object stored in object stream: 
                 * 2nd argument is object number of object stream
                 * 3rd argument is index of object within object stream
                 * 
                 * For sequential PDFParser we do not need this information
                 * because
                 * These objects are handled by the dereferenceObjects() method
                 * since they're only pointing to object numbers
                 * 
                 * However for XRef aware parsers we have to know which objects contain
                 * object streams. We will store this information in normal xref mapping
                 * table but add object stream number with minus sign in order to
                 * distinguish from file offsets
                 */
                var objstmObjNr = 0;
                for (var i = 0; i < fieldSizes.Field2Size; i++)
                {
                    objstmObjNr += (lineBuffer[i + fieldSizes.Field1Size] & 0x00ff) << ((fieldSizes.Field2Size - i - 1) * 8);
                }

                results.Add((objectNumber, 0, -objstmObjNr));

                break;
        }
    }

    private static (long from, long? to) ReadStreamTolerant(IInputBytes bytes)
    {
        var buffer = new CircularByteBuffer("endstream ".Length);

        var startMarker = bytes.CurrentOffset;
        long? endMarker = null;

        while (bytes.CurrentByte == '>' && bytes.MoveNext())
        {
        }

        bool IsStreamWhitespace()
        {
            return bytes.CurrentByte == (byte)' '
                   || bytes.CurrentByte == (byte)'\r'
                   || bytes.CurrentByte == (byte)'\n';
        }

        var isWhitespaceActive = IsStreamWhitespace();

        do
        {

            // Normalize whitespace.
            if (IsStreamWhitespace())
            {
                buffer.Add((byte)' ');

                if (isWhitespaceActive)
                {
                    startMarker = bytes.CurrentOffset;
                }
            }
            else
            {
                buffer.Add(bytes.CurrentByte);
                isWhitespaceActive = false;
            }

            if (buffer.EndsWith("endstream "))
            {
                endMarker = bytes.CurrentOffset - "endstream ".Length;
                break;
            }

            if (buffer.EndsWith("stream "))
            {
                startMarker = bytes.CurrentOffset;

                isWhitespaceActive = IsStreamWhitespace();
            }
            else if (buffer.EndsWith("endobj "))
            {
                endMarker = bytes.CurrentOffset - "endobj ".Length;
                break;
            }
        } while (bytes.MoveNext());

        return (startMarker, endMarker);
    }

    private static ReadOnlySpan<long> GetObjectNumbers(DictionaryToken dictionary)
    {
        //  The number one greater than the highest object number used in this section or in any section for which this is an update.
        if (!dictionary.TryGet(NameToken.Size, out var sizeToken) || !(sizeToken is NumericToken sizeNumeric))
        {
            throw new PdfDocumentFormatException($"The stream dictionary must contain a numeric size value: {dictionary}.");
        }

        var objNums = new List<long>();

        if (dictionary.TryGet(NameToken.Index, out var indexToken) && indexToken is ArrayToken indexArrayToken)
        {
            // An array containing a pair of integers for each subsection in this section. 
            // Pair[0] is the first object number in the subsection; Pair[1] is the number of entries in the subsection.
            for (var i = 0; i < indexArrayToken.Length; i += 2)
            {
                var firstObjectNumber = indexArrayToken.GetNumeric(i).Int;
                var size = indexArrayToken.GetNumeric(i + 1).Int;

                for (var j = 0; j < size; j++)
                {
                    objNums.Add(firstObjectNumber + j);
                }
            }
        }
        else
        {
            for (var i = 0; i < sizeNumeric.Int; i++)
            {
                objNums.Add(i);
            }
        }

#if NET
            return System.Runtime.InteropServices.CollectionsMarshal.AsSpan(objNums);
#else
        return objNums.ToArray();
#endif
    }

    private static bool TryReadStreamObjAt(long offset, ISeekableTokenScanner scanner, out DictionaryToken? dictionary)
    {
        dictionary = null;

        scanner.Seek(offset);
        if (scanner.TryReadToken(out NumericToken _)
            && scanner.TryReadToken(out NumericToken _)
            && scanner.TryReadToken(out OperatorToken opToken)
            && ReferenceEquals(opToken, OperatorToken.StartObject)
            && scanner.TryReadToken(out DictionaryToken dictToken))
        {
            dictionary = dictToken;
            return true;
        }

        return false;
    }


    /// <summary>
    /// The array representing the size of the fields in a cross reference stream.
    /// </summary>
    private class XrefFieldSize
    {
        /// <summary>
        /// The type of the entry.
        /// </summary>
        public int Field1Size { get; }

        /// <summary>
        /// Type 0 and 2 is the object number, Type 1 this is the byte offset from beginning of file.
        /// </summary>
        public int Field2Size { get; }

        /// <summary>
        /// For types 0 and 1 this is the generation number. For type 2 it is the stream index.
        /// </summary>
        public int Field3Size { get; }

        /// <summary>
        /// How many bytes are in a line.
        /// </summary>
        public int LineLength { get; }

        public XrefFieldSize(ArrayToken wArray)
        {
            if (wArray.Data.Count < 3)
            {
                throw new PdfDocumentFormatException($"There must be at least 3 entries in a W entry for a stream dictionary: {wArray}.");
            }

            Field1Size = wArray.GetNumeric(0).Int;
            Field2Size = wArray.GetNumeric(1).Int;
            Field3Size = wArray.GetNumeric(2).Int;

            LineLength = Field1Size + Field2Size + Field3Size;
        }
    }
}