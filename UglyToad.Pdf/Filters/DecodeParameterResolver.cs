namespace UglyToad.Pdf.Filters
{
    using System;
    using ContentStream;
    using ContentStream.TypedAccessors;
    using Cos;
    using Logging;

    internal class DecodeParameterResolver : IDecodeParameterResolver
    {
        private readonly ILog log;

        public DecodeParameterResolver(ILog log)
        {
            this.log = log;
        }

        public ContentStreamDictionary GetFilterParameters(ContentStreamDictionary streamDictionary, int index)
        {
            if (streamDictionary == null)
            {
                throw new ArgumentNullException(nameof(streamDictionary));
            }

            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Index must be 0 or greater");
            }

            var filter = streamDictionary.GetDictionaryObject(CosName.FILTER, CosName.F);

            var parameters = streamDictionary.GetDictionaryObject(CosName.DECODE_PARMS, CosName.DP);

            switch (filter)
            {
                case CosName _:
                    if (parameters is ContentStreamDictionary dict)
                    {
                        return dict;
                    }
                    break;
                case COSArray array:
                    if (parameters is COSArray arr)
                    {
                        if (index < arr.size() && array.getObject(index) is ContentStreamDictionary dictionary)
                        {
                            return dictionary;
                        }
                    }
                    break;
                default:
                    if (parameters != null)
                    {
                        log?.Error("Expected the decode parameters for the stream to be either an array or dictionary");
                    }
                    break;
            }

            return new ContentStreamDictionary();
        }
    }
}