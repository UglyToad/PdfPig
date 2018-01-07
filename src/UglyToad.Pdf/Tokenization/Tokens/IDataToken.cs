namespace UglyToad.Pdf.Tokenization.Tokens
{
    internal interface IDataToken<out T> : IToken
    {
        T Data { get; }
    }
}