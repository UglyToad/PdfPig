namespace UglyToad.PdfPig.Encryption
{
    using System;
    using System.Security.Cryptography;

    internal static class AesEncryptionHelper
    {
        public static byte[] Encrypt256()
        {
            // See https://stackoverflow.com/questions/73779169/cryptographicexception-bad-pkcs7-padding-invalid-length-0-cannot-decrypt-sa
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
                if (data.Length <= iv.Length)
                {
                    aes.Clear();
                    return [];
                }

                var encryptedData = data.AsSpan(iv.Length);
                var output = aes.DecryptCbc(encryptedData, iv, PaddingMode.PKCS7);
                aes.Clear();
                return output;
#else
                if (data.Length <= iv.Length)
                {
                    aes.Clear();
                    return [];
                }

                using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                {
                    var output = decryptor.TransformFinalBlock(data, iv.Length, data.Length - iv.Length);
                    aes.Clear();
                    return output;
                }
#endif
            }
        }
    }
}
