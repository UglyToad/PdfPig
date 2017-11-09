using System;
using System.Text;
using System.IO;

namespace UglyToad.Pdf.Util
{

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
         * Returns a hex string of the given byte.
         */
        public static string GetString(byte b)
        {
            char[] chars = { HexChars[GetHighNibble(b)], HexChars[GetLowNibble(b)] };
            return new String(chars);
        }

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
         * Returns the bytes corresponding to the ASCII hex encoding of the given byte.
         */
        public static byte[] GetBytes(byte b)
        {
            return new[] { HexBytes[GetHighNibble(b)], HexBytes[GetLowNibble(b)] };
        }

        /**
         * Returns the bytes corresponding to the ASCII hex encoding of the given bytes.
         */
        public static byte[] GetBytes(byte[] bytes)
        {
            byte[] asciiBytes = new byte[bytes.Length * 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                asciiBytes[i * 2] = HexBytes[GetHighNibble(bytes[i])];
                asciiBytes[i * 2 + 1] = HexBytes[GetLowNibble(bytes[i])];
            }
            return asciiBytes;
        }

        /** 
         * Returns the characters corresponding to the ASCII hex encoding of the given short.
         */
        public static char[] GetChars(short num)
        {
            char[] hex = new char[4];
            hex[0] = HexChars[(num >> 12) & 0x0F];
            hex[1] = HexChars[(num >> 8) & 0x0F];
            hex[2] = HexChars[(num >> 4) & 0x0F];
            hex[3] = HexChars[num & 0x0F];
            return hex;
        }

        /**
         * Takes the characters in the given string, convert it to bytes in UTF16-BE format
         * and build a char array that corresponds to the ASCII hex encoding of the resulting
         * bytes.
         *
         * Example:
         * <pre>
         *   getCharsUTF16BE("ab") == new char[]{'0','0','6','1','0','0','6','2'}
         * </pre>
         *
         * @param text The string to convert
         * @return The string converted to hex
         */
        public static char[] GetCharsUtf16Be(String text)
        {
            // Note that the internal representation of string in Java is already UTF-16. Therefore
            // we do not need to use an encoder to convert the string to its byte representation.
            char[] hex = new char[text.Length * 4];

            for (int stringIdx = 0, charIdx = 0; stringIdx < text.Length; stringIdx++)
            {
                char c = text[stringIdx];
                hex[charIdx++] = HexChars[(c >> 12) & 0x0F];
                hex[charIdx++] = HexChars[(c >> 8) & 0x0F];
                hex[charIdx++] = HexChars[(c >> 4) & 0x0F];
                hex[charIdx++] = HexChars[c & 0x0F];
            }

            return hex;
        }

        /**
         * Writes the given byte as hex value to the given output stream.
         * @param b the byte to be written
         * @param output the output stream to be written to
         * @throws IOException exception if anything went wrong
         */
        public static void WriteHexByte(byte b, StreamWriter output)
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
        public static void WriteHexBytes(byte[] bytes, StreamWriter output)
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
