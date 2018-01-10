namespace UglyToad.PdfPig.Tokenization.Tokens
{
    internal class StringToken : IDataToken<string>
    {
        public string Data { get; }

        public StringToken(string data)
        {
            Data = data;
        }

        public override string ToString()
        {
            return $"\"{Data}\"";
        }
    }
}