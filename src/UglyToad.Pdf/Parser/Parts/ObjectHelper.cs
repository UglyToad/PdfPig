using System;

namespace UglyToad.Pdf.Parser.Parts
{
    using IO;

    internal static class ObjectHelper
    {
        private const long ObjectNumberThreshold = 10000000000L;
        private const long GenerationNumberThreshold = 65535;

        public static long ReadObjectNumber(IRandomAccessRead reader)
        {
            long retval = ReadHelper.ReadLong(reader);
            if (retval < 0 || retval >= ObjectNumberThreshold)
            {
                throw new FormatException($"Object Number \'{retval}\' has more than 10 digits or is negative");
            }

            return retval;
        }

        public static int ReadGenerationNumber(IRandomAccessRead reader)
        {
            int retval = ReadHelper.ReadInt(reader);
            if (retval < 0 || retval > GenerationNumberThreshold)
            {
                throw new FormatException("Generation Number '" + retval + "' has more than 5 digits");
            }
            return retval;
        }

        public static string createObjectString(long objectID, long genID)
        {
            return $"{objectID} {genID} obj";
        }
    }
}
