namespace UglyToad.Pdf.Parser
{
    using System;
    using System.Collections.Generic;
    using Cos;
    using IO;
    using Parts;

    internal class PDFObjectStreamParser : BaseParser
    {
        /**
     * Log instance.
     */

        private List<CosObject> streamObjects = null;
        private readonly COSStream stream;

        /**
     * Constructor.
     *
     * @param stream The stream to parse.
     * @param document The document for the current parsing.
     * @throws IOException If there is an error initializing the stream.
     */
        public PDFObjectStreamParser(COSStream stream, COSDocument document) : base(new BufferSequentialSource(new RandomAccessBuffer()))
        {
            throw new NotImplementedException();
            //super(new InputStreamSource(stream.createInputStream()));
            this.stream = stream;
            this.document = document;
        }

        /**
     * This will parse the tokens in the stream.  This will close the
     * stream when it is finished parsing.
     *
     * @throws IOException If there is an error while parsing the stream.
     */
        public void parse(CosBaseParser baseParser, CosObjectPool pool)
        {
            try
            {
                //need to first parse the header.
                int numberOfObjects = stream.getInt("N");
                List<long> objectNumbers = new List<long>(numberOfObjects);
                streamObjects = new List<CosObject>(numberOfObjects);
                for (int i = 0; i < numberOfObjects; i++)
                {
                    long objectNumber = readObjectNumber();
                    // skip offset
                    readLong();
                    objectNumbers.Add(objectNumber);
                }
                CosObject obj;
                CosBase cosObject;
                int objectCounter = 0;
                while ((cosObject = baseParser.Parse(null, pool)) != null)
                {
                    obj = new CosObject(cosObject);
                    obj.SetGenerationNumber(0);
                    if (objectCounter >= objectNumbers.Count)
                    {
                        //LOG.error("/ObjStm (object stream) has more objects than /N " + numberOfObjects);
                        break;
                    }
                    obj.SetObjectNumber(objectNumbers[objectCounter]);
                    streamObjects.Add(obj);
               
                    // According to the spec objects within an object stream shall not be enclosed 
                    // by obj/endobj tags, but there are some pdfs in the wild using those tags 
                    // skip endobject marker if present
                    if (!seqSource.isEOF() && seqSource.peek() == 'e')
                    {
                        readLine();
                    }
                    objectCounter++;
                }
            }
            finally
            {
                seqSource.Dispose();
            }
        }

        /**
     * This will get the objects that were parsed from the stream.
     *
     * @return All of the objects in the stream.
     */
        public List<CosObject> getObjects()
        {
            return streamObjects;
        }
    }
}