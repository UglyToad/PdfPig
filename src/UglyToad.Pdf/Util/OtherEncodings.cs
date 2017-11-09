using System.Text;

namespace UglyToad.Pdf.Util
{
    public static class OtherEncodings
    {
        /// <summary>
        /// Latin 1 Encoding: ISO 8859-1 is a single-byte encoding that can represent the first 256 Unicode characters.
        /// </summary>
        public static Encoding Iso88591 = Encoding.GetEncoding("ISO-8859-1");

        public static byte[] StringAsLatin1Bytes(string s)
        {
            if (s == null)
            {
                return null;
            }

            return Iso88591.GetBytes(s);
        }

        public static string BytesAsLatin1String(byte[] bytes)
        {
            if (bytes == null)
            {
                return null;
            }

            return Iso88591.GetString(bytes);
        }
    }
}
