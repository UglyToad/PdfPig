namespace UglyToad.Pdf.Fonts.Parser.Parts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Cmap;
    using Tokenization.Scanner;
    using Tokenization.Tokens;

    internal class BaseFontRangeParser : ICidFontPartParser<NumericToken>
    {
        public void Parse(NumericToken numeric, ITokenScanner scanner, CharacterMapBuilder builder, bool isLenientParsing)
        {
            for (var i = 0; i < numeric.Int; i++)
            {
                if (!scanner.TryReadToken(out HexToken lowSourceCode))
                {
                    // TODO: message
                    throw new InvalidOperationException();
                }

                if (!scanner.TryReadToken(out HexToken highSourceCode))
                {
                    // TODO: message
                    throw new InvalidOperationException();
                }

                if (!scanner.MoveNext())
                {
                    // TODO: message
                    throw new InvalidOperationException();
                }

                List<byte> destinationBytes = null;
                ArrayToken destinationArray = null;
                switch (scanner.CurrentToken)
                {
                    case ArrayToken arrayToken:
                        destinationArray = arrayToken;
                        break;
                    case HexToken hexToken:
                        destinationBytes = hexToken.Bytes.ToList();
                        break;
                    case NumericToken _:
                        throw new NotImplementedException("From the spec it seems this possible but the meaning is unclear...");
                    default:
                        throw new InvalidOperationException();
                }

                var done = false;
                var startCode = new List<byte>(lowSourceCode.Bytes);
                var endCode = highSourceCode.Bytes;

                int arrayIndex = 0;
                while (!done)
                {
                    if (Compare(startCode, endCode) >= 0)
                    {
                        done = true;
                    }

                    builder.AddBaseFontCharacter(startCode, destinationBytes);

                    Increment(startCode, startCode.Count - 1);

                    if (destinationArray == null)
                    {
                        Increment(destinationBytes, destinationBytes.Count - 1);
                    }
                    else
                    {
                        arrayIndex++;
                        if (arrayIndex < destinationArray.Data.Count)
                        {
                            destinationBytes = ((HexToken)destinationArray.Data[arrayIndex]).Bytes.ToList();
                        }
                    }
                }
            }
        }

        private static void Increment(IList<byte> data, int position)
        {
            if (position > 0 && (data[position] & 0xFF) == 255)
            {
                data[position] = 0;
                Increment(data, position - 1);
            }
            else
            {
                data[position] = (byte)(data[position] + 1);
            }
        }

        private static int Compare(IReadOnlyList<byte> first, IReadOnlyList<byte> second)
        {
            for (var i = 0; i < first.Count; i++)
            {
                if (first[i] == second[i])
                {
                    continue;
                }

                if ((first[i] & 0xFF) < (second[i] & 0xFF))
                {
                    return -1;
                }

                return 1;
            }

            return 0;
        }

    }
}
