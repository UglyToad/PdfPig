namespace UglyToad.PdfPig.Encryption
{
    using Core;
    using Tokens;

    internal class NoOpEncryptionHandler : IEncryptionHandler
    {
        public static NoOpEncryptionHandler Instance { get; } = new NoOpEncryptionHandler();

        private NoOpEncryptionHandler()
        {
        }

        public IToken Decrypt(IndirectReference reference, IToken token)
        {
            return token;
        }
    }
}