namespace UglyToad.Pdf.Tokenization
{
    using IO;
    using Tokens;

    internal interface ITokenizer
    {
        bool ReadsNextByte { get; }

        bool TryTokenize(byte currentByte, IInputBytes inputBytes, out IToken token);
    }
}