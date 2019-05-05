namespace UglyToad.PdfPig.Encryption
{
    using System;
    using System.Collections.Generic;
    using System.Security.Cryptography;
    using System.Text;
    using CrossReference;
    using Tokens;
    using Util;
    using Util.JetBrains.Annotations;

    internal class EncryptionHandler : IEncryptionHandler
    {
        private static readonly byte[] PaddingBytes = 
        {
            0x28, 0xBF, 0x4E, 0x5E,
            0x4E, 0x75, 0x8A, 0x41,
            0x64, 0x00, 0x4E, 0x56,
            0xFF, 0xFA, 0x01, 0x08,
            0x2E, 0x2E, 0x00, 0xB6,
            0xD0, 0x68, 0x3E, 0x80,
            0x2F, 0x0C, 0xA9, 0xFE,
            0x64, 0x53, 0x69, 0x7A
        };

        [CanBeNull]
        private readonly EncryptionDictionary encryptionDictionary;

        [NotNull]
        private readonly byte[] documentIdBytes;

        [NotNull]
        private readonly string password;

        private readonly byte[] encryptionKey;

        private readonly bool useAes;

        public EncryptionHandler(EncryptionDictionary encryptionDictionary, TrailerDictionary trailerDictionary, string password)
        {
            this.encryptionDictionary = encryptionDictionary;

            documentIdBytes = trailerDictionary.Identifier != null && trailerDictionary.Identifier.Count == 2 ? 
                OtherEncodings.StringAsLatin1Bytes(trailerDictionary.Identifier[0])
                : EmptyArray<byte>.Instance;
            this.password = password ?? string.Empty;

            if (encryptionDictionary == null)
            {
                return;
            }

            var userKey = OtherEncodings.StringAsLatin1Bytes(encryptionDictionary.UserPasswordCheck);
            var ownerKey = OtherEncodings.StringAsLatin1Bytes(encryptionDictionary.OwnerPasswordCheck);

            var charset = OtherEncodings.Iso88591;

            if (encryptionDictionary.StandardSecurityHandlerRevision == 5 || encryptionDictionary.StandardSecurityHandlerRevision == 6)
            {
                charset = Encoding.UTF8;
                throw new NotSupportedException($"Revision of {encryptionDictionary.StandardSecurityHandlerRevision} not supported, please raise an issue.");
            }

            var passwordBytes = charset.GetBytes(this.password);

            var length = encryptionDictionary.EncryptionAlgorithmCode == EncryptionAlgorithmCode.Rc4OrAes40BitKey 
                ? 5 
                : encryptionDictionary.KeyLength.GetValueOrDefault() / 8;

            encryptionKey = CalculateKeyRevisions2To4(passwordBytes, ownerKey, (int) encryptionDictionary.UserAccessPermissions, encryptionDictionary.StandardSecurityHandlerRevision,
                length, documentIdBytes, encryptionDictionary.EncryptMetadata);
        }
        
        public IReadOnlyList<byte> Decrypt(StreamToken stream)
        {
            if (encryptionDictionary == null)
            {
                return stream?.Data;
            }

            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            throw new NotImplementedException($"Encryption is not supported yet. Encryption used in document was: {encryptionDictionary.Dictionary}.");
        }

        public void Decrypt(IndirectReference reference, IToken token)
        {
            if (token is StreamToken stream)
            {
                
            }
            else if (token is StringToken stringToken)
            {
                
            }
            else if (token is DictionaryToken dictionary)
            {
                
            }
            else if (token is ArrayToken array)
            {

            }
        }

        private byte[] DecryptData(byte[] data, IndirectReference reference)
        {
            if (useAes && encryptionKey.Length == 32)
            {
                throw new NotImplementedException("Decryption for AES-256 not currently supported.");
            }

            var finalKey = GetObjectKey(reference);

            if (useAes)
            {
                throw new NotImplementedException("Decryption for AES-128 not currently supported.");
            }
            
            return RC4.Encrypt(finalKey, data);
        }

        private byte[] GetObjectKey(IndirectReference reference)
        { 
            // 1. Get the object and generation number from the object

            // 2. Treating the object and generation number as binary integers extend the
            // original n byte encryption key to n + 5 bytes by taking the low-order 3 bytes
            // of the object number and the low-order 2 bytes of the generation number, low order
            // byte first.
            var finalKey = new byte[encryptionKey.Length + 5 + (useAes ? 4 : 0)];
            Array.Copy(encryptionKey, finalKey, encryptionKey.Length);

            finalKey[encryptionKey.Length] = (byte) reference.ObjectNumber;
            finalKey[encryptionKey.Length + 1] = (byte) (reference.ObjectNumber >> 1);
            finalKey[encryptionKey.Length + 2] = (byte) (reference.ObjectNumber >> 2);

            finalKey[encryptionKey.Length + 3] = (byte) reference.Generation;
            finalKey[encryptionKey.Length + 4] = (byte) (reference.Generation >> 1);
            
            // 2. If using the AES algorithm extend the encryption key by 4 bytes by adding the value "sAlT".
            if (useAes)
            {
                finalKey[encryptionKey.Length + 5] = (byte)'s';
                finalKey[encryptionKey.Length + 6] = (byte)'A';
                finalKey[encryptionKey.Length + 7] = (byte)'l';
                finalKey[encryptionKey.Length + 8] = (byte)'T';
            }

            // 3. Initialize the MD5 hash function and pass the result of 2 as input.
            using (var md5 = MD5.Create())
            {
                md5.ComputeHash(finalKey);

                // 4. Use the first (n + 5) bytes (maximum of 16) of the MD5 output as the key for the
                // RC4 or AES symmetric key algorithms along with the string or stream data to en/de-crypt
                // If using AES the Cipher Block Chaining mode with block size of 16 bytes is used. The
                // initialization vector is a 16-byte random number stored as the first 16 bytes of the stream of string.
                var length = Math.Min(16, encryptionKey.Length + 5);
                var result = new byte[length];
                Array.Copy(md5.Hash, result, length);

                return result;
            }
        }

        private static bool IsUserPassword(byte[] password, byte[] userKey, byte[] ownerKey, int permissions,
            byte[] documentId, int revision, int length, bool encryptMetadata)
        {
            switch (revision)
            {
                case 2:
                case 3:
                case 4:
                    break;
                case 5:
                case 6:
                    break;
                default:
                    throw new NotSupportedException($"Unsupported encryption revision: {revision}.");
            }

            return false;
        }

        private static byte[] CalculateKeyRevisions2To4(byte[] password, byte[] ownerKey,
            int permissions, int revision, int length, byte[] documentId, bool encryptMetadata)
        {
            // 1. Pad or truncate the password string to exactly 32 bytes. 
            var passwordFull = GetPaddedPassword(password);

            using (var md5 = MD5.Create())
            {
                // 2. Initialize the MD5 hash function and pass the result of step 1 as input to this function.
                var has = md5.ComputeHash(passwordFull);

                // 3. Pass the value of the encryption dictionary's owner key entry to the MD5 hash function. 
                var has1 = md5.ComputeHash(ownerKey);

                // 4. Treat the value of the P entry as an unsigned 4-byte integer. 
                var unsigned = (uint) permissions;
                var permissionsBytes = new []
                {
                    (byte) (unsigned),
                    (byte) (unsigned >> 8),
                    (byte) (unsigned >> 16),
                    (byte) (unsigned >> 24)
                };

                // 4. Pass these bytes to the MD5 hash function, low-order byte first.
                var has2 = md5.ComputeHash(permissionsBytes);

                // 5. Pass the first element of the file's file identifier array to the hash.
                var has3 = md5.ComputeHash(documentId);

                // 6. (Revision 4 or greater) If document metadata is not being encrypted, pass 4 bytes
                // with the value 0xFFFFFFFF to the MD5 hash function.
                if (revision >= 4)
                {
                    md5.ComputeHash(new byte[] {0xFF, 0xFF, 0xFF, 0xFF});
                }

                // 7. Do the following 50 times: Take the output from the previous MD5 hash and
                // pass the first n bytes of the output as input into a new MD5 hash,
                // where n is the number of bytes of the encryption key as defined by the value
                // of the encryption dictionary's Length entry. 
                if (revision == 3 || revision == 4)
                {
                    var n = length;

                    var input = md5.Hash;

                    for (var i = 0; i < 50; i++)
                    {
                        md5.ComputeHash(input, 0, n);
                        input = md5.Hash;
                    }
                }

                var result = new byte[length];

                Array.Copy(md5.Hash, result, length);

                return result;
            }
        }

        private static byte[] GetPaddedPassword(byte[] password)
        {
            if (password == null || password.Length == 0)
            {
                return PaddingBytes;
            }

            var result = new byte[32];

            var passwordBytes = password.Length <= 32 ? password.Length : 32;

            var paddingBytes = 32 - passwordBytes;

            Array.ConstrainedCopy(password, 0, result, 0, passwordBytes);

            if (paddingBytes > 0)
            {
                Array.ConstrainedCopy(PaddingBytes, 0, result, passwordBytes, paddingBytes);
            }

            return result;
        }
    }
}