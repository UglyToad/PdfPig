namespace UglyToad.Pdf.Tokenization.Tokens
{
    public class BooleanToken : IDataToken<bool>
    {
        public bool Data { get; }

        public BooleanToken(bool data)
        {
            Data = data;
        }
    }
}
