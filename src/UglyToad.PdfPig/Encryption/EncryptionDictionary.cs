namespace UglyToad.PdfPig.Encryption
{
    using System;
    using Exceptions;
    using Tokens;
    using Util;

    internal class EncryptionDictionary
    {
        public string Filter { get; }

        public EncryptionAlgorithmCode EncryptionAlgorithmCode { get; }

        public int? KeyLength { get; }

        public int StandardSecurityHandlerRevision { get; }

        public string OwnerPasswordCheck { get; }

        public string UserPasswordCheck { get; }

        public byte[] OwnerBytes { get; }

        public byte[] UserBytes { get; }

        public UserAccessPermissions UserAccessPermissions { get; }

        public bool IsStandardFilter => string.Equals(Filter, "Standard", StringComparison.OrdinalIgnoreCase);

        public bool EncryptMetadata { get; }

        public DictionaryToken Dictionary { get; }

        public EncryptionDictionary(string filter, EncryptionAlgorithmCode encryptionAlgorithmCode, 
            int? keyLength, 
            int standardSecurityHandlerRevision, 
            string ownerPasswordCheck, 
            string userPasswordCheck, 
            UserAccessPermissions userAccessPermissions, 
            DictionaryToken dictionary, 
            bool encryptMetadata)
        {
            Filter = filter;
            EncryptionAlgorithmCode = encryptionAlgorithmCode;
            KeyLength = keyLength;
            StandardSecurityHandlerRevision = standardSecurityHandlerRevision;
            OwnerPasswordCheck = ownerPasswordCheck;
            UserPasswordCheck = userPasswordCheck;
            UserAccessPermissions = userAccessPermissions;
            Dictionary = dictionary;
            EncryptMetadata = encryptMetadata;

            OwnerBytes = OtherEncodings.StringAsLatin1Bytes(ownerPasswordCheck);
            UserBytes = OtherEncodings.StringAsLatin1Bytes(userPasswordCheck);
        }

        public bool TryGetCryptHandler(out CryptHandler cryptHandler)
        {
            cryptHandler = null;

            if (EncryptionAlgorithmCode != EncryptionAlgorithmCode.SecurityHandlerInDocument)
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

