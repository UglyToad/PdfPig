namespace UglyToad.Pdf.Tokenization.Scanner
{
    using Tokens;

    public interface ITokenScanner
    {
        bool MoveNext();

        IToken CurrentToken { get; }

        bool TryReadToken<T>(out T token) where T : class, IToken;
    }
}