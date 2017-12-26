namespace UglyToad.Pdf.Parser.Parts.CrossReference
{
    using System.Collections.Generic;
    using System.IO;
    using ContentStream;
    using ContentStream.TypedAccessors;
    using Cos;
    using Filters;

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
        public CrossReferenceTablePart Parse(long streamOffset, PdfRawStream stream)
        {
            var w = stream.Dictionary.GetDictionaryObject(CosName.W);
            if (!(w is COSArray format))
            {
                throw new IOException("/W array is missing in Xref stream");
            }
            
            var objNums = GetObjectNumbers(stream);

            /*
             * Calculating the size of the line in bytes
             */
            int w0 = format.getInt(0);
            int w1 = format.getInt(1);
            int w2 = format.getInt(2);
            int lineSize = w0 + w1 + w2;

            var decoded = stream.Decode(filterProvider);

            var lineCount = decoded.Length / lineSize;
            var lineNumber = 0;

            var builder = new CrossReferenceTablePartBuilder
            {
                Offset = streamOffset,
                Previous = stream.Dictionary.GetLongOrDefault(CosName.PREV),
                Dictionary = stream.Dictionary,
                XRefType = CrossReferenceType.Stream
            };

            using (IEnumerator<long> objIter = objNums.GetEnumerator())
            {
                var currLine = new byte[lineSize];

                while (lineNumber < lineCount && objIter.MoveNext())
                {
                    var byteOffset = lineNumber * lineSize;
                    for (int i = 0; i < lineSize; i++)
                    {
                        currLine[i] = decoded[byteOffset + i];
                    }

                    int type;
                    if (w0 == 0)
                    {
                        // "If the first element is zero, 
                        // the type field shall not be present, and shall default to type 1"
                        type = 1;
                    }
                    else
                    {
                        type = 0;
                        /*
                         * Grabs the number of bytes specified for the first column in
                         * the W array and stores it.
                         */
                        for (int i = 0; i < w0; i++)
                        {
                            type += (currLine[i] & 0x00ff) << ((w0 - i - 1) * 8);
                        }
                    }
                    //Need to remember the current objID
                    long objectId = objIter.Current;
                    /*
                     * 3 different types of entries.
                     */
                    switch (type)
                    {
                        case 0:
                            /*
                             * Skipping free objects
                             */
                            break;
                        case 1:
                            int offset = 0;
                            for (int i = 0; i < w1; i++)
                            {
                                offset += (currLine[i + w0] & 0x00ff) << ((w1 - i - 1) * 8);
                            }
                            int genNum = 0;
                            for (int i = 0; i < w2; i++)
                            {
                                genNum += (currLine[i + w0 + w1] & 0x00ff) << ((w2 - i - 1) * 8);
                            }

                            builder.Add(objectId, genNum, offset);

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
                            for (int i = 0; i < w1; i++)
                            {
                                objstmObjNr += (currLine[i + w0] & 0x00ff) << ((w1 - i - 1) * 8);
                            }

                            builder.Add(objectId, 0, -objstmObjNr);

                            break;
                    }

                    lineNumber++;
                }
            }

            return builder.AsCrossReferenceTablePart();
        }

        private static List<long> GetObjectNumbers(PdfRawStream stream)
        {
            var indexArray = (COSArray) stream.Dictionary.GetDictionaryObject(CosName.INDEX);
            
            // If Index doesn't exist, we will use the default values.
            if (indexArray == null)
            {
                indexArray = new COSArray();
                indexArray.add(CosInt.Zero);
                indexArray.add(stream.Dictionary.GetDictionaryObject(CosName.SIZE));
            }

            List<long> objNums = new List<long>();
            
            // Populates objNums with all object numbers available

            for (int i = 0; i < indexArray.Count; i+=2)
            {
                var longId = ((CosInt) indexArray.get(i)).AsLong();
                var size = ((CosInt)indexArray.get(i + 1)).AsInt();

                for (int j = 0; j < size; j++)
                {
                    objNums.Add(longId + j);
                }
            }

            return objNums;
        }
    }
}
