namespace UglyToad.Pdf.Tokenization.Tokens
{
    using System.Globalization;

    public class NumericToken : IDataToken<decimal>
    {
        public decimal Data { get; }

        public bool IsWhole { get; }

        public int Int { get; }

        public long Long { get; }

        public NumericToken(decimal value)
        {
            Data = value;
            IsWhole = decimal.Floor(value) == value;
            Int = (int) value;
            Long = (long) value;
        }

        public override string ToString()
        {
            return Data.ToString(NumberFormatInfo.InvariantInfo);
        }
    }
}