namespace UglyToad.PdfPig.Tokenization
{
    using System.Collections.Generic;
    using Core;
    using Tokens;

    internal class HexTokenizer : ITokenizer
    {
        public bool ReadsNextByte { get; } = false;

        public bool TryTokenize(byte currentByte, IInputBytes inputBytes, out IToken token)
        {
            token = null;

            if (currentByte != '<')
            {
                return false;
            }
            
            var characters = new List<char>();

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

                characters.Add((char)current);
            }

            token = new HexToken(characters);

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