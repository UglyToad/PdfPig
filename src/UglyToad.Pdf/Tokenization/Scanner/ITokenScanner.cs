namespace UglyToad.Pdf.Tokenization.Scanner
{
    using Tokens;

    internal interface ITokenScanner
    {
        bool MoveNext();

        IToken CurrentToken { get; }

        bool TryReadToken<T>(out T token) where T : class, IToken;
    }

    internal interface ISeekableTokenScanner : ITokenScanner
    {
        void Seek(long position);

        long CurrentPosition { get; }

        void RegisterCustomTokenizer(byte firstByte, ITokenizer tokenizer);

        void DeregisterCustomTokenizer(ITokenizer tokenizer);
    }
}