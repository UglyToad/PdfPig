namespace UglyToad.Pdf.Tokenization.Tokens
{
    internal class BooleanToken : IDataToken<bool>
    {
        public static BooleanToken True { get; } = new BooleanToken(true);

        public static BooleanToken False { get; } = new BooleanToken(false);

        public bool Data { get; }

        private BooleanToken(bool data)
        {
            Data = data;
        }
    }
}