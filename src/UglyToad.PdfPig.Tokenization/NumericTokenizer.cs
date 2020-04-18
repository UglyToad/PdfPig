namespace UglyToad.PdfPig.Tokenization
{
    using System;
    using System.Globalization;
    using System.Text;
    using Core;
    using Tokens;

    internal class NumericTokenizer : ITokenizer
    {
        private static readonly StringBuilderPool StringBuilderPool = new StringBuilderPool(10);

        private const byte Zero = 48;
        private const byte Nine = 57;

        public bool ReadsNextByte { get; } = true;

        public bool TryTokenize(byte currentByte, IInputBytes inputBytes, out IToken token)
        {
            token = null;

            StringBuilder characters;

            if ((currentByte >= Zero && currentByte <= Nine) || currentByte == '-' || currentByte == '+' || currentByte == '.')
            {
                characters = StringBuilderPool.Borrow();
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

            try
            {
                var str = characters.ToString();
                StringBuilderPool.Return(characters);

                switch (str)
                {
                    case "-1":
                        token = NumericToken.MinusOne;
                        return true;
                    case "-":
                    case ".":
                    case "0":
                    case "0000":
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
                    case "4":
                        token = NumericToken.Four;
                        return true;
                    case "5":
                        token = NumericToken.Five;
                        return true;
                    case "6":
                        token = NumericToken.Six;
                        return true;
                    case "7":
                        token = NumericToken.Seven;
                        return true;
                    case "8":
                        token = NumericToken.Eight;
                        return true;
                    case "9":
                        token = NumericToken.Nine;
                        return true;
                    case "10":
                        token = NumericToken.Ten;
                        return true;
                    case "11":
                        token = NumericToken.Eleven;
                        return true;
                    case "12":
                        token = NumericToken.Twelve;
                        return true;
                    case "13":
                        token = NumericToken.Thirteen;
                        return true;
                    case "14":
                        token = NumericToken.Fourteen;
                        return true;
                    case "15":
                        token = NumericToken.Fifteen;
                        return true;
                    case "16":
                        token = NumericToken.Sixteen;
                        return true;
                    case "17":
                        token = NumericToken.Seventeen;
                        return true;
                    case "18":
                        token = NumericToken.Eighteen;
                        return true;
                    case "19":
                        token = NumericToken.Nineteen;
                        return true;
                    case "20":
                        token = NumericToken.Twenty;
                        return true;
                    case "100":
                        token = NumericToken.OneHundred;
                        return true;
                    case "500":
                        token = NumericToken.FiveHundred;
                        return true;
                    case "1000":
                        token = NumericToken.OneThousand;
                        return true;
                    default:
                        if (!decimal.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
                        {
                            return false;
                        }

                        token = new NumericToken(value);
                        return true;
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
        }
    }
}
