namespace UglyToad.PdfPig.Encryption
{
    using Tokens;

    /// <summary>
    ///  Manages decryption of tokens in a PDF document where encryption is used.
    /// </summary>
    internal interface IEncryptionHandler
    {
        IToken Decrypt(IndirectReference reference, IToken token);
    }
}
