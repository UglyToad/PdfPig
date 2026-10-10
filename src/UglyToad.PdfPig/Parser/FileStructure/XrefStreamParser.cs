namespace UglyToad.PdfPig.Parser.FileStructure;

using Core;
using Filters;
using Logging;
using System.Diagnostics.CodeAnalysis;
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
        IFilterProvider filterProvider,
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

            var fieldSizes = new XrefFieldSize(dictArray);

            var stream = new StreamToken(WithValidColumns(dictToken, fieldSizes.LineLength, log), data);

            var decoded = stream.Decode(filterProvider).Span;

            var lineCount = decoded.Length / fieldSizes.LineLength;

            var objectNumbers = GetObjectNumbers(dictToken);

            var lineNumber = 0;

            var numbers = new List<(long obj, int gen, XrefLocation location)>();

            foreach (var objectNumber in objectNumbers)
            {
                if (lineNumber >= lineCount)
                {
                    break;
                }

                var lineBuffer = decoded.Slice(lineNumber * fieldSizes.LineLength, fieldSizes.LineLength);

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
                numbers.ToDictionary(x => new IndirectReference(x.obj, x.gen), x => x.location),
                dictToken,
                offsetCorrectionType,
                offsetCorrection);
        }
        catch (NotSupportedException)
        {
            // A filter this build cannot decode will not decode on a second attempt, and brute
            // forcing the file cannot get past it either. Letting it out keeps the reason in the
            // exception the caller sees, rather than ending as "could not find an xref trailer".
            throw;
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

    /// <summary>
    /// The rows of a predicted cross reference stream are its entries, so where the /Columns decode
    /// parameter cannot be used the entry length from /W is used instead. Otherwise the decode fails
    /// and every entry in the stream is lost.
    /// </summary>
    private static DictionaryToken WithValidColumns(DictionaryToken dictionary, int lineLength, ILog log)
    {
        // TODO - Should be in Lenient parsing only
        if (!dictionary.TryGet(NameToken.DecodeParms, out var decodeParmsToken))
        {
            return dictionary;
        }

        if (decodeParmsToken is DictionaryToken decodeParms)
        {
            return TryGetWithValidColumns(decodeParms, lineLength, log, out var corrected)
                ? dictionary.With(NameToken.DecodeParms, corrected)
                : dictionary;
        }

        // With a filter array, one entry per filter.
        if (decodeParmsToken is ArrayToken decodeParmsArray)
        {
            IToken[]? correctedArray = null;

            for (var i = 0; i < decodeParmsArray.Length; i++)
            {
                if (decodeParmsArray[i] is DictionaryToken filterParms
                    && TryGetWithValidColumns(filterParms, lineLength, log, out var corrected))
                {
                    correctedArray ??= decodeParmsArray.Data.ToArray();
                    correctedArray[i] = corrected;
                }
            }

            return correctedArray is null
                ? dictionary
                : dictionary.With(NameToken.DecodeParms, new ArrayToken(correctedArray));
        }

        return dictionary;

        static bool TryGetWithValidColumns(DictionaryToken decodeParms, int lineLength, ILog log, [NotNullWhen(true)] out DictionaryToken? corrected)
        {
            corrected = null;

            if (!decodeParms.TryGet(NameToken.Columns, out var columnsToken)
                || columnsToken is NumericToken columns && columns.Double >= 1 && columns.Double <= int.MaxValue)
            {
                return false;
            }

            log.Warn($"Invalid /Columns {columnsToken} in the decode parameters of a cross reference stream, using the entry length {lineLength} from /W.");

            corrected = decodeParms.With(NameToken.Columns, new NumericToken(lineLength));
            return true;
        }
    }

    private static void ReadNextStreamObject(
        int type,
        long objectNumber,
        XrefFieldSize fieldSizes,
        List<(long, int, XrefLocation)> results,
        ReadOnlySpan<byte> lineBuffer)
    {
        switch (type)
        {
            case 0:
                // Ignore free objects.
                break;
            case 1:
                var offset = ReadUnsigned(
                    lineBuffer,
                    fieldSizes.Field1Size,
                    fieldSizes.Field2Size);

                var genNum = ReadUnsigned(
                    lineBuffer,
                    fieldSizes.Field1Size + fieldSizes.Field2Size,
                    fieldSizes.Field3Size);

                if (offset < 0)
                { 
                    throw new PdfDocumentFormatException(
                        $"Location with negative offset {offset} found for object {objectNumber}");
                }

                results.Add((objectNumber, (int)genNum, XrefLocation.File(offset)));

                break;
            case 2:
                /*
                 * object stored in object stream: 
                 * 2nd argument is object number of object stream
                 * 3rd argument is index of object within object stream
                 * 
                 */
            
                var objectStreamNumber = ReadUnsigned(
                    lineBuffer,
                    fieldSizes.Field1Size,
                    fieldSizes.Field2Size);

                var streamIndex = ReadUnsigned(
                    lineBuffer,
                    fieldSizes.Field1Size + fieldSizes.Field2Size,
                    fieldSizes.Field3Size);

                if (objectStreamNumber < 0)
                {
                    throw new PdfDocumentFormatException(
                        $"Location with negative or zero object stream number {objectStreamNumber} found for object {objectNumber}");
                }

                if (streamIndex < 0)
                {
                    throw new PdfDocumentFormatException(
                        $"Location with negative stream index {streamIndex} found for object {objectNumber} in stream {objectStreamNumber}");
                }

                results.Add((objectNumber, 0, XrefLocation.Stream(objectStreamNumber, (int)streamIndex)));

                break;
        }
    }

    private static long ReadUnsigned(ReadOnlySpan<byte> buffer, int start, int width)
    {
        long value = 0;

        for (int i = 0; i < width; i++)
        {
            value <<= 8;
            value |= buffer[start + i];
        }

        return value;
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
    private readonly struct XrefFieldSize
    {
        /// <summary>
        /// The type of the entry.
        /// </summary>
        public readonly int Field1Size;

        /// <summary>
        /// Type 0 and 2 is the object number, Type 1 this is the byte offset from beginning of file.
        /// </summary>
        public readonly int Field2Size;

        /// <summary>
        /// For types 0 and 1 this is the generation number. For type 2 it is the stream index.
        /// </summary>
        public readonly int Field3Size;

        /// <summary>
        /// How many bytes are in a line.
        /// </summary>
        public readonly int LineLength;

        public XrefFieldSize(ArrayToken wArray)
        {
            if (wArray.Data.Count < 3)
            {
                throw new PdfDocumentFormatException($"There must be at least 3 entries in a W entry for a stream dictionary: {wArray}.");
            }

            Field1Size = GetFieldSize(wArray, 0);
            Field2Size = GetFieldSize(wArray, 1);
            Field3Size = GetFieldSize(wArray, 2);

            /* Not enforced for now as we don't have the lenient flag
            // 7.5.8.2: "A value of zero shall not be used for the second element of the array."
            if (Field2Size == 0)
            {
                throw new PdfDocumentFormatException($"The second entry in a W entry for a stream dictionary must not be zero: {wArray}.");
            }
            */

            // No line longer than a span can be read, so a sum that does not fit in an int is invalid.
            var lineLength = (long)Field1Size + Field2Size + Field3Size;
            if (lineLength > int.MaxValue)
            {
                throw new PdfDocumentFormatException($"The entries in a W entry for a stream dictionary are too large: {wArray}.");
            }

            if (lineLength == 0)
            {
                throw new PdfDocumentFormatException($"The entries in a W entry for a stream dictionary must not all be zero: {wArray}.");
            }

            LineLength = (int)lineLength;
        }

        private static int GetFieldSize(ArrayToken wArray, int index)
        {
            var size = wArray.GetNumeric(index).Int;

            if (size < 0)
            {
                throw new PdfDocumentFormatException($"Invalid field size {size} at index {index} in the W entry for a stream dictionary: {wArray}.");
            }

            return size;
        }
    }
}