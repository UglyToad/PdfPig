namespace UglyToad.PdfPig.Encryption
{
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
}