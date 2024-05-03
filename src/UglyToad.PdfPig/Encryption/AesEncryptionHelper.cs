﻿namespace UglyToad.PdfPig.Encryption
{
    using System.IO;
    using System.Security.Cryptography;

    internal static class AesEncryptionHelper
    {
        public static byte[] Encrypt256()
        {
            throw new NotImplementedException();
        }

        public static byte[] Decrypt(byte[] data, byte[] finalKey)
        {
            if (data.Length == 0)
            {
                return data;
            }

            var iv = new byte[16];
            Array.Copy(data, iv, iv.Length);

            using (var aes = Aes.Create())
            {
                aes.Key = finalKey;
                aes.IV = iv;

#if NET8_0_OR_GREATER
                return aes.DecryptCbc(data.AsSpan(iv.Length), iv, PaddingMode.PKCS7);
#else
                var buffer = new byte[256];

                using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                using (var input = new MemoryStream(data))
                using (var output = new MemoryStream())
                {
                    input.Seek(iv.Length, SeekOrigin.Begin);
                    using (var cryptoStream = new CryptoStream(input, decryptor, CryptoStreamMode.Read))
                    {
                        int read;
                        do
                        {
                            read = cryptoStream.Read(buffer, 0, buffer.Length);

                            if (read > 0)
                            {
                                output.Write(buffer, 0, read);
                            }
                        } while (read > 0);

                        return output.ToArray();
                    }
                }
#endif
            }
        }
    }
}
