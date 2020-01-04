namespace UglyToad.PdfPig.Parser.Parts
{
    using System;
    using Core;

    internal static class ObjectHelper
    {
        private const long ObjectNumberThreshold = 10000000000L;
        private const long GenerationNumberThreshold = 65535;

        public static long ReadObjectNumber(IInputBytes bytes)
        {
            long result = ReadHelper.ReadLong(bytes);
            if (result < 0 || result >= ObjectNumberThreshold)
            {
                throw new FormatException($"Object Number \'{result}\' has more than 10 digits or is negative");
            }

            return result;
        }

        public static int ReadGenerationNumber(IInputBytes bytes)
        {
            int result = ReadHelper.ReadInt(bytes);
            if (result < 0 || result > GenerationNumberThreshold)
            {
                throw new FormatException("Generation Number '" + result + "' has more than 5 digits");
            }

            return result;
        }

        public static string CreateObjectString(long objectId, long genId)
        {
            return $"{objectId} {genId} obj";
        }
    }
}
