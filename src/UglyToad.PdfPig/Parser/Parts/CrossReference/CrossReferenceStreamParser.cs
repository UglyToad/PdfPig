namespace UglyToad.PdfPig.Parser.Parts.CrossReference
{
    using Core;
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
        public CrossReferenceTablePart Parse(long streamOffset, long? fromTableAtOffset, StreamToken stream)
        {
            var decoded = stream.Decode(filterProvider).Span;

            var fieldSizes = new CrossReferenceStreamFieldSize(stream.StreamDictionary);

            var lineCount = decoded.Length / fieldSizes.LineLength;
            
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
                XRefType = CrossReferenceType.Stream,
                TiedToPreviousAtOffset = fromTableAtOffset
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
                    var objstmObjNr = 0;
                    for (var i = 0; i < fieldSizes.Field2Size; i++)
                    {
                        objstmObjNr += (lineBuffer[i + fieldSizes.Field1Size] & 0x00ff) << ((fieldSizes.Field2Size - i - 1) * 8);
                    }

                    builder.Add(objectNumber, 0, -objstmObjNr);

                    break;
            }
        }

        private static IEnumerable<long> GetObjectNumbers(DictionaryToken dictionary)
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

            return objNums;
        }
    }
}
