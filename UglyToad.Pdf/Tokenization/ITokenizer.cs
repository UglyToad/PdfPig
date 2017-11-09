namespace UglyToad.Pdf.IO
{
    using Tokenization.Tokens;

    internal interface ITokenizer
    {
        bool TryTokenize(byte currentByte, IInputBytes inputBytes, out IToken token);
    }
}