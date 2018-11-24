namespace UglyToad.PdfPig.Parser.Parts.CrossReference
{
    using System.Collections.Generic;
    using Cos;
    using Exceptions;
    using Filters;
    using PdfPig.CrossReference;
    using Tokens;
    using Util;

    internal class CrossReferenceStreamParser
    {
        private readonly IFilterProvider filterProvider;

        public CrossReferenceStreamParser(IFilterProvider filterProvider)
        {
            this.filterProvider = filterProvider;
        }

        /// <summary>
        /// Parses through the unfiltered stream and populates the xrefTable HashMap.
        /// </summary>
        public CrossReferenceTablePart Parse(long streamOffset, StreamToken stream)
        {
            var decoded = stream.Decode(filterProvider);

            var fieldSizes = new CrossReferenceStreamFieldSize(stream.StreamDictionary);

            var lineCount = decoded.Count / fieldSizes.LineLength;
            
            long previousOffset = -1;
            if (stream.StreamDictionary.TryGet(NameToken.Prev, out var prevToken) && prevToken is NumericToken prevNumeric)
            {
                previousOffset = prevNumeric.Long;
            }

            var builder = new CrossReferenceTablePartBuilder
            {
                Offset = streamOffset,
                Previous = previousOffset,
                Dictionary = stream.StreamDictionary,
                XRefType = CrossReferenceType.Stream
            };

            var objectNumbers = GetObjectNumbers(stream.StreamDictionary);

            var lineNumber = 0;
            var lineBuffer = new byte[fieldSizes.LineLength];
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

                ReadNextStreamObject(type, objectNumber, fieldSizes, builder, lineBuffer);

                lineNumber++;
            }

            return builder.Build();
        }

        private static void ReadNextStreamObject(int type, long objectNumber, CrossReferenceStreamFieldSize fieldSizes,
            CrossReferenceTablePartBuilder builder, byte[] lineBuffer)
        {
            switch (type)
            {
                case 0:
                    // Ignore free objects.
                    break;
                case 1:
                    // Non object stream entries.
                    int offset = 0;
                    for (int i = 0; i < fieldSizes.Field2Size; i++)
                    {
                        offset += (lineBuffer[i + fieldSizes.Field1Size] & 0x00ff) << ((fieldSizes.Field2Size - i - 1) * 8);
                    }
                    int genNum = 0;
                    for (int i = 0; i < fieldSizes.Field3Size; i++)
                    {
                        genNum += (lineBuffer[i + fieldSizes.Field1Size + fieldSizes.Field2Size] & 0x00ff) << ((fieldSizes.Field3Size - i - 1) * 8);
                    }

                    builder.Add(objectNumber, genNum, offset);

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
                    int objstmObjNr = 0;
                    for (int i = 0; i < fieldSizes.Field2Size; i++)
                    {
                        objstmObjNr += (lineBuffer[i + fieldSizes.Field1Size] & 0x00ff) << ((fieldSizes.Field2Size - i - 1) * 8);
                    }

                    builder.Add(objectNumber, 0, -objstmObjNr);

                    break;
            }
        }

        private static List<long> GetObjectNumbers(DictionaryToken dictionary)
        {
            if (!dictionary.TryGet(NameToken.Size, out var sizeToken) || !(sizeToken is NumericToken sizeNumeric))
            {
                throw new PdfDocumentFormatException($"The stream dictionary must contain a numeric size value: {dictionary}.");
            }

            var indexArray = new[] { 0, sizeNumeric.Int };

            if (dictionary.TryGet(NameToken.Index, out var indexToken) && indexToken is ArrayToken indexArrayToken)
            {
                indexArray = new[]
                {
                    indexArrayToken.GetNumeric(0).Int,
                    indexArrayToken.GetNumeric(1).Int
                };
            }

            List<long> objNums = new List<long>();
            
            var firstObjectNumber = indexArray[0];
            var size = indexArray[1];

            for (var i = 0; i < size; i++)
            {
                objNums.Add(firstObjectNumber + i);
            }

            return objNums;
        }
    }
}
