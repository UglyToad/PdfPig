namespace UglyToad.PdfPig.Tokens
{
    using System;
    using System.Globalization;

    /// <inheritdoc />
    /// <summary>
    /// PDF supports integer and real numbers. Integer objects represent mathematical integers within a certain interval centered at 0.
    /// Real objects  approximate mathematical real numbers, but with limited range and precision.
    /// This token represents both types and they are used interchangeably in the specification.
    /// </summary>
    public class NumericToken : IDataToken<double>
    {
        /// <summary>
        /// Single instance of numeric token for -1.
        /// </summary>
        public static readonly NumericToken MinusOne = new NumericToken(-1);

        /// <summary>
        /// Single instance of numeric token for 0.
        /// </summary>
        public static readonly NumericToken Zero = new NumericToken(0);

        /// <summary>
        /// Single instance of numeric token for 1.
        /// </summary>
        public static readonly NumericToken One = new NumericToken(1);

        /// <summary>
        /// Single instance of numeric token for 2.
        /// </summary>
        public static readonly NumericToken Two = new NumericToken(2);

        /// <summary>
        /// Single instance of numeric token for 3.
        /// </summary>
        public static readonly NumericToken Three = new NumericToken(3);

        /// <summary>
        /// Single instance of numeric token for 4.
        /// </summary>
        public static readonly NumericToken Four = new NumericToken(4);
        /// <summary>
        /// Single instance of numeric token for 5.
        /// </summary>
        public static readonly NumericToken Five = new NumericToken(5);

        /// <summary>
        /// Single instance of numeric token for 6.
        /// </summary>
        public static readonly NumericToken Six = new NumericToken(6);

        /// <summary>
        /// Single instance of numeric token for 7.
        /// </summary>
        public static readonly NumericToken Seven = new NumericToken(7);

        /// <summary>
        /// Single instance of numeric token for 8.
        /// </summary>
        public static readonly NumericToken Eight = new NumericToken(8);

        /// <summary>
        /// Single instance of numeric token for 9.
        /// </summary>
        public static readonly NumericToken Nine = new NumericToken(9);

        /// <summary>
        /// Single instance of numeric token for 10.
        /// </summary>
        public static readonly NumericToken Ten = new NumericToken(10);

        /// <summary>
        /// Single instance of numeric token for 11.
        /// </summary>
        public static readonly NumericToken Eleven = new NumericToken(11);

        /// <summary>
        /// Single instance of numeric token for 12.
        /// </summary>
        public static readonly NumericToken Twelve = new NumericToken(12);

        /// <summary>
        /// Single instance of numeric token for 13.
        /// </summary>
        public static readonly NumericToken Thirteen = new NumericToken(13);

        /// <summary>
        /// Single instance of numeric token for 14.
        /// </summary>
        public static readonly NumericToken Fourteen = new NumericToken(14);

        /// <summary>
        /// Single instance of numeric token for 15.
        /// </summary>
        public static readonly NumericToken Fifteen = new NumericToken(15);

        /// <summary>
        /// Single instance of numeric token for 16.
        /// </summary>
        public static readonly NumericToken Sixteen = new NumericToken(16);

        /// <summary>
        /// Single instance of numeric token for 17.
        /// </summary>
        public static readonly NumericToken Seventeen = new NumericToken(17);

        /// <summary>
        /// Single instance of numeric token for 18.
        /// </summary>
        public static readonly NumericToken Eighteen = new NumericToken(18);

        /// <summary>
        /// Single instance of numeric token for 19.
        /// </summary>
        public static readonly NumericToken Nineteen = new NumericToken(19);

        /// <summary>
        /// Single instance of numeric token for 20.
        /// </summary>
        public static readonly NumericToken Twenty = new NumericToken(20);

        /// <summary>
        /// Single instance of numeric token for 100.
        /// </summary>
        public static readonly NumericToken OneHundred = new NumericToken(100);

        /// <summary>
        /// Single instance of numeric token for 500.
        /// </summary>
        public static readonly NumericToken FiveHundred = new NumericToken(500);

        /// <summary>
        /// Single instance of numeric token for 1000.
        /// </summary>
        public static readonly NumericToken OneThousand = new NumericToken(1000);

        /// <inheritdoc />
        public double Data { get; }

        /// <summary>
        /// Whether the number represented has a non-zero decimal part.
        /// </summary>
        public bool HasDecimalPlaces => !Math.Floor(Data).Equals(Data);

        /// <summary>
        /// The value of this number as an <see langword="int"/>.
        /// </summary>
        public int Int => (int)Data;

        /// <summary>
        /// The value of this number as a <see langword="long"/>.
        /// </summary>
        public long Long => (long)Data;

        /// <summary>
        /// The value of this number as a <see langword="double"/>.
        /// </summary>
        public double Double => Data;

        /// <summary>
        /// Create a <see cref="NumericToken"/>.
        /// </summary>
        /// <param name="value">The number to represent.</param>
        //[Obsolete("Use double constructor instead.")]
        public NumericToken(decimal value)
        {
            Data = (double)value;
        }

        /// <summary>
        /// Create a <see cref="NumericToken"/>.
        /// </summary>
        /// <param name="value">The number to represent.</param>
        public NumericToken(int value)
        {
            Data = value;
        }

        /// <summary>
        /// Create a <see cref="NumericToken"/>.
        /// </summary>
        /// <param name="value">The number to represent.</param>
        public NumericToken(double value)
        {
            Data = value;
        }

        /// <inheritdoc />
        public bool Equals(IToken obj)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (!(obj is NumericToken other))
            {
                return false;
            }

            return Data.Equals(other.Data);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return Data.GetHashCode();
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return Data.ToString(NumberFormatInfo.InvariantInfo);
        }
    }
}