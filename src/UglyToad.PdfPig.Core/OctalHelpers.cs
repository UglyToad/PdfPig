namespace UglyToad.PdfPig.Core
{
    using System;

    /// <summary>
    /// Interprets numbers in octal format.
    /// </summary>
    public static class OctalHelpers
    {
        /// <summary>
        /// Read a short.
        /// </summary>
        public static short CharacterToShort(this char c)
        {
            return c switch {
                '0' => 0,
                '1' => 1,
                '2' => 2,
                '3' => 3,
                '4' => 4,
                '5' => 5,
                '6' => 6,
                '7' => 7,
                '8' => 8,
                '9' => 9,
                _ => throw new InvalidOperationException($"Could not convert the character {c} to a short.")
            };
        }

        /// <summary>
        /// Read an integer from octal digits.
        /// </summary>
        public static int FromOctalDigits(ReadOnlySpan<short> octal)
        {
            int sum = 0;
            for (int i = octal.Length - 1; i >= 0; i--)
            {
                var power = i;
                sum += octal[i] * QuickPower(8, power);
            }

            return sum;
        }

        /// <summary>
        /// Interpret an int as octal.
        /// </summary>
        public static int FromOctalInt(int input)
        {
            return Convert.ToInt32($"{input}", 8);
        }

        private static int QuickPower(int x, int pow)
        {
            int ret = 1;
            while (pow != 0)
            {
                if ((pow & 1) == 1)
                {
                    ret *= x;
                }

                x *= x;
                pow >>= 1;
            }

            return ret;
        }
    }
}
