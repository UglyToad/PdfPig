using System;

namespace UglyToad.Pdf.Filters
{
    using Cos;
    using IO;

    public abstract class Filter
    {
        /**
         * Decodes data, producing the original non-encoded data.
         * @param encoded the encoded byte stream
         * @param decoded the stream where decoded data will be written
         * @param parameters the parameters used for decoding
         * @param index the index to the filter being decoded
         * @return repaired parameters dictionary, or the original parameters dictionary
         * @throws IOException if the stream cannot be decoded
         */
        public abstract DecodeResult decode(IInputStream encoded, IOutputStream decoded, CosDictionary parameters,
                                int index);

        /**
         * Encodes data.
         * @param input the byte stream to encode
         * @param encoded the stream where encoded data will be written
         * @param parameters the parameters used for encoding
         * @param index the index to the filter being encoded
         * @throws IOException if the stream cannot be encoded
         */
        public void encode(IInputStream input, IOutputStream encoded, CosDictionary parameters,
                                int index)
        {
            encode(input, encoded, parameters.asUnmodifiableDictionary());
        }

        // implemented in subclasses
        protected abstract void encode(IInputStream input, IOutputStream encoded,
                                       CosDictionary parameters);

        // gets the decode params for a specific filter index, this is used to
        // normalise the DecodeParams entry so that it is always a dictionary
        protected CosDictionary getDecodeParams(CosDictionary dictionary, int index)
        {
            CosBase filter = dictionary.getDictionaryObject(CosName.FILTER, CosName.F);
            CosBase obj = dictionary.getDictionaryObject(CosName.DECODE_PARMS, CosName.DP);
            if (filter is CosName && obj is CosDictionary)
            {
                // PDFBOX-3932: The PDF specification requires "If there is only one filter and that 
                // filter has parameters, DecodeParms shall be set to the filter’s parameter dictionary" 
                // but tests show that Adobe means "one filter name object".
                return (CosDictionary)obj;
            }

            if (filter is COSArray && obj is COSArray)
            {
                COSArray array = (COSArray)obj;
                if (index < array.size())
                {
                    return (CosDictionary)array.getObject(index);
                }
            }
            else if (obj != null && !(filter is COSArray || obj is COSArray))
            {
                //LOG.error("Expected DecodeParams to be an Array or Dictionary but found " + obj.getClass().getName());
            }
            return new CosDictionary();
        }

        /**
         * Finds a suitable image reader for a format.
         *
         * @param formatName The format to search for.
         * @param errorCause The probably cause if something goes wrong.
         * @return The image reader for the format.
         * @throws MissingImageReaderException if no image reader is found.
         */
        protected static object findImageReader(String formatName, String errorCause)
        {
            throw new NotImplementedException();
        }
    }
}
