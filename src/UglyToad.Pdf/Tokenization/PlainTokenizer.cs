namespace UglyToad.Pdf.Tokenization
{
    using System;
    using System.Text;
    using IO;
    using Parser.Parts;
    using Tokens;

    public class PlainTokenizer : ITokenizer
    {
        public bool TryTokenize(byte currentByte, IInputBytes inputBytes, out IToken token)
        {
            token = null;

            if (ReadHelper.IsWhitespace(currentByte))
            {
                return false;
            }

            var builder = new StringBuilder();
            builder.Append(currentByte);
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

                builder.Append((char) currentByte);
            }

            var text = builder.ToString();

            switch (text)
            {
                case "true":
                    break;
                case "false":
                    break;
                case "null":
                    break;
                case "endstream":
                    break;
                case "stream":
                    break;
                case "obj":
                    break;
                case "endobj":
                    break;
                default:
                    break;
            }

            return true;
        }
    }
}
