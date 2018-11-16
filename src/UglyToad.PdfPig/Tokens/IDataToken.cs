namespace UglyToad.PdfPig.Tokens
{
    internal interface IDataToken<out T> : IToken
    {
        T Data { get; }
    }
}