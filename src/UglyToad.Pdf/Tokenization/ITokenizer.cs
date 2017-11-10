namespace UglyToad.Pdf.Tokenization
{
    using IO;
    using Tokens;

    internal interface ITokenizer
    {
        bool TryTokenize(byte currentByte, IInputBytes inputBytes, out IToken token);
    }
}