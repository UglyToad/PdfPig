namespace UglyToad.PdfPig.Encryption
{
    using System.Collections.Generic;
    using Tokens;

    internal class NoOpEncryptionHandler : IEncryptionHandler
    {
        public static NoOpEncryptionHandler Instance { get; } = new NoOpEncryptionHandler();

        private NoOpEncryptionHandler()
        {
        }

        public IReadOnlyList<byte> Decrypt(StreamToken stream)
        {
            return stream.Data;
        }
    }
}