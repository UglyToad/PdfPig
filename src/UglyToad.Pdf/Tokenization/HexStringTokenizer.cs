namespace UglyToad.Pdf.Tokenization
{
    using IO;
    using Parser.Parts;
    using Tokens;

    public class HexStringTokenizer : ITokenizer
    {
        public bool TryTokenize(byte currentByte, IInputBytes inputBytes, out IToken token)
        {
            token = null;

            if (currentByte != '<')
            {
                return false;
            }

            while (inputBytes.MoveNext())
            {
                var current = inputBytes.CurrentByte;

                if (ReadHelper.IsWhitespace(current))
                {
                    continue;
                }

                if (!IsValidHexCharacter(current))
                {
                    return false;
                }

                if (current == '>')
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsValidHexCharacter(byte b)
        {
            return (b >= '0' && b <= '9')
                   || (b >= 'a' && b <= 'f')
                   || (b >= 'A' && b <= 'F');
        }
    }
}