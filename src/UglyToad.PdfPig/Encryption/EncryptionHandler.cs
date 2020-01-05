namespace UglyToad.PdfPig.Encryption
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;
    using Core;
    using CrossReference;
    using Exceptions;
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

        private readonly HashSet<IndirectReference> previouslyDecrypted = new HashSet<IndirectReference>();

        [CanBeNull]
        private readonly EncryptionDictionary encryptionDictionary;

        [CanBeNull]
        private readonly CryptHandler cryptHandler;

        private readonly byte[] encryptionKey;

        private readonly bool useAes;

        public EncryptionHandler(EncryptionDictionary encryptionDictionary, TrailerDictionary trailerDictionary, IReadOnlyList<string> passwords)
        {
            this.encryptionDictionary = encryptionDictionary;

            passwords = passwords ?? new[] { string.Empty };

            if (!passwords.Contains(string.Empty))
            {
                passwords = new List<string>(passwords)
                {
                    string.Empty
                };
            }

            byte[] documentIdBytes;

            if (trailerDictionary.Identifier != null && trailerDictionary.Identifier.Count == 2)
            {
                var token = trailerDictionary.Identifier[0];

                switch (token)
                {
                    case HexToken hex:
                        documentIdBytes = hex.Bytes.ToArray();
                        break;
                    default:
                        documentIdBytes = OtherEncodings.StringAsLatin1Bytes(token.Data);
                        break;
                }

            }
            else
            {
                documentIdBytes = EmptyArray<byte>.Instance;
            }

            if (encryptionDictionary == null)
            {
                return;
            }

            useAes = false;

            if (encryptionDictionary.EncryptionAlgorithmCode == EncryptionAlgorithmCode.SecurityHandlerInDocument
                || encryptionDictionary.EncryptionAlgorithmCode == EncryptionAlgorithmCode.SecurityHandlerInDocument256)
            {
                if (!encryptionDictionary.TryGetCryptHandler(out var cryptHandlerLocal))
                {
                    throw new PdfDocumentEncryptedException("Document encrypted with security handler in document but no crypt dictionary found.", encryptionDictionary);
                }

                cryptHandler = cryptHandlerLocal;

                useAes = cryptHandlerLocal?.StreamDictionary?.Name == CryptDictionary.Method.AesV2
                    || cryptHandlerLocal?.StreamDictionary?.Name == CryptDictionary.Method.AesV3;
            }

            var charset = OtherEncodings.Iso88591;

            if (encryptionDictionary.Revision == 5 || encryptionDictionary.Revision == 6)
            {
                // ReSharper disable once RedundantAssignment
                charset = Encoding.UTF8;
            }

            var length = encryptionDictionary.EncryptionAlgorithmCode == EncryptionAlgorithmCode.Rc4OrAes40BitKey
                ? 5
                : encryptionDictionary.KeyLength.GetValueOrDefault() / 8;

            var foundPassword = false;

            foreach (var password in passwords)
            {
                var passwordBytes = charset.GetBytes(password);

                var isUserPassword = false;
                byte[] decryptionPasswordBytes;

                if (IsOwnerPassword(passwordBytes, encryptionDictionary, length, documentIdBytes, out var userPassBytes))
                {
                    if (encryptionDictionary.Revision == 5 || encryptionDictionary.Revision == 6)
                    {
                        decryptionPasswordBytes = passwordBytes;
                    }
                    else
                    {
                        decryptionPasswordBytes = userPassBytes;
                    }
                }
                else if (IsUserPassword(passwordBytes, encryptionDictionary, length, documentIdBytes))
                {
                    decryptionPasswordBytes = passwordBytes;
                    isUserPassword = true;
                }
                else
                {
                    continue;
                }

                encryptionKey = CalculateEncryptionKey(decryptionPasswordBytes, encryptionDictionary,
                    length,
                    documentIdBytes,
                    isUserPassword);

                foundPassword = true;

                break;
            }

            if (!foundPassword)
            {
                throw new PdfDocumentEncryptedException("The document was encrypted and none of the provided passwords were the user or owner password.",
                    encryptionDictionary);
            }
        }

        private static bool IsUserPassword(byte[] passwordBytes, EncryptionDictionary encryptionDictionary, int length, byte[] documentIdBytes)
        {
            if (encryptionDictionary.Revision == 5 || encryptionDictionary.Revision == 6)
            {
                return IsUserPasswordRevision5And6(passwordBytes, encryptionDictionary);
            }

            // 1. Create an encryption key based on the user password string.
            var calculatedEncryptionKey = CalculateKeyRevisions2To4(passwordBytes, encryptionDictionary, length, documentIdBytes);

            byte[] output;

            if (encryptionDictionary.Revision >= 3)
            {
                using (var md5 = MD5.Create())
                {
                    // 2. Initialize the MD5 hash function and pass the 32-byte padding string.
                    UpdateMd5(md5, PaddingBytes);

                    // 3.   Pass the first element of the file identifier array to the hash function and finish the hash. 
                    UpdateMd5(md5, documentIdBytes);
                    md5.TransformFinalBlock(EmptyArray<byte>.Instance, 0, 0);

                    var result = md5.Hash;

                    // 4. Encrypt the 16-byte result of the hash, using an RC4 encryption function with the encryption key from step 1.
                    var temp = RC4.Encrypt(calculatedEncryptionKey, result);

                    // 5. Do the following 19 times:
                    for (byte i = 1; i <= 19; i++)
                    {
                        // Take the output from the previous invocation of the RC4 function
                        // and pass it as input to a new invocation of the function

                        // Use an encryption key generated by taking each byte of the original encryption key (from step 1) 
                        // and performing an XOR operation between that byte and the single-byte value of the iteration counter. 
                        var key = calculatedEncryptionKey.Select(x => (byte)(x ^ i)).ToArray();
                        temp = RC4.Encrypt(key, temp);
                    }

                    output = temp;
                }
            }
            else
            {
                // 2. Encrypt the 32-byte padding string using an RC4 encryption function with the encryption key from the preceding step.
                output = RC4.Encrypt(calculatedEncryptionKey, PaddingBytes);
            }

            if (encryptionDictionary.Revision >= 3)
            {
                return encryptionDictionary.UserBytes.Take(16).SequenceEqual(output.Take(16));
            }

            return encryptionDictionary.UserBytes.SequenceEqual(output);
        }

        private static bool IsUserPasswordRevision5And6(byte[] passwordBytes, EncryptionDictionary encryptionDictionary)
        {
            // Test the password against the user key by computing the SHA-256 hash of the UTF-8 password concatenated with the 8 bytes of User Validation Salt.
            // If the 32-byte result matches the first 32 bytes of the U string, this is the user password
            var truncatedPassword = TruncatePasswordTo127Bytes(passwordBytes);

            // The 48-byte string consisting of the 32-byte hash followed by the User Validation Salt followed by the User Key Salt is stored as the U key
            var userPasswordHash = new byte[32];
            var userValidationSalt = new byte[8];
            Array.Copy(encryptionDictionary.UserBytes, userPasswordHash, 32);
            Array.Copy(encryptionDictionary.UserBytes, 32, userValidationSalt, 0, 8);

            byte[] result;
            if (encryptionDictionary.Revision == 6)
            {
                result = ComputeStupidIsoHash(truncatedPassword, userValidationSalt, null);
            }
            else
            {
                result = ComputeSha256Hash(truncatedPassword, userValidationSalt);
            }

            return result.SequenceEqual(userPasswordHash);
        }

        private static bool IsOwnerPassword(byte[] passwordBytes, EncryptionDictionary encryptionDictionary, int length, byte[] documentIdBytes,
            out byte[] userPassword)
        {
            userPassword = null;

            if (encryptionDictionary.Revision == 5 || encryptionDictionary.Revision == 6)
            {
                return IsOwnerPasswordRevision5And6(passwordBytes, encryptionDictionary);
            }

            // 1. Pad or truncate the owner password string, if there is no owner password use the user password instead.
            var paddedPassword = GetPaddedPassword(passwordBytes);

            using (var md5 = MD5.Create())
            {
                // 2. Initialize the MD5 hash function and pass the result of step 1 as input.
                var hash = md5.ComputeHash(paddedPassword);

                // 3. (Revision 3 or greater) Do the following 50 times: 
                if (encryptionDictionary.Revision >= 3)
                {
                    // Take the output from the previous MD5 hash and pass it as input into a new MD5 hash.
                    for (var i = 0; i < 50; i++)
                    {
                        hash = md5.ComputeHash(md5.Hash);
                    }
                }

                // 4. Create an RC4 encryption key using the first n bytes of the output from the final MD5 hash,
                // where n is always 5 for revision 2 but for revision 3 or greater depends on the value of the encryption dictionary's Length entry. 
                var key = hash.Take(length).ToArray();

                if (encryptionDictionary.Revision == 2)
                {
                    // 5. (Revision 2 only) Decrypt the value of the encryption dictionary's owner entry, 
                    // using an RC4 encryption function with the encryption key computed in step 1 - 4.
                    userPassword = RC4.Encrypt(key, encryptionDictionary.OwnerBytes);
                }
                else
                {
                    // 5. (Revision 3 or greater) Do the following 20 times:
                    byte[] output = null;
                    for (var i = 0; i < 20; i++)
                    {
                        // Generate a per iteration key by taking the original key and performing an XOR operation between each
                        // byte of the key and the single-byte value of the iteration counter (from 19 to 0).
                        var keyIter = key.Select(x => (byte)(x ^ (19 - i))).ToArray();

                        if (i == 0)
                        {
                            output = encryptionDictionary.OwnerBytes;
                        }

                        // Decrypt the value of the encryption dictionary's owner entry (first iteration) 
                        // or the output from the previous iteration using an RC4 encryption function. 
                        output = RC4.Encrypt(keyIter, output);
                    }

                    userPassword = output;
                }

                // 6. The result of step 5 purports to be the user password. 
                // Authenticate this user password, if it is correct, the password supplied is the owner password. 
                var result = IsUserPassword(userPassword, encryptionDictionary, length, documentIdBytes);

                return result;
            }
        }

        private static bool IsOwnerPasswordRevision5And6(byte[] passwordBytes, EncryptionDictionary encryptionDictionary)
        {
            // Test the password against the user key by computing the SHA-256 hash of the UTF-8 password concatenated with the 8 bytes of Owner Validation Salt and the 48 byte  U string.
            // If the 32 byte result matches the first 32 bytes of the O string, this is the user password.

            var truncatedPassword = TruncatePasswordTo127Bytes(passwordBytes);

            // The 48-byte string consisting of the 32-byte hash followed by the Owner Validation Salt followed by the Owner Key Salt is stored as the O key.
            var ownerHash = new byte[32];
            var validationSalt = new byte[8];
            Array.Copy(encryptionDictionary.OwnerBytes, ownerHash, ownerHash.Length);
            Array.Copy(encryptionDictionary.OwnerBytes, ownerHash.Length, validationSalt, 0, validationSalt.Length);

            byte[] result;
            if (encryptionDictionary.Revision == 6)
            {
                result = ComputeStupidIsoHash(truncatedPassword, validationSalt, encryptionDictionary.UserBytes);
            }
            else
            {
                result = ComputeSha256Hash(truncatedPassword, validationSalt, encryptionDictionary.UserBytes);
            }

            return result.SequenceEqual(ownerHash);
        }

        public IToken Decrypt(IndirectReference reference, IToken token)
        {
            if (token == null)
            {
                throw new ArgumentNullException(nameof(token));
            }

            try
            {
                token = DecryptInternal(reference, token);

                previouslyDecrypted.Add(reference);

                return token;
            }
            catch (Exception ex)
            {
                throw new PdfDocumentEncryptedException($"The document was encrypted and decryption of a token failed. Token was: {token}.", encryptionDictionary, ex);
            }
        }

        private IToken DecryptInternal(IndirectReference reference, IToken token)
        {
            switch (token)
            {
                case StreamToken stream:
                    {
                        if (cryptHandler?.StreamDictionary?.IsIdentity == true
                            || cryptHandler?.StreamDictionary?.Name == CryptDictionary.Method.None)
                        {
                            // TODO: No idea if this is right.
                            return token;
                        }

                        if (stream.StreamDictionary.TryGet(NameToken.Type, out NameToken typeName))
                        {
                            if (NameToken.Xref.Equals(typeName))
                            {
                                return token;
                            }

                            if (!encryptionDictionary.EncryptMetadata && NameToken.Metadata.Equals(typeName))
                            {
                                return token;
                            }

                            // TODO: check unencrypted metadata
                        }

                        var streamDictionary = (DictionaryToken)DecryptInternal(reference, stream.StreamDictionary);

                        var decrypted = DecryptData(stream.Data.ToArray(), reference);

                        token = new StreamToken(streamDictionary, decrypted);

                        break;
                    }
                case StringToken stringToken:
                    {
                        if (cryptHandler?.StringDictionary?.IsIdentity == true
                            || cryptHandler?.StringDictionary?.Name == CryptDictionary.Method.None)
                        {
                            // TODO: No idea if this is right.
                            return token;
                        }

                        var data = OtherEncodings.StringAsLatin1Bytes(stringToken.Data);

                        var decrypted = DecryptData(data, reference);

                        token = new StringToken(OtherEncodings.BytesAsLatin1String(decrypted));

                        break;
                    }
                case HexToken hexToken:
                    {
                        var data = hexToken.Bytes.ToArray();

                        var decrypted = DecryptData(data, reference);

                        token = new HexToken(Hex.GetString(decrypted).ToCharArray());

                        break;
                    }
                case DictionaryToken dictionary:
                    {
                        // PDFBOX-2936: avoid orphan /CF dictionaries found in US govt "I-" files
                        if (dictionary.TryGet(NameToken.Cf, out _))
                        {
                            return token;
                        }

                        var isSignatureDictionary = dictionary.TryGet(NameToken.Type, out NameToken typeName)
                                                    && (typeName.Equals(NameToken.Sig) || typeName.Equals(NameToken.DocTimeStamp));

                        foreach (var keyValuePair in dictionary.Data)
                        {
                            if (isSignatureDictionary && keyValuePair.Key == NameToken.Contents.Data)
                            {
                                continue;
                            }

                            if (keyValuePair.Value is StringToken || keyValuePair.Value is ArrayToken
                                                                  || keyValuePair.Value is DictionaryToken
                                                                  || keyValuePair.Value is HexToken)
                            {
                                var inner = DecryptInternal(reference, keyValuePair.Value);
                                dictionary = dictionary.With(keyValuePair.Key, inner);
                            }
                        }

                        token = dictionary;

                        break;
                    }
                case ArrayToken array:
                    {
                        var result = new IToken[array.Length];

                        for (var i = 0; i < array.Length; i++)
                        {
                            result[i] = DecryptInternal(reference, array.Data[i]);
                        }

                        token = new ArrayToken(result);

                        break;
                    }
            }

            return token;
        }

        private byte[] DecryptData(byte[] data, IndirectReference reference)
        {
            if (useAes && encryptionKey.Length == 32)
            {
                return AesEncryptionHelper.Decrypt(data, encryptionKey);
            }

            var finalKey = GetObjectKey(reference);

            if (useAes)
            {
                return AesEncryptionHelper.Decrypt(data, finalKey);
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

            finalKey[encryptionKey.Length] = (byte)reference.ObjectNumber;
            finalKey[encryptionKey.Length + 1] = (byte)(reference.ObjectNumber >> 8);
            finalKey[encryptionKey.Length + 2] = (byte)(reference.ObjectNumber >> 16);

            finalKey[encryptionKey.Length + 3] = (byte)reference.Generation;
            finalKey[encryptionKey.Length + 4] = (byte)(reference.Generation >> 8);

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

        private static byte[] CalculateEncryptionKey(byte[] password, EncryptionDictionary encryptionDictionary, int length, byte[] documentId, bool isUserPassword)
        {
            if (encryptionDictionary.Revision >= 2 && encryptionDictionary.Revision <= 4)
            {
                return CalculateKeyRevisions2To4(password, encryptionDictionary, length, documentId);
            }

            if (encryptionDictionary.Revision <= 6)
            {
                return CalculateKeyRevisions5And6(password, encryptionDictionary, isUserPassword);
            }

            throw new PdfDocumentEncryptedException($"PDF encrypted with unrecognized revision: {encryptionDictionary}.");
        }

        private static byte[] CalculateKeyRevisions2To4(byte[] password, EncryptionDictionary encryptionDictionary, int length, byte[] documentId)
        {
            // 1. Pad or truncate the password string to exactly 32 bytes. 
            var passwordFull = GetPaddedPassword(password);

            var revision = encryptionDictionary.Revision;

            using (var md5 = MD5.Create())
            {
                // 2. Initialize the MD5 hash function and pass the result of step 1 as input to this function.
                UpdateMd5(md5, passwordFull);

                // 3. Pass the value of the encryption dictionary's owner key entry to the MD5 hash function. 
                UpdateMd5(md5, encryptionDictionary.OwnerBytes);

                // 4. Treat the value of the P entry as an unsigned 4-byte integer. 
                var unsigned = (uint)encryptionDictionary.UserAccessPermissions;

                // 4. Pass these bytes to the MD5 hash function, low-order byte first.
                UpdateMd5(md5, new[] { (byte)(unsigned) });
                UpdateMd5(md5, new[] { (byte)(unsigned >> 8) });
                UpdateMd5(md5, new[] { (byte)(unsigned >> 16) });
                UpdateMd5(md5, new[] { (byte)(unsigned >> 24) });

                // 5. Pass the first element of the file's file identifier array to the hash.
                UpdateMd5(md5, documentId);

                // 6. (Revision 4 or greater) If document metadata is not being encrypted, pass 4 bytes
                // with the value 0xFFFFFFFF to the MD5 hash function.
                if (revision >= 4 && !encryptionDictionary.EncryptMetadata)
                {
                    UpdateMd5(md5, new byte[] { 0xFF, 0xFF, 0xFF, 0xFF });
                }

                // 7. Do the following 50 times: Take the output from the previous MD5 hash and
                // pass the first n bytes of the output as input into a new MD5 hash,
                // where n is the number of bytes of the encryption key as defined by the value
                // of the encryption dictionary's Length entry. 
                if (revision == 3 || revision == 4)
                {
                    var n = length;

                    md5.TransformFinalBlock(EmptyArray<byte>.Instance, 0, 0);

                    var input = md5.Hash;
                    using (var newMd5 = MD5.Create())
                    {
                        for (var i = 0; i < 50; i++)
                        {
                            input = newMd5.ComputeHash(input.Take(n).ToArray());
                        }
                    }

                    var result = new byte[length];

                    Array.Copy(input, result, length);

                    return result;
                }
                else
                {
                    md5.TransformFinalBlock(EmptyArray<byte>.Instance, 0, 0);

                    var result = new byte[length];

                    Array.Copy(md5.Hash, result, length);

                    return result;
                }
            }
        }

        private static byte[] CalculateKeyRevisions5And6(byte[] password, EncryptionDictionary encryptionDictionary, bool isUserPassword)
        {
            // Truncate the UTF-8 representation of the password to 127 bytes if it is longer than 127 bytes
            password = TruncatePasswordTo127Bytes(password);

            byte[] intermediateKey;
            byte[] encryptedFileKey;
            // If the password is the owner password:
            // Compute an intermediate owner key by computing the SHA-256 hash of the UTF-8 password concatenated with the 8 bytes of owner Key Salt,
            // concatenated with the 48-byte U string. The 32-byte result is the key used to decrypt the 32-byte OE string using AES-256 in CBC mode
            // with no padding and an initialization vector of zero. The 32-byte result is the file encryption key.
            if (!isUserPassword)
            {
                var ownerKeySalt = new byte[8];
                Array.Copy(encryptionDictionary.OwnerBytes, 40, ownerKeySalt, 0, ownerKeySalt.Length);

                if (encryptionDictionary.Revision == 6)
                {
                    intermediateKey = ComputeStupidIsoHash(password, ownerKeySalt, encryptionDictionary.UserBytes);
                }
                else
                {
                    intermediateKey = ComputeSha256Hash(password, ownerKeySalt, encryptionDictionary.UserBytes);
                }

                encryptedFileKey = encryptionDictionary.OwnerEncryptionBytes;
            }
            else
            {
                // If the password is the user password:
                // Compute an intermediate user key by computing the SHA-256 hash of the UTF-8 password concatenated with the 8 bytes of user Key Salt.
                // The 32-byte result is the key used to decrypt the 32-byte UE string using AES-256 in CBC mode with no padding and an initialization vector of zero.
                // The 32-byte result is the file encryption key.
                var userKeySalt = new byte[8];
                Array.Copy(encryptionDictionary.UserBytes, 40, userKeySalt, 0, 8);

                if (encryptionDictionary.Revision == 6)
                {
                    intermediateKey = ComputeStupidIsoHash(password, userKeySalt, null);
                }
                else
                {
                    intermediateKey = ComputeSha256Hash(password, userKeySalt);
                }

                encryptedFileKey = encryptionDictionary.UserEncryptionBytes;
            }

            var iv = new byte[16];

            using (var rijndaelManaged = new RijndaelManaged { Key = intermediateKey, IV = iv, Mode = CipherMode.CBC, Padding = PaddingMode.None })
            using (var memoryStream = new MemoryStream(encryptedFileKey))
            using (var output = new MemoryStream())
            using (var cryptoStream = new CryptoStream(memoryStream, rijndaelManaged.CreateDecryptor(intermediateKey, iv), CryptoStreamMode.Read))
            {
                cryptoStream.CopyTo(output);
                var result = output.ToArray();

                return result;
            }
        }

        private static byte[] ComputeSha256Hash(byte[] input1, byte[] input2, byte[] input3 = null)
        {
            using (var sha = SHA256.Create())
            {
                sha.TransformBlock(input1, 0, input1.Length, null, 0);
                sha.TransformBlock(input2, 0, input2.Length, null, 0);

                if (input3 != null)
                {
                    sha.TransformFinalBlock(input3, 0, input3.Length);
                }
                else
                {
                    sha.TransformFinalBlock(EmptyArray<byte>.Instance, 0, 0);
                }

                return sha.Hash;
            }
        }

        private static void UpdateMd5(MD5 md5, byte[] data)
        {
            md5.TransformBlock(data, 0, data.Length, null, 0);
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

        private static byte[] TruncatePasswordTo127Bytes(byte[] password)
        {
            if (password.Length <= 127)
            {
                return password;
            }

            var result = new byte[127];
            Array.Copy(password, result, 127);
            return result;
        }

        /// <summary>
        /// This is the revision 6 algorithm the specification for which can't be viewed unless you pay the ISO a lot of money (which makes me extremely angry).
        /// This is an attempt to port directly from PDFBox code.
        /// </summary>
        /// <param name="password">The truncated user or owner password.</param>
        /// <param name="salt">The 8 byte user or owner key validation salt.</param>
        /// <param name="vector">Used when hashing owner password in which case it is the 48 byte /U key.</param>
        private static byte[] ComputeStupidIsoHash(byte[] password, byte[] salt, byte[] vector)
        {
            // There are some details here https://web.archive.org/web/20180311160224/esec-lab.sogeti.com/posts/2011/09/14/the-undocumented-password-validation-algorithm-of-adobe-reader-x.html
            if (vector == null)
            {
                vector = EmptyArray<byte>.Instance;
            }
            else if (vector.Length > 0 && vector.Length < 48)
            {
                throw new PdfDocumentEncryptedException($"Vector for revision 6 owner password check (/U) is the wrong length, expected 48 bytes got {vector.Length} bytes.");
            }
            else if (vector.Length > 48)
            {
                var temp = new byte[48];
                Array.Copy(vector, temp, temp.Length);
                vector = temp;
            }

            password = TruncatePasswordTo127Bytes(password);

            byte[] input;
            using (var sha256 = SHA256.Create())
            {
                sha256.TransformBlock(password, 0, password.Length, null, 0);
                sha256.TransformBlock(salt, 0, salt.Length, null, 0);
                sha256.TransformBlock(vector, 0, vector.Length, null, 0);
                sha256.TransformFinalBlock(EmptyArray<byte>.Instance, 0, 0);
                input = sha256.Hash;
            }

            var key = new byte[16];
            Array.Copy(input, key, 16);

            var iv = new byte[16];
            Array.Copy(input, 16, iv, 0, iv.Length);

            byte[] x = null;
            var i = 0;
            while (i < 64 || i < x[x.Length - 1] + 32)
            {
                var roundResult = new byte[64 * (password.Length + input.Length + vector.Length)];

                var position = 0;
                for (var j = 0; j < 64; j++)
                {
                    Array.Copy(password, 0, roundResult, position, password.Length);
                    position += password.Length;
                    Array.Copy(input, 0, roundResult, position, input.Length);
                    position += input.Length;
                    if (vector.Length > 0)
                    {
                        Array.Copy(vector, 0, roundResult, position, vector.Length);
                        position += vector.Length;
                    }
                }

                using (var aes = Aes.Create())
                {
                    aes.Key = key;
                    aes.IV = iv;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.None;

                    using (var decryptor = aes.CreateEncryptor())
                    {
                        x = decryptor.TransformFinalBlock(roundResult, 0, roundResult.Length);
                    }
                }

                var sumOfFirstSixteenBytesOfX = x.Take(16).Sum(v => (long)v);
                var mod3 = sumOfFirstSixteenBytesOfX % 3;

                HashAlgorithm nextHash;
                switch (mod3)
                {
                    case 0:
                        nextHash = SHA256.Create();
                        break;
                    case 1:
                        nextHash = SHA384.Create();
                        break;
                    case 2:
                        nextHash = SHA512.Create();
                        break;
                    default:
                        throw new PdfDocumentEncryptedException("Invalid remainder from summing first sixteen bytes of this round's hash.");
                }

                input = nextHash.ComputeHash(x);
                Array.Copy(input, key, 16);
                Array.Copy(input, 16, iv, 0, 16);

                i++;
            }

            if (input.Length > 32)
            {
                var result = new byte[32];
                Array.Copy(input, result, result.Length);
                return result;
            }

            return input;
        }
    }
}