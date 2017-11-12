namespace UglyToad.Pdf.Tokenization
{
    using System.Text;
    using IO;
    using Parser.Parts;
    using Tokens;

    public class PlainTokenizer : ITokenizer
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
                    || inputBytes.CurrentByte == '/')
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
                case "endstream":
                    token = ObjectDelimiterToken.EndStream;
                    break;
                case "stream":
                    token = ObjectDelimiterToken.StartStream;
                    break;
                case "obj":
                    token = ObjectDelimiterToken.StartObject;
                    break;
                case "endobj":
                    token = ObjectDelimiterToken.EndObject;
                    break;
                default:
                    token = new OperatorToken(text);
                    break;
            }

            return true;
        }
    }
}
