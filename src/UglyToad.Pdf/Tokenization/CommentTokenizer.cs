namespace UglyToad.Pdf.Tokenization
{
    using System.Text;
    using IO;
    using Parser.Parts;
    using Tokens;

    internal class CommentTokenizer : ITokenizer
    {
        public bool ReadsNextByte { get; } = true;

        public bool TryTokenize(byte currentByte, IInputBytes inputBytes, out IToken token)
        {
            token = null;

            if (currentByte != '%')
            {
                return false;
            }

            var builder = new StringBuilder();

            while (inputBytes.MoveNext() && !ReadHelper.IsEndOfLine(inputBytes.CurrentByte))
            {
                builder.Append((char) inputBytes.CurrentByte);
            }

            token = new CommentToken(builder.ToString());

            return true;
        }
    }
}
