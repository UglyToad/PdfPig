namespace UglyToad.Pdf.Util
{
    using System;

    internal static class OctalHelpers
    {
        public static short CharacterToShort(this char c)
        {
            switch (c)
            {
                case '0':
                    return 0;
                case '1':
                    return 1;
                case '2':
                    return 2;
                case '3':
                    return 3;
                case '4':
                    return 4;
                case '5':
                    return 5;
                case '6':
                    return 6;
                case '7':
                    return 7;
                case '8':
                    return 8;
                case '9':
                    return 9;
                default:
                    throw new InvalidOperationException($"Could not convert the character {c} to a short.");
            }
        }

        public static int FromOctalDigits(short[] octal)
        {


            int sum = 0;
            for (int i = octal.Length - 1; i >= 0; i--)
            {
                var power = i;
                sum += octal[i] * QuickPower(8, power);
            }

            return sum;
        }

        public static int FromOctalInt(int input)
        {
            var str = input.ToString();

            int sum = 0;
            for (var i = 0; i < str.Length; i++)
            {
                var part = str[str.Length - 1 - i].CharacterToShort();

                sum += part * QuickPower(8, i);
            }

            return sum;
        }

        private static int QuickPower(int x, int pow)
        {
            int ret = 1;
            while (pow != 0)
            {
                if ((pow & 1) == 1)
                    ret *= x;
                x *= x;
                pow >>= 1;
            }

            return ret;
        }
    }
}
