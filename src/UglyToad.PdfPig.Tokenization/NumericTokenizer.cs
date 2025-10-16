#nullable enable
namespace UglyToad.PdfPig.Tokenization;

using System;
using Core;
using Tokens;

internal sealed class NumericTokenizer : ITokenizer
{
    public bool ReadsNextByte => false;

    public bool TryTokenize(byte currentByte, IInputBytes inputBytes, out IToken? token)
    {
        token = null;

        var readBytes = 0;

        // Everything before the decimal part.
        var isNegative = false;
        double integerPart = 0;

        // Everything after the decimal point.
        var hasFraction = false;
        long fractionalPart = 0;
        var fractionalCount = 0;

        // Support scientific notation in some font files.
        var hasExponent = false;
        var isExponentNegative = false;
        var exponentPart = 0;

        byte? firstByte = currentByte;
        bool noRead = true;
        bool acceptSign = true;
        while (!inputBytes.IsAtEnd() || firstByte is { })
        {
            if (firstByte is { } b)
            {
                firstByte = null;
            }
            else if (noRead)
            {
                noRead = false;
                b = inputBytes.Peek() ?? 0;
            }
            else
            {
                inputBytes.MoveNext();
                b = inputBytes.Peek() ?? 0;
            }

            if (b >= '0' && b <= '9')
            {
                var value = b - '0';
                if (hasExponent)
                {
                    exponentPart = (exponentPart * 10) + value;
                }
                else if (hasFraction)
                {
                    fractionalPart = (fractionalPart * 10) + value;
                    fractionalCount++;
                }
                else
                {
                    integerPart = (integerPart * 10) + value;
                }
                acceptSign = false;
            }
            else if (b == '+' && acceptSign)
            {
                // Has no impact
                acceptSign = false;
            }
            else if (b == '-' && acceptSign)
            {
                if (hasExponent)
                {
                    isExponentNegative = true;
                }
                else
                {
                    isNegative = true;
                }
                // acceptSign = false; // Somehow we have a test that expects to support "--21.72" to return -21.72
            }
            else if (b == '.' && !hasExponent && !hasFraction)
            {
                hasFraction = true;
                acceptSign = false;
            }
            else if ((b == 'e' || b == 'E') && readBytes > 0 && !hasExponent)
            {
                hasExponent = true;
                acceptSign = true;
            }
            else
            {
                // No valid first character.
                if (readBytes == 0)
                {
                    return false;
                }

                break;
            }

            readBytes++;
        }

        if (hasExponent && !isExponentNegative)
        {
            // Apply the multiplication before any fraction logic to avoid loss of precision.
            // E.g. 1.53E3 should be exactly 1,530.

            // Move the whole part to the left of the decimal point.
            var combined = integerPart * Pow10(fractionalCount) + fractionalPart;

            // For 1.53E3 we changed this to 153 above, 2 fractional parts, so now we are missing (3-2) 1 additional power of 10.
            var shift = exponentPart - fractionalCount;

            if (shift >= 0)
            {
                integerPart = combined * Pow10(shift);
            }
            else
            {
                // Still a positive exponent, but not enough to fully shift
                // For example 1.457E2 becomes 1,457 but shift is (2-3) -1, the outcome should be 145.7
                integerPart = combined / Pow10(-shift);
            }

            hasFraction = false;
            hasExponent = false;
        }

        if (hasFraction && fractionalCount > 0)
        {
            switch (fractionalCount)
            {
                case 1:
                    integerPart += fractionalPart / 10.0;
                    break;
                case 2:
                    integerPart += fractionalPart / 100.0;
                    break;
                case 3:
                    integerPart += fractionalPart / 1000.0;
                    break;
                default:
                    integerPart += fractionalPart / Math.Pow(10, fractionalCount);
                    break;
            }
        }

        if (hasExponent)
        {
            var signedExponent = isExponentNegative ? -exponentPart : exponentPart;
            integerPart *= Math.Pow(10, signedExponent);
        }

        if (isNegative)
        {
            integerPart = -integerPart;
        }

        if (integerPart == 0)
        {
            token = NumericToken.Zero;
        }
        else
        {
            token = new NumericToken(integerPart);
        }

        return true;
    }

    private static double Pow10(int exp)
    {
        return exp switch
        {
            0 => 1,
            1 => 10,
            2 => 100,
            3 => 1000,
            4 => 10000,
            5 => 100000,
            6 => 1000000,
            7 => 10000000,
            8 => 100000000,
            9 => 1000000000,
            _ => Math.Pow(10, exp)
        };
    }
}