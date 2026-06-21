namespace UglyToad.PdfPig.Tokenization
{
    using Core;
    using System.Text;
    using Tokens;

    internal sealed class PlainTokenizer : ITokenizer
    {
        /// <summary>
        /// <c>true</c> required to read malformed CMap streams which omit the whitespace mandated between tokens (see #1331).
        /// It must NOT be enabled for general content stream parsing, where operators such as the Type 3 glyph operators
        /// <c>d0</c> and <c>d1</c> legitimately contain digits.
        /// </summary>
        private readonly bool splitOnDigit;

        public bool ReadsNextByte => true;

        public PlainTokenizer(bool splitOnDigit = false)
        {
            this.splitOnDigit = splitOnDigit;
        }

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

                if (inputBytes.CurrentByte is (byte)'<' or (byte)'[' or (byte)'/' or (byte)']' or (byte)'>' or (byte)'(' or (byte)')' or (byte)'%')
                {
                    break;
                }

                if (splitOnDigit && inputBytes.CurrentByte is >= (byte)'0' and <= (byte)'9')
                {
                    // Malformed CMap, see #1331
                    break;
                }

                builder.Append((char)inputBytes.CurrentByte);
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
