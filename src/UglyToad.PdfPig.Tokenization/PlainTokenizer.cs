namespace UglyToad.PdfPig.Tokenization
{
    using Core;
    using System.Text;
    using Tokens;

    internal sealed class PlainTokenizer : ITokenizer
    {
        public bool ReadsNextByte { get; } = true;

        public bool TryTokenize(byte currentByte, IInputBytes inputBytes, out IToken token)
        {
            if (ReadHelper.IsWhitespace(currentByte))
            {
                token = null;

                return false;
            }

            using var builder = new ValueStringBuilder(stackalloc char[16]);

            builder.Append((char)currentByte);
            
            while (inputBytes.MoveNext())
            {
                if (ReadHelper.IsWhitespace(inputBytes.CurrentByte))
                {
                    break;
                }

                if (inputBytes.CurrentByte is (byte)'<' or (byte)'[' or (byte)'/' or (byte)']' or (byte)'>' or (byte)'(' or (byte)')')
                {
                    break;
                }

                builder.Append((char) inputBytes.CurrentByte);
            }

            var text = builder.AsSpan();

            token = text switch {
                "true"  => BooleanToken.True,
                "false" => BooleanToken.False,
                "null"  => NullToken.Instance,
                _       => OperatorToken.Create(text),
            };

            return true;
        }
    }
}
