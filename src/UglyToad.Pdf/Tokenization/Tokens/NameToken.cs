namespace UglyToad.Pdf.Tokenization.Tokens
{
    using Cos;

    internal class NameToken : IDataToken<CosName>
    {
        public CosName Data { get; }

        public NameToken(string text)
        {
            Data = CosName.Create(text);
        }

        public override string ToString()
        {
            return Data.ToString();
        }
    }
}