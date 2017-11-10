namespace UglyToad.Pdf.Tokenization
{
    using System.Text;
    using IO;
    using Parser.Parts;
    using Tokens;

    public class HexTokenizer : ITokenizer
    {
        public bool TryTokenize(byte currentByte, IInputBytes inputBytes, out IToken token)
        {
            token = null;

            if (currentByte != '<')
            {
                return false;
            }

            var characters = new StringBuilder();

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

                characters.Append((char)current);
            }

            token = new HexToken(characters.ToString());

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