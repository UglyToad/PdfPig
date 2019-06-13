namespace UglyToad.PdfPig.Encryption
{
    internal class CryptDictionary
    {
        public static CryptDictionary Identity { get; } = new CryptDictionary();
        
        public Method Name { get; }

        public TriggerEvent Event { get; }

        public int Length { get; }

        public bool IsIdentity { get; }

        public CryptDictionary(Method name, TriggerEvent @event, int length)
        {
            Name = name;
            Event = @event;
            Length = length;
            IsIdentity = false;
        }

        private CryptDictionary()
        {
            Name = Method.None;
            IsIdentity = true;
        }

        /// <summary>
        /// The method used by the consumer application to decrypt data.
        /// </summary>
        public enum Method
        {
            /// <summary>
            /// The application does not decrypt data but directs the input stream
            /// to the security handler for decryption.
            /// </summary>
            None,
            /// <summary>
            /// The application asks the security handler for the encryption key
            /// and implicitly decrypts data using the RC4 algorithm.
            /// </summary>
            V2,
            /// <summary>
            /// (PDF 1.6) The application asks the security handler for the encryption key and implicitly decrypts data using the AES algorithm in Cipher Block Chaining (CBC) mode
            /// with a 16-byte block size and an initialization vector that is randomly generated and placed as the first 16 bytes in the stream or string. 
            /// </summary>
            AesV2,
            /// <summary>
            /// The application asks the security handler for the encryption key and implicitly decrypts data using the AES-256 algorithm in Cipher Block Chaining (CBC) with padding mode 
            /// with a 16-byte block size and an initialization vector that is randomly generated and placed as the first 16 bytes in the stream or string. 
            /// The key size shall be 256 bits.
            /// </summary>
            AesV3
        }

        /// <summary>
        /// The event to be used to trigger the authorization that is required
        /// to access encryption keys used by this filter. 
        /// </summary>
        public enum TriggerEvent
        {
            /// <summary>
            /// Authorization is required when a document is opened.
            /// </summary>
            DocumentOpen,
            /// <summary>
            /// Authorization is required when accessing embedded files.
            /// </summary>
            EmbeddedFileOpen
        }
    }
}
