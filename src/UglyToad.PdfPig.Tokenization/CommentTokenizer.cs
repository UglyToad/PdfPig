namespace UglyToad.PdfPig.Tokenization
{
    using Core;
    using System.Text;
    using Tokens;

    internal sealed class CommentTokenizer : ITokenizer
    {
        public bool ReadsNextByte => false;

        public bool TryTokenize(byte currentByte, IInputBytes inputBytes, out IToken token)
        {
            token = null;

            if (currentByte != '%')
            {
                return false;
            }

            using var builder = new ValueStringBuilder(stackalloc char[32]);

            while (inputBytes.Peek() is { } c && !ReadHelper.IsEndOfLine(c))
            {
                inputBytes.MoveNext();
                builder.Append((char) inputBytes.CurrentByte);
            }

            token = new CommentToken(builder.ToString());

            return true;
        }
    }
}
