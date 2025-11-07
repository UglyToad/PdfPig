namespace UglyToad.PdfPig.Tokenization
{
    using Core;
    using System.Text;
    using Tokens;

    internal sealed class CommentTokenizer : ITokenizer
    {
        public bool ReadsNextByte { get; } = true;

        public bool TryTokenize(byte currentByte, IInputBytes inputBytes, out IToken token)
        {
            token = null;

            if (currentByte != '%')
            {
                return false;
            }

            using var builder = new ValueStringBuilder();

            while (inputBytes.MoveNext() && !ReadHelper.IsEndOfLine(inputBytes.CurrentByte))
            {
                builder.Append((char) inputBytes.CurrentByte);
            }

            token = new CommentToken(builder.ToString());

            return true;
        }
    }
}
