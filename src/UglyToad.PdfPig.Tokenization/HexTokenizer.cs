namespace UglyToad.PdfPig.Tokenization
{
    using Core;
    using Tokens;

    internal sealed class HexTokenizer : ITokenizer
    {
        public bool ReadsNextByte { get; } = false;

        public bool TryTokenize(byte currentByte, IInputBytes inputBytes, out IToken token)
        {
            token = null;

            if (currentByte != '<')
            {
                return false;
            }

            using var charBuffer = new ArrayPoolBufferWriter<char>();

            while (inputBytes.MoveNext())
            {
                var current = inputBytes.CurrentByte;

                if (ReadHelper.IsWhitespace(current))
                {
                    continue;
                }

                if (current == '>')
                {
                    break;
                }

                if (!IsValidHexCharacter(current))
                {
                    return false;
                }

                charBuffer.Write((char)current);
            }

            token = new HexToken(charBuffer.WrittenSpan);

            return true;
        }

        private static bool IsValidHexCharacter(byte b)
        {
            return (b >= '0' && b <= '9')
                   || (b >= 'a' && b <= 'f')
                   || (b >= 'A' && b <= 'F');
        }
    }
}