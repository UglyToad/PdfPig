namespace UglyToad.PdfPig.Tokenization
{
    using System;
    using System.Globalization;
    using System.Text;
    using IO;
    using Tokens;

    internal class NumericTokenizer : ITokenizer
    {
        public bool ReadsNextByte { get; } = true;

        public bool TryTokenize(byte currentByte, IInputBytes inputBytes, out IToken token)
        {
            token = null;

            StringBuilder characters;

            if ((currentByte >= '0' && currentByte <= '9') || currentByte == '-' || currentByte == '+' || currentByte == '.')
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
                var c = (char) b;

                if (char.IsDigit(c) ||
                    c == '-' ||
                    c == '+' ||
                    c == '.' ||
                    c == 'E' ||
                    c == 'e')
                {
                    characters.Append(c);
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
                    value = decimal.Parse(characters.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture);
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
