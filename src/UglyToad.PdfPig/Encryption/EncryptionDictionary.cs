namespace UglyToad.PdfPig.Encryption
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using Exceptions;
    using Tokens;

    internal class EncryptionDictionary
    {
        public string Filter { get; }

        public EncryptionAlgorithmCode EncryptionAlgorithmCode { get; }

        public int? KeyLength { get; }

        public int Revision { get; }

        public byte[]? OwnerBytes { get; }

        public byte[]? UserBytes { get; }

        /// <summary>
        /// Required if <see cref="Revision"/> is 5 or above. A 32-byte string, based on the owner and user passwords that is used in computing the encryption key.
        /// </summary>
        public byte[]? OwnerEncryptionBytes { get; }

        /// <summary>
        /// Required if <see cref="Revision"/> is 5 or above. A 32-byte string, based on the user password that is used in computing the encryption key.
        /// </summary>
        public byte[]? UserEncryptionBytes { get; }

        public UserAccessPermissions UserAccessPermissions { get; }

        public bool IsStandardFilter => string.Equals(Filter, "Standard", StringComparison.OrdinalIgnoreCase);

        public bool EncryptMetadata { get; }

        public DictionaryToken Dictionary { get; }

        public EncryptionDictionary(string filter, EncryptionAlgorithmCode encryptionAlgorithmCode, 
            int? keyLength, 
            int revision, 
            byte[]? ownerBytes, 
            byte[]? userBytes, 
            byte[]? ownerEncryptionBytes,
            byte[]? userEncryptionBytes,
            UserAccessPermissions userAccessPermissions, 
            DictionaryToken dictionary, 
            bool encryptMetadata)
        {
            Filter = filter;
            EncryptionAlgorithmCode = encryptionAlgorithmCode;
            KeyLength = keyLength;
            Revision = revision;
            OwnerBytes = ownerBytes;
            UserBytes = userBytes;
            OwnerEncryptionBytes = ownerEncryptionBytes;
            UserEncryptionBytes = userEncryptionBytes;
            UserAccessPermissions = userAccessPermissions;
            Dictionary = dictionary;
            EncryptMetadata = encryptMetadata;
        }

        public bool TryGetCryptHandler([NotNullWhen(true)] out CryptHandler? cryptHandler)
        {
            cryptHandler = null;

            if (EncryptionAlgorithmCode != EncryptionAlgorithmCode.SecurityHandlerInDocument
                && EncryptionAlgorithmCode != EncryptionAlgorithmCode.SecurityHandlerInDocument256)
            {
                return false;
            }

            if (!Dictionary.TryGet(NameToken.Cf, out DictionaryToken cryptFilterDictionary))
            {
                return false;
            }

            var namedFilters = cryptFilterDictionary;

            var streamFilterName = Dictionary.TryGet(NameToken.StmF, out NameToken streamFilterToken) ? streamFilterToken : NameToken.Identity;
            var stringFilterName = Dictionary.TryGet(NameToken.StrF, out NameToken stringFilterToken) ? stringFilterToken : NameToken.Identity;

            if (streamFilterName != NameToken.Identity && !namedFilters.TryGet(streamFilterName, out _))
            {
                throw new PdfDocumentEncryptedException($"Stream filter {streamFilterName} not found in crypt dictionary: {cryptFilterDictionary}.");
            }

            if (stringFilterName != NameToken.Identity && !namedFilters.TryGet(stringFilterName, out _))
            {
                throw new PdfDocumentEncryptedException($"String filter {stringFilterName} not found in crypt dictionary: {cryptFilterDictionary}.");
            }

            cryptHandler = new CryptHandler(namedFilters, streamFilterName, stringFilterName);

            return true;
        }
    }
}

