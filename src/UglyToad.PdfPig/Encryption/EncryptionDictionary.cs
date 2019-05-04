namespace UglyToad.PdfPig.Encryption
{
    using System;
    using Tokenization.Scanner;
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
        }
    }

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
                encryptMetadata?.Data ?? false);
        }
    }

    /// <summary>
    /// A code specifying the algorithm to be used in encrypting and decrypting the document.
    /// </summary>
    internal enum EncryptionAlgorithmCode
    {
        /// <summary>
        /// An algorithm that is undocumented and no longer supported.
        /// </summary>
        Unrecognized = 0,
        /// <summary>
        /// RC4 or AES encryption using a key of 40 bits.
        /// </summary>
        Rc4OrAes40BitKey = 1,
        /// <summary>
        /// RC4 or AES encryption using a key of more than 40 bits.
        /// </summary>
        Rc4OrAesGreaterThan40BitKey = 2,
        /// <summary>
        ///  An unpublished algorithm that permits encryption key lengths ranging from 40 to 128 bits.
        /// </summary>
        UnpublishedAlgorithm40To128BitKey = 3,
        /// <summary>
        ///  The security handler defines the use of encryption and decryption in the document.
        /// </summary>
        SecurityHandlerInDocument
    }

    [Flags]
    internal enum UserAccessPermissions
    {
        /// <summary>
        /// (Revision 2) Print the document.
        /// (Revision 3 or greater) Print the document (possibly not at the highest quality level, see <see cref="PrintHighQuality"/>).
        /// </summary>
        Print = 1 << 2,
        /// <summary>
        /// Modify the contents of the document by operations other than those
        /// controlled by <see cref="AddOrModifyTextAnnotationsAndFillFormFields"/>, <see cref="FillExistingFormFields"/> and <see cref="AssembleDocument"/>. 
        /// </summary>
        Modify = 1 << 3,
        /// <summary>
        /// (Revision 2) Copy or otherwise extract text and graphics from the document, including extracting text and graphics
        /// (in support of accessibility to users with disabilities or for other purposes).
        /// (Revision 3 or greater) Copy or otherwise extract text and graphics from the document by operations other
        /// than that controlled by <see cref="ExtractTextAndGraphics"/>. 
        /// </summary>
        CopyTextAndGraphics = 1 << 4,
        /// <summary>
        /// Add or modify text annotations, fill in interactive form fields, and, if <see cref="Modify"/> is also set,
        /// create or modify interactive form fields (including signature fields). 
        /// </summary>
        AddOrModifyTextAnnotationsAndFillFormFields = 1 << 5,
        /// <summary>
        /// (Revision 3 or greater) Fill in existing interactive form fields (including signature fields),
        /// even if <see cref="AddOrModifyTextAnnotationsAndFillFormFields"/> is clear. 
        /// </summary>
        FillExistingFormFields = 1 << 8,
        /// <summary>
        /// (Revision 3 or greater) Extract text and graphics (in support of accessibility to users with disabilities or for other purposes). 
        /// </summary>
        ExtractTextAndGraphics = 1 << 9,
        /// <summary>
        /// (Revision 3 or greater) Assemble the document (insert, rotate, or delete pages and create bookmarks or thumbnail images),
        /// even if <see cref="Modify"/> is clear. 
        /// </summary>
        AssembleDocument = 1 << 10,
        /// <summary>
        /// (Revision 3 or greater) Print the document to a representation from  which a faithful digital copy of the PDF content could be generated.
        /// When this is clear (and <see cref="Print"/> is set), printing is limited to a low-level representation of the appearance,
        /// possibly of degraded quality. 
        /// </summary>
        PrintHighQuality = 1 << 12
    }

}

