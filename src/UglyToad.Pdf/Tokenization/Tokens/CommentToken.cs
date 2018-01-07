namespace UglyToad.Pdf.Tokenization.Tokens
{
    internal class CommentToken : IDataToken<string>
    {
        public string Data { get; }

        public CommentToken(string data)
        {
            Data = data;
        }

        public override string ToString()
        {
            return Data ?? "NULL";
        }
    }
}
