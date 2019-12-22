namespace UglyToad.PdfPig.Tokenization
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text;
    using IO;
    using Tokens;

    internal class NumericTokenizer : ITokenizer
    {
        private const byte Zero = 48;
        private const byte Nine = 57;

        private readonly Dictionary<string, NumericToken> cachedTokens = new Dictionary<string, NumericToken>();

        public bool ReadsNextByte { get; } = true;
        
        public bool TryTokenize(byte currentByte, IInputBytes inputBytes, out IToken token)
        {
            token = null;

            StringBuilder characters;

            if ((currentByte >= Zero && currentByte <= Nine) || currentByte == '-' || currentByte == '+' || currentByte == '.')
            {
                characters = new StringBuilder();
                characters.Append((char)currentByte);
            }
            else
            {
                return false;
            }

            while (inputBytes.MoveNext())
            {
                var b = inputBytes.CurrentByte;

                if ((b >= Zero && b <= Nine) ||
                    b == '-' ||
                    b == '+' ||
                    b == '.' ||
                    b == 'E' ||
                    b == 'e')
                {
                    characters.Append((char)b);
                }
                else
                {
                    break;
                }
            }

            decimal value;

            try
            {
                if (characters.Length == 1 && (characters[0] == '-' || characters[0] == '.'))
                {
                    value = 0;
                }
                else
                {
                    var str = characters.ToString();

                    switch (str)
                    {
                        case "0":
                            token = NumericToken.Zero;
                            return true;
                        case "1":
                            token = NumericToken.One;
                            return true;
                        case "2":
                            token = NumericToken.Two;
                            return true;
                        case "3":
                            token = NumericToken.Three;
                            return true;
                        case "8":
                            token = NumericToken.Eight;
                            return true;
                        default:
                            {
                                if (!cachedTokens.TryGetValue(str, out var result))
                                {
                                    value = decimal.Parse(str, NumberStyles.Any, CultureInfo.InvariantCulture);
                                    result = new NumericToken(value);
                                    cachedTokens[str] = result;
                                }

                                token = result;
                                return true;
                            }
                    }
                }
            }
            catch (FormatException)
            {
                return false;
            }
            catch (OverflowException)
            {
                return false;
            }

            token = new NumericToken(value);

            return true;
        }
    }
}
