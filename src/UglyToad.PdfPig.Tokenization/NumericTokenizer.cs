#nullable enable
namespace UglyToad.PdfPig.Tokenization;

using System;
using Core;
using Tokens;

internal sealed class NumericTokenizer : ITokenizer
{
    private const byte Zero = 48;
    private const byte Nine = 57;
    private const byte Negative = (byte)'-';
    private const byte Positive = (byte)'+';
    private const byte Period = (byte)'.';
    private const byte ExponentLower = (byte)'e';
    private const byte ExponentUpper = (byte)'E';

    // Over 866,944 pdfs (5.6 billion numbers in content streams) 28.6% of all numbers were whole
    // and in -128..1023, half of them 0. Widening to -1024..4095 would add 1.7 points at five times
    // the size; a cache keyed by value hit 65% more but its lookup cost more than the allocation.
    private const int SmallestShared = -128;
    private const int LargestShared = 1023;
    private static readonly NumericToken[] SharedIntegers = CreateSharedIntegers();

    private static NumericToken[] CreateSharedIntegers()
    {
        var result = new NumericToken[LargestShared - SmallestShared + 1];
        for (var i = 0; i < result.Length; i++)
        {
            var value = SmallestShared + i;
            result[i] = value == 0 ? NumericToken.Zero : new NumericToken(value);
        }

        return result;
    }

    public bool ReadsNextByte => true;

    public bool TryTokenize(byte currentByte, IInputBytes inputBytes, out IToken? token)
    {
        if (inputBytes is MemoryInputBytes memoryBytes)
        {
            return TryTokenizeInMemory(memoryBytes, out token);
        }

        token = null;

        var number = new NumberReader();
        var readBytes = 0;

        do
        {
            var step = number.Read(inputBytes.CurrentByte, readBytes);
            if (step == Step.Invalid)
            {
                return false;
            }

            if (step == Step.End)
            {
                break;
            }

            readBytes++;
        } while (inputBytes.MoveNext());

        token = number.ToToken();

        return true;
    }

    /// <summary>
    /// The same reading as the byte-by-byte loop, over the input's span, which saves the interface
    /// call per byte. Content streams are always read from memory so their numbers take this path.
    /// The input is left where the loop would leave it: on the byte that ended the number, on the
    /// byte that made it invalid, or on the last byte when the number ran to the end of the input.
    /// </summary>
    private static bool TryTokenizeInMemory(MemoryInputBytes inputBytes, out IToken? token)
    {
        var span = inputBytes.Span;
        var start = inputBytes.Position;

        var number = new NumberReader();
        var index = start;

        while (true)
        {
            var step = number.Read(span[index], index - start);
            if (step == Step.Invalid)
            {
                inputBytes.MoveTo(index);
                token = null;
                return false;
            }

            if (step == Step.End)
            {
                break;
            }

            index++;

            if (index == span.Length)
            {
                index = span.Length - 1;
                break;
            }
        }

        inputBytes.MoveTo(index);
        token = number.ToToken();

        return true;
    }

    private enum Step : byte
    {
        Continue,
        End,
        Invalid
    }

    private ref struct NumberReader
    {
        // Everything before the decimal part.
        private bool isNegative;
        private double integerPart;

        // Everything after the decimal point.
        private bool hasFraction;
        private long fractionalPart;
        private int fractionalCount;

        // Support scientific notation in some font files.
        private bool hasExponent;
        private bool isExponentNegative;
        private int exponentPart;

        public Step Read(byte b, int readBytes)
        {
            if (b >= Zero && b <= Nine)
            {
                if (hasExponent)
                {
                    exponentPart = (exponentPart * 10) + (b - Zero);
                }
                else if (hasFraction)
                {
                    fractionalPart = (fractionalPart * 10) + (b - Zero);
                    fractionalCount++;
                }
                else
                {
                    integerPart = (integerPart * 10) + (b - Zero);
                }
            }
            else if (b == Positive)
            {
                // Has no impact
            }
            else if (b == Negative)
            {
                if (hasExponent)
                {
                    isExponentNegative = true;
                }
                else
                {
                    isNegative = true;
                }
            }
            else if (b == Period)
            {
                if (hasExponent || hasFraction)
                {
                    return Step.Invalid;
                }

                hasFraction = true;
            }
            else if (b == ExponentLower || b == ExponentUpper)
            {
                // Don't allow leading exponent.
                if (readBytes == 0)
                {
                    return Step.Invalid;
                }

                if (hasExponent)
                {
                    return Step.Invalid;
                }

                hasExponent = true;
            }
            else
            {
                // No valid first character.
                return readBytes == 0 ? Step.Invalid : Step.End;
            }

            return Step.Continue;
        }

        public IToken ToToken()
        {
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
                return NumericToken.Zero;
            }

            if (integerPart >= SmallestShared && integerPart <= LargestShared)
            {
                var whole = (int)integerPart;
                if (whole == integerPart)
                {
                    return SharedIntegers[whole - SmallestShared];
                }
            }

            return new NumericToken(integerPart);
        }
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
