namespace UglyToad.PdfPig.Util
{
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

        public static void GetUtf8Chars(ReadOnlySpan<byte> bytes, Span<byte> utf8Chars)
        {
            int position = 0;

            foreach (var b in bytes)
            {
                utf8Chars[position++] = (byte)HexChars[GetHighNibble(b)];
                utf8Chars[position++] = (byte)HexChars[GetLowNibble(b)];
            }
        }

        /// <summary>
        /// Returns a hex string for the given byte array.
        /// </summary>
        public static string GetString(ReadOnlySpan<byte> bytes)
        {
#if NET6_0_OR_GREATER
            return Convert.ToHexString(bytes); 
#else
            var chars = new char[bytes.Length * 2];
            int position = 0;

            foreach (var b in bytes)
            {
                chars[position++] = HexChars[GetHighNibble(b)];
                chars[position++] = HexChars[GetLowNibble(b)];
            }

            return new string(chars);
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
