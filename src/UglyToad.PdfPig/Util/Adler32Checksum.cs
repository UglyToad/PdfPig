namespace UglyToad.PdfPig.Util
{
    /// <summary>
    /// Used to calculate the Adler-32 checksum used for ZLIB data in accordance with 
    /// RFC 1950: ZLIB Compressed Data Format Specification.
    /// </summary>
    public static class Adler32Checksum
    {
        // Both sums (s1 and s2) are done modulo 65521.
        private const int AdlerModulus = 65521;

        /// <summary>
        /// Calculate the Adler-32 checksum for some data.
        /// </summary>
        public static int Calculate(ReadOnlySpan<byte> data)
        {
            // s1 is the sum of all bytes.
            var s1 = 1;

            // s2 is the sum of all s1 values.
            var s2 = 0;

            foreach (var b in data)
            {
                s1 = (s1 + b) % AdlerModulus;
                s2 = (s1 + s2) % AdlerModulus;
            }

            // The Adler-32 checksum is stored as s2*65536 + s1.
            return s2 * 65536 + s1;
        }
    }
}
