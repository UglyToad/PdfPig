namespace UglyToad.Pdf.Cos
{
    using System.IO;
    using Core;

    internal class CosBoolean : CosBase, ICosStreamWriter
    {
        /**
     * The true boolean token.
     */
        public static readonly byte[] TrueBytes = { 116, 114, 117, 101 }; //"true".getBytes( "ISO-8859-1" );
        /**
         * The false boolean token.
         */
        public static readonly byte[] FalseBytes = { 102, 97, 108, 115, 101 }; //"false".getBytes( "ISO-8859-1" );

        /**
         * The PDF true value.
         */
        public static readonly CosBoolean True = new CosBoolean(true);

        /**
         * The PDF false value.
         */
        public static readonly CosBoolean False = new CosBoolean(false);

        public bool Value { get; }

        private CosBoolean(bool value)
        {
            Value = value;
        }

        /**
         * This will get the boolean value.
         *
         * @param value Parameter telling which boolean value to get.
         *
         * @return The single boolean instance that matches the parameter.
         */
        public static explicit operator CosBoolean(bool value)
        {
            return value ? True : False;
        }

        /**
         * Return a string representation of this object.
         *
         * @return The string value of this object.
         */
        public override string ToString()
        {
            return Value.ToString();
        }

        /**
         * This will write this object out to a PDF stream.
         *
         * @param output The stream to write this object out to.
         *
         * @throws IOException If an error occurs while writing out this object.
         */
        public void WriteToPdfStream(StreamWriter output)
        {
            output.Write(Value ? TrueBytes : FalseBytes);
        }

        public override object Accept(ICosVisitor visitor)
        {
            return visitor.VisitFromBoolean(this);
        }
    }
}
