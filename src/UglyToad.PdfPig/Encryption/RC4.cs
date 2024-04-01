namespace UglyToad.PdfPig.Encryption
{
    using System;

    internal static class RC4
    {
        public static byte[] Encrypt(ReadOnlySpan<byte> key, ReadOnlySpan<byte> data)
        {
            // Key-scheduling algorithm
            var s = new byte[256];
            for (var i = 0; i < 256; i++)
            {
                s[i] = (byte)i;
            }

            var j = 0;
            for (var i = 0; i < 256; i++)
            {
                j = (j + s[i] + key[i % key.Length]) % 256;

                var temp = s[i];
                s[i] = s[j];
                s[j] = temp;
            }

            var result = new byte[data.Length];

            // Pseudo-random generation algorithm
            {
                j = 0;
                var i = 0;
                for (var step = 0; step < data.Length; step++)
                {
                    i = (i + 1) % 256;
                    j = (j + s[i]) % 256;

                    var temp = s[i];
                    s[i] = s[j];
                    s[j] = temp;

                    var k = s[(s[i] + s[j]) % 256];
                    result[step] = (byte)(data[step] ^ k);
                }
            }

            return result;
        }
    }
}
