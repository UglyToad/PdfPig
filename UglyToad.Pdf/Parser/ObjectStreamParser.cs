namespace UglyToad.Pdf.Parser
{
    using System;
    using System.Collections.Generic;
    using ContentStream.TypedAccessors;
    using Cos;
    using Filters;
    using IO;
    using Logging;
    using Parts;

    internal class ObjectStreamParser
    {
        private readonly ILog log;
        private readonly IFilterProvider filterProvider;
        private readonly CosBaseParser baseParser;

        public ObjectStreamParser(ILog log, IFilterProvider filterProvider, CosBaseParser baseParser)
        {
            this.log = log;
            this.filterProvider = filterProvider;
            this.baseParser = baseParser;
        }

        public IReadOnlyList<CosObject> Parse(RawCosStream stream, CosObjectPool pool)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            //need to first parse the header.
            var numberOfObjects = stream.Dictionary.GetIntOrDefault(CosName.N);
            var objectNumbers = new List<long>(numberOfObjects);

            var streamObjects = new List<CosObject>(numberOfObjects);

            var bytes = stream.Decode(filterProvider);

            var reader = new RandomAccessBuffer(bytes);

            for (int i = 0; i < numberOfObjects; i++)
            {
                long objectNumber = ObjectHelper.ReadObjectNumber(reader);
                // skip offset
                ReadHelper.ReadLong(reader);
                objectNumbers.Add(objectNumber);
            }

            CosObject obj;
            CosBase cosObject;
            int objectCounter = 0;
            while ((cosObject = baseParser.Parse(reader, pool)) != null)
            {
                obj = new CosObject(cosObject);
                obj.SetGenerationNumber(0);

                if (objectCounter >= objectNumbers.Count)
                {
                    log.Error("/ObjStm (object stream) has more objects than /N " + numberOfObjects);
                    break;
                }

                obj.SetObjectNumber(objectNumbers[objectCounter]);
                streamObjects.Add(obj);

                // According to the spec objects within an object stream shall not be enclosed 
                // by obj/endobj tags, but there are some pdfs in the wild using those tags 
                // skip endobject marker if present
                if (!reader.IsEof() && reader.Peek() == 'e')
                {
                    ReadHelper.ReadLine(reader);
                }

                objectCounter++;
            }

            return streamObjects;
        }
    }
}
