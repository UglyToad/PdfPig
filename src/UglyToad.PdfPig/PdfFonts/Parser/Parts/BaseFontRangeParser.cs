namespace UglyToad.PdfPig.PdfFonts.Parser.Parts
{
    using System;
    using Cmap;
    using Fonts;
    using Tokenization.Scanner;
    using Tokens;

    /// <summary>
    /// The beginbfrange and endbfrange operators map i ranges of input codes to the corresponding output code range.
    /// </summary>
    internal class BaseFontRangeParser : ICidFontPartParser<NumericToken>
    {
        public void Parse(NumericToken numberOfOperations, ITokenScanner scanner, CharacterMapBuilder builder)
        {
            for (var i = 0; i < numberOfOperations.Int; i++)
            {
                // The start of the input code range.
                if (!scanner.TryReadToken(out HexToken lowSourceCode))
                {
                    // Allow a miscount.
                    if (scanner.CurrentToken is OperatorToken ot &&
                        ot.Data.Equals("endbfrange", StringComparison.OrdinalIgnoreCase))
                    {
                        break;
                    }

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

                byte[]? destinationBytes = null;
                ArrayToken? destinationArray = null;

                switch (scanner.CurrentToken)
                {
                    case ArrayToken arrayToken:
                        destinationArray = arrayToken;
                        break;
                    case HexToken hexToken:
                        destinationBytes = [.. hexToken.Bytes];
                        break;
                    case NumericToken _:
                        throw new NotImplementedException("From the spec it seems this possible but the meaning is unclear...");
                    default:
                        throw new InvalidOperationException();
                }

                var done = false;
                var startCode = lowSourceCode.Bytes.ToArray();
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
                            builder.AddBaseFontCharacter(startCode, name.Data);
                        }
                        else if (destination is HexToken hex)
                        {
                            builder.AddBaseFontCharacter(startCode, hex.Bytes);
                        }
                        
                        Increment(startCode, startCode.Length - 1);

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

                    builder.AddBaseFontCharacter(startCode, destinationBytes!);

                    Increment(startCode, startCode.Length - 1);

                    Increment(destinationBytes!, destinationBytes!.Length - 1);
                }
            }
        }

        private static void Increment(Span<byte> data, int position)
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

        private static int Compare(ReadOnlySpan<byte> first, ReadOnlySpan<byte> second)
        {
            for (var i = 0; i < first.Length; i++)
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
