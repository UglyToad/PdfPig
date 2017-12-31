namespace UglyToad.Pdf.Util
{
    using System.Text;
    using System.IO;
    /**
     * Utility functions for hex encoding.
     *
     * @author John Hewson
     */
    public class Hex
    {
        /**
         * for hex conversion.
         * 
         * https://stackoverflow.com/questions/2817752/java-code-to-convert-byte-to-hexadecimal
         *
         */
        private static readonly byte[] HexBytes = { (byte) '0', (byte)'1', (byte)'2', (byte)'3', (byte)'4', (byte)'5', (byte)'6', (byte)'7',
            (byte) '8', (byte) '9', (byte) 'A', (byte) 'B', (byte) 'C', (byte) 'D', (byte) 'E', (byte) 'F' };
        private static readonly char[] HexChars = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };

        private Hex() { }

        /**
         * Returns a hex string of the given byte array.
         */
        public static string GetString(byte[] bytes)
        {
            var stringBuilder = new StringBuilder(bytes.Length * 2);

            foreach (var b in bytes)
            {
                stringBuilder.Append(HexChars[GetHighNibble(b)]).Append(HexChars[GetLowNibble(b)]);
            }

            return stringBuilder.ToString();
        }

        /**
         * Writes the given byte as hex value to the given output stream.
         * @param b the byte to be written
         * @param output the output stream to be written to
         * @throws IOException exception if anything went wrong
         */
        public static void WriteHexByte(byte b, BinaryWriter output)
        {
            output.Write(HexBytes[GetHighNibble(b)]);
            output.Write(HexBytes[GetLowNibble(b)]);
        }

        /** 
         * Writes the given byte array as hex value to the given output stream.
         * @param bytes the byte array to be written
         * @param output the output stream to be written to
         * @throws IOException exception if anything went wrong
         */
        public static void WriteHexBytes(byte[] bytes, BinaryWriter output)
        {
            foreach (var b in bytes)
            {
                WriteHexByte(b, output);
            }
        }

        /**
         * GetLongOrDefault the high nibble of the given byte.
         * 
         * @param b the given byte
         * @return the high nibble
         */
        private static int GetHighNibble(byte b)
        {
            return (b & 0xF0) >> 4;
        }

        /**
         * GetLongOrDefault the low nibble of the given byte.
         * 
         * @param b the given byte
         * @return the low nibble
         */
        private static int GetLowNibble(byte b)
        {
            return b & 0x0F;
        }
    }

}
