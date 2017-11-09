namespace UglyToad.Pdf.Tokenization.Scanner
{
    using Tokens;

    public interface ITokenScanner
    {
        bool MoveNext();

        IToken CurrentToken { get; }
    }
}