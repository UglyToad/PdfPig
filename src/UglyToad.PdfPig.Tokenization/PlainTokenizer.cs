namespace UglyToad.PdfPig.Tokenization
{
    using Core;
    using Tokens;

    internal class PlainTokenizer : ITokenizer
    {
        private static readonly StringBuilderPool StringBuilderPool = new StringBuilderPool(10);

        public bool ReadsNextByte { get; } = true;

        public bool TryTokenize(byte currentByte, IInputBytes inputBytes, out IToken token)
        {
            token = null;

            if (ReadHelper.IsWhitespace(currentByte))
            {
                return false;
            }

            var builder = StringBuilderPool.Borrow();
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
            StringBuilderPool.Return(builder);

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
