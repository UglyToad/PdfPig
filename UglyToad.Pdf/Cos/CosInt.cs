using System.Text;
using System.IO;
using UglyToad.Pdf.Core;

namespace UglyToad.Pdf.Cos
{
    public class CosInt : CosBase, ICosNumber, ICosStreamWriter
    {
        /**
         * The lowest integer to be kept in the {@link #STATIC} array.
         */
        private const int Low = -100;

        /**
     * The highest integer to be kept in the {@link #STATIC} array.
     */
        private const int High = 256;

        /**
     * Static instances of all CosInts in the range from {@link #LOW}
     * to {@link #HIGH}.
     */
        private static readonly CosInt[] Static = new CosInt[High - Low + 1];

        /**
         * Constant for the number zero.
         * @since Apache PDFBox 1.1.0
         */
        public static readonly CosInt Zero = Get(0);

        /**
         * Constant for the number one.
         * @since Apache PDFBox 1.1.0
         */
        public static readonly CosInt One = Get(1);

        /**
         * Constant for the number two.
         * @since Apache PDFBox 1.1.0
         */
        public static readonly CosInt Two = Get(2);

        /**
         * Constant for the number three.
         * @since Apache PDFBox 1.1.0
         */
        public static readonly CosInt Three = Get(3);

        /**
         * Returns a CosInt instance with the given value.
         *
         * @param val integer value
         * @return CosInt instance
         */
        public static CosInt Get(long val)
        {
            if (Low <= val && val <= High)
            {
                int index = (int)val - Low;
                // no synchronization needed
                if (Static[index] == null)
                {
                    Static[index] = new CosInt(val);
                }
                return Static[index];
            }
            return new CosInt(val);
        }

        private readonly long value;

        /**
         * constructor.
         *
         * @param val The integer value of this object.
         */
        private CosInt(long val)
        {
            value = val;
        }

        /**
         * {@inheritDoc}
         */

        public override bool Equals(object obj)
        {
            return Equals(obj as CosInt);
        }

        protected bool Equals(CosInt other)
        {
            return value == other?.value;
        }

        public override int GetHashCode()
        {
            return value.GetHashCode();
        }

        public override string ToString()
        {
            return "COSInt{" + value + "}";
        }

        public void WriteToPdfStream(StreamWriter output)
        {
            var encoding = Encoding.GetEncoding("ISO-8859-1");

            output.Write(encoding.GetBytes(value.ToString("D")));
        }

        /**
     * visitor pattern double dispatch method.
     *
     * @param visitor The object to notify when visiting this object.
     * @return any object, depending on the visitor implementation, or null
     * @throws IOException If an error occurs while visiting this object.
     */

        public override object Accept(ICosVisitor visitor)
        {
            return visitor.VisitFromInt(this);
        }

        public float AsFloat()
        {
            return value;
        }

        public double AsDouble()
        {
            return value;
        }

        public int AsInt()
        {
            return (int)value;
        }

        public long AsLong()
        {
            return value;
        }
    }
}
