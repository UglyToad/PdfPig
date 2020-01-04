namespace UglyToad.PdfPig.Tokenization
{
    using System.Text;
    using Core;
    using Parser.Parts;
    using Tokens;

    internal class PlainTokenizer : ITokenizer
    {
        public bool ReadsNextByte { get; } = true;

        public bool TryTokenize(byte currentByte, IInputBytes inputBytes, out IToken token)
        {
            token = null;

            if (ReadHelper.IsWhitespace(currentByte))
            {
                return false;
            }

            var builder = new StringBuilder();
            builder.Append((char)currentByte);
            while (inputBytes.MoveNext())
            {
                if (ReadHelper.IsWhitespace(inputBytes.CurrentByte))
                {
                    break;
                }

                if (inputBytes.CurrentByte == '<' || inputBytes.CurrentByte == '['
                    || inputBytes.CurrentByte == '/' || inputBytes.CurrentByte == ']'
                    || inputBytes.CurrentByte == '>' || inputBytes.CurrentByte == '('
                    || inputBytes.CurrentByte == ')')
                {
                    break;
                }

                builder.Append((char) inputBytes.CurrentByte);
            }

            var text = builder.ToString();

            switch (text)
            {
                case "true":
                    token = BooleanToken.True;
                    break;
                case "false":
                    token = BooleanToken.False;
                    break;
                case "null":
                    token = NullToken.Instance;
                    break;
                default:
                    token = OperatorToken.Create(text);
                    break;
            }

            return true;
        }
    }
}
