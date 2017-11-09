namespace UglyToad.Pdf.Tokenization.Tokens
{
    public interface IDataToken<out T> : IToken
    {
        T Data { get; }
    }
}