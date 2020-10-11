namespace UglyToad.PdfPig.Function
{
    using UglyToad.PdfPig.Tokens;

    /// <summary>
    /// This class will be used to signify a range.  a(min) &lt;= a* &lt;= a(max)
    /// </summary>
    public class PdfRange
    {
        private readonly ArrayToken rangeArray;
        private readonly int startingIndex;

        /// <summary>
        /// Constructor with an initial range of 0..1.
        /// </summary>
        public PdfRange()
        {
            rangeArray = new ArrayToken(new NumericToken[]
            {
                new NumericToken(0),
                new NumericToken(1)
            });
            startingIndex = 0;
        }

        /// <summary>
        /// Constructor assumes a starting index of 0.
        /// </summary>
        /// <param name="range">The array that describes the range.</param>
        public PdfRange(ArrayToken range)
        {
            rangeArray = range;
        }

        /// <summary>
        /// Constructor with an index into an array. Because some arrays specify
        /// multiple ranges ie [0, 1, 0, 2, 2, 3] It is convenient for this
        /// class to take an index into an array. So if you want this range to
        /// represent 0,2 in the above example then you would say <code>new PDRange(array, 1)</code>
        /// </summary>
        /// <param name="range">The array that describes the index.</param>
        /// <param name="index">The range index into the array for the start of the range..</param>
        public PdfRange(ArrayToken range, int index)
        {
            rangeArray = range;
            startingIndex = index;
        }

        /// <summary>
        /// This will get the minimum value of the range.
        /// </summary>
        public decimal Min
        {
            get
            {
                var min = (NumericToken)rangeArray.Data[startingIndex * 2];
                return min.Data;
            }
        }

        /// <summary>
        /// This will get the maximum value of the range.
        /// </summary>
        public decimal Max
        {
            get
            {
                var min = (NumericToken)rangeArray.Data[startingIndex * 2 + 1];
                return min.Data;
            }
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return "PdfRange{" + Min + ", " + Max + '}';
        }
    }
}
