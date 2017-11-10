namespace UglyToad.Pdf.Tokenization.Tokens
{
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
    }
}