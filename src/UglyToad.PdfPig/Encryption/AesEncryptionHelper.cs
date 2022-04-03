namespace UglyToad.PdfPig.Encryption
{
    using System;
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
            
            using (var rijndael = Rijndael.Create())
            {
                rijndael.Key = finalKey;
                rijndael.IV = iv;

                var buffer = new byte[256];

                using (var decryptor = rijndael.CreateDecryptor(rijndael.Key, rijndael.IV))
                using (var input = new MemoryStream(data))
                using (var output = new MemoryStream())
                {
                    input.Seek(iv.Length, SeekOrigin.Begin);
                    using (var cryptoStream = new CryptoStream(input, decryptor, CryptoStreamMode.Read))
                    {
                        var offset = 0;
                        int read;
                        do
                        {
                            read = cryptoStream.Read(buffer, offset, buffer.Length - offset);

                            output.Write(buffer, offset, read);

                            offset += read;
                        } while (read > 0);

                        return output.ToArray();
                    }
                }
            }
        }
    }
}
