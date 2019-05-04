namespace UglyToad.PdfPig.Encryption
{
    using System.Collections.Generic;
    using Tokens;

    /// <summary>
    ///  Manages decryption of tokens in a PDF document where encryption is used.
    /// </summary>
    internal interface IEncryptionHandler
    {
        /// <summary>
        /// Decrypt the contents of the stream if encryption is applied.
        /// </summary>
        IReadOnlyList<byte> Decrypt(StreamToken stream);
    }
}
