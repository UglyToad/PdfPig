namespace UglyToad.Pdf.Filters
{
    using Cos;

    public class DecodeResult
    {
        /** Default decode result. */
        public static DecodeResult DEFAULT = new DecodeResult(new CosDictionary());

        private readonly CosDictionary parameters;
        private PDJPXColorSpace colorSpace;

        public DecodeResult(CosDictionary parameters)
        {
            this.parameters = parameters;
        }

        public DecodeResult(CosDictionary parameters, PDJPXColorSpace colorSpace)
        {
            this.parameters = parameters;
            this.colorSpace = colorSpace;
        }

        /**
         * Returns the stream parameters, repaired using the embedded stream data.
         * @return the repaired stream parameters, or an empty dictionary
         */
        public CosDictionary getParameters()
        {
            return parameters;
        }

        /**
         * Returns the embedded JPX color space, if any.
         * @return the the embedded JPX color space, or null if there is none.
         */
        public PDJPXColorSpace getJPXColorSpace()
        {
            return colorSpace;
        }

        // Sets the JPX color space
        void setColorSpace(PDJPXColorSpace colorSpace)
        {
            this.colorSpace = colorSpace;
        }
    }

    public class PDJPXColorSpace { }

}
