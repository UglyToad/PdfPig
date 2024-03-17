namespace UglyToad.PdfPig.Util
{
    using System;
    using System.Text;

    /**
     * Utility functions for hex encoding.
     *
     * @author John Hewson
     */
    internal static class Hex
    {
        /**
         * for hex conversion.
         * 
         * https://stackoverflow.com/questions/2817752/java-code-to-convert-byte-to-hexadecimal
         *
         */
        private static readonly char[] HexChars = ['0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F'];
        
        /// <summary>
        /// Returns a hex string for the given byte array.
        /// </summary>
        public static string GetString(ReadOnlySpan<byte> bytes)
        {
#if NET6_0_OR_GREATER
            return Convert.ToHexString(bytes); 
#else
            var stringBuilder = new StringBuilder(bytes.Length * 2);

            foreach (var b in bytes)
            {
                stringBuilder.Append(HexChars[GetHighNibble(b)]).Append(HexChars[GetLowNibble(b)]);
            }

            return stringBuilder.ToString();
#endif
        }

        private static int GetHighNibble(byte b)
        {
            return (b & 0xF0) >> 4;
        }

        private static int GetLowNibble(byte b)
        {
            return b & 0x0F;
        }
    }
}
