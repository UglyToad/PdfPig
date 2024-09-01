﻿namespace UglyToad.PdfPig.Encryption
{
    using System;
    using System.Linq;
    using Core;
    using System.Diagnostics;
    using System.Threading;
    using Tokenization.Scanner;
    using Tokens;
    using Util;

    internal static class EncryptionDictionaryFactory
    {
        public static EncryptionDictionary Read(DictionaryToken encryptionDictionary, IPdfTokenScanner tokenScanner)
        {
            if (encryptionDictionary is null)
            {
                throw new ArgumentNullException(nameof(encryptionDictionary));
            }
            
            var filter = encryptionDictionary.Get<NameToken>(NameToken.Filter, tokenScanner);

            var code = EncryptionAlgorithmCode.Unrecognized;

            if (encryptionDictionary.TryGetOptionalTokenDirect(NameToken.V, tokenScanner, out NumericToken? vNum))
            {
                code = (EncryptionAlgorithmCode) vNum.Int;
            }

            var length = default(int?);
            
            if (encryptionDictionary.TryGetOptionalTokenDirect(NameToken.Length, tokenScanner, out NumericToken? lengthToken))
            {
                length = lengthToken.Int;
            }

            var revision = default(int);
            if (encryptionDictionary.TryGetOptionalTokenDirect(NameToken.R, tokenScanner, out NumericToken? revisionToken))
            {
                revision = revisionToken.Int;
            }

            byte[]? ownerBytes = null;
            if (encryptionDictionary.TryGet(NameToken.O, out IToken ownerToken))
            {
                if (ownerToken is StringToken ownerString)
                {
                    ownerBytes = ownerString.GetBytes();
                }
                else if (ownerToken is HexToken ownerHex)
                {
                    ownerBytes = ownerHex.Bytes.ToArray();
                }
            }
            
            byte[]? userBytes = null;
            if (encryptionDictionary.TryGet(NameToken.U, out IToken userToken))
            {
                if (userToken is StringToken userString)
                {
                    userBytes = userString.GetBytes();
                }
                else if (userToken is HexToken userHex)
                {
                    userBytes = userHex.Bytes.ToArray();
                }
            }
            
            var access = default(UserAccessPermissions);

            if (encryptionDictionary.TryGetOptionalTokenDirect(NameToken.P, tokenScanner, out NumericToken? accessToken))
            {
                // This can be bigger than an integer.
                access = (UserAccessPermissions) accessToken.Long;
            }

            byte[]? userEncryptionBytes = null;
            byte[]? ownerEncryptionBytes = null;
            if (revision >= 5)
            {
                ownerEncryptionBytes = GetEncryptionBytesOrDefault(encryptionDictionary, tokenScanner, false);
                userEncryptionBytes = GetEncryptionBytesOrDefault(encryptionDictionary, tokenScanner, true);
            }

            encryptionDictionary.TryGetOptionalTokenDirect(NameToken.EncryptMetaData, tokenScanner, out BooleanToken? encryptMetadata);

            return new EncryptionDictionary(filter.Data, code, length, revision,
                ownerBytes,
                userBytes, 
                ownerEncryptionBytes,
                userEncryptionBytes,
                access, 
                encryptionDictionary,
                encryptMetadata?.Data ?? true);
        }

        private static byte[]? GetEncryptionBytesOrDefault(DictionaryToken encryptionDictionary, IPdfTokenScanner tokenScanner, bool isUser)
        {
            var name = isUser ? NameToken.Ue : NameToken.Oe;
            if (encryptionDictionary.TryGet(name, tokenScanner, out StringToken? stringToken))
            {
                return stringToken.GetBytes();
            }

            if (encryptionDictionary.TryGet(name, tokenScanner, out HexToken? hexToken))
            {
                return hexToken.Bytes.ToArray();
            }

            return null;
        }
    }
}