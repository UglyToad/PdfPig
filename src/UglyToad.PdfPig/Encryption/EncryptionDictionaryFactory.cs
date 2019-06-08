namespace UglyToad.PdfPig.Encryption
{
    using System;
    using Tokenization.Scanner;
    using Tokens;
    using Util;

    internal static class EncryptionDictionaryFactory
    {
        public static EncryptionDictionary Read(DictionaryToken encryptionDictionary, IPdfTokenScanner tokenScanner)
        {
            if (encryptionDictionary == null)
            {
                throw new ArgumentNullException(nameof(encryptionDictionary));
            }
            
            var filter = encryptionDictionary.Get<NameToken>(NameToken.Filter, tokenScanner);

            var code = EncryptionAlgorithmCode.Unrecognized;

            if (encryptionDictionary.TryGetOptionalTokenDirect(NameToken.V, tokenScanner, out NumericToken vNum))
            {
                code = (EncryptionAlgorithmCode) vNum.Int;
            }

            var length = default(int?);
            
            if (encryptionDictionary.TryGetOptionalTokenDirect(NameToken.Length, tokenScanner, out NumericToken lengthToken))
            {
                length = lengthToken.Int;
            }

            var revision = default(int);
            if (encryptionDictionary.TryGetOptionalTokenDirect(NameToken.R, tokenScanner, out NumericToken revisionToken))
            {
                revision = revisionToken.Int;
            }

            encryptionDictionary.TryGetOptionalStringDirect(NameToken.O, tokenScanner, out var ownerString);
            encryptionDictionary.TryGetOptionalStringDirect(NameToken.U, tokenScanner, out var userString);

            var access = default(UserAccessPermissions);

            if (encryptionDictionary.TryGetOptionalTokenDirect(NameToken.P, tokenScanner, out NumericToken accessToken))
            {
                access = (UserAccessPermissions) accessToken.Int;
            }

            encryptionDictionary.TryGetOptionalTokenDirect(NameToken.EncryptMetaData, tokenScanner, out BooleanToken encryptMetadata);

            return new EncryptionDictionary(filter.Data, code, length, revision, ownerString, userString, access, encryptionDictionary,
                encryptMetadata?.Data ?? true);
        }
    }
}