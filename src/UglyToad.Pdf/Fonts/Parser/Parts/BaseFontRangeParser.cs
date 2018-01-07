namespace UglyToad.Pdf.Fonts.Parser.Parts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Cmap;
    using Exceptions;
    using Tokenization.Scanner;
    using Tokenization.Tokens;

    /// <summary>
    /// The beginbfrange and endbfrange operators map i ranges of input codes to the corresponding output code range.
    /// </summary>
    internal class BaseFontRangeParser : ICidFontPartParser<NumericToken>
    {
        public void Parse(NumericToken numberOfOperations, ITokenScanner scanner, CharacterMapBuilder builder, bool isLenientParsing)
        {
            for (var i = 0; i < numberOfOperations.Int; i++)
            {
                // The start of the input code range.
                if (!scanner.TryReadToken(out HexToken lowSourceCode))
                {
                    throw new InvalidFontFormatException($"bfrange was missing the low source code: {scanner.CurrentToken}");
                }

                // The inclusive end of the input code range.
                if (!scanner.TryReadToken(out HexToken highSourceCode))
                {
                    throw new InvalidFontFormatException($"bfrange was missing the high source code: {scanner.CurrentToken}");
                }

                if (!scanner.MoveNext())
                {
                    throw new InvalidFontFormatException("bfrange ended unexpectedly after the high source code.");
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

                if (destinationArray != null)
                {
                    int arrayIndex = 0;
                    while (!done)
                    {
                        if (Compare(startCode, endCode) >= 0)
                        {
                            done = true;
                        }

                        var destination = destinationArray.Data[arrayIndex];

                        if (destination is NameToken name)
                        {
                            builder.AddBaseFontCharacter(startCode, name.Data.Name);
                        }
                        else if (destination is HexToken hex)
                        {
                            builder.AddBaseFontCharacter(startCode, hex.Bytes);
                        }
                        
                        Increment(startCode, startCode.Count - 1);

                        arrayIndex++;
                    }

                    continue;
                }

                while (!done)
                {
                    if (Compare(startCode, endCode) >= 0)
                    {
                        done = true;
                    }

                    builder.AddBaseFontCharacter(startCode, destinationBytes);

                    Increment(startCode, startCode.Count - 1);

                    Increment(destinationBytes, destinationBytes.Count - 1);
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
