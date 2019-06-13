namespace UglyToad.PdfPig.Encryption
{
    using System;
    using Exceptions;
    using Tokens;

    internal class CryptHandler
    {
        private readonly DictionaryToken cryptDictionary;

        public CryptDictionary StreamDictionary { get; }

        public CryptDictionary StringDictionary { get; }

        public CryptHandler(DictionaryToken cryptDictionary,
            NameToken streamName, NameToken stringName)
        {
            if (streamName == null)
            {
                throw new ArgumentNullException(nameof(streamName));
            }

            if (stringName == null)
            {
                throw new ArgumentNullException(nameof(stringName));
            }

            this.cryptDictionary = cryptDictionary ?? throw new ArgumentNullException(nameof(cryptDictionary));            
            StreamDictionary = ParseCryptDictionary(cryptDictionary, streamName);
            StringDictionary = ParseCryptDictionary(cryptDictionary, stringName);
        }

        public CryptDictionary GetNamedCryptDictionary(NameToken name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            return ParseCryptDictionary(cryptDictionary, name);
        }

        private static CryptDictionary ParseCryptDictionary(DictionaryToken cryptDictionary, NameToken name)
        {
            if (name == NameToken.Identity)
            {
                return CryptDictionary.Identity;
            }

            if (!cryptDictionary.TryGet(name, out DictionaryToken cryptDictionaryToken))
            {
                throw new PdfDocumentEncryptedException($"Could not find named crypt filter {name} for decryption in crypt dictionary: {cryptDictionaryToken}.");
            }

            if (cryptDictionaryToken.TryGet(NameToken.Type, out NameToken typeName) && typeName != NameToken.CryptFilter)
            {
                throw new PdfDocumentEncryptedException($"Invalid crypt dictionary type {typeName} for crypt filter {name}: {cryptDictionaryToken}.");
            }

            var cfmName = cryptDictionaryToken.TryGet(NameToken.Cfm, out NameToken cfm) ? cfm : NameToken.None;

            CryptDictionary.Method method;
            if (cfmName == NameToken.None)
            {
                method = CryptDictionary.Method.None;
            }
            else if (cfmName == NameToken.V2)
            {
                method = CryptDictionary.Method.V2;
            }
            else if (cfmName == NameToken.Aesv2)
            {
                method = CryptDictionary.Method.AesV2;
            }
            else if (cfmName == NameToken.Aesv3)
            {
                method = CryptDictionary.Method.AesV3;
            }
            else
            {
                throw new PdfDocumentEncryptedException($"Unrecognized CFM option for crypt filter {cfm}: {cryptDictionaryToken}.");
            }

            var eventName = cryptDictionaryToken.TryGet(NameToken.AuthEvent, out NameToken auth) ? auth : NameToken.DocOpen;

            CryptDictionary.TriggerEvent @event;
            if (eventName == NameToken.DocOpen)
            {
                @event = CryptDictionary.TriggerEvent.DocumentOpen;
            }
            else if (eventName == NameToken.EfOpen)
            {
                @event = CryptDictionary.TriggerEvent.EmbeddedFileOpen;
            }
            else
            {
                throw new PdfDocumentEncryptedException($"Unrecognized AuthEvent option for crypt filter {eventName}: {cryptDictionaryToken}.");
            }

            var length = cryptDictionaryToken.TryGet(NameToken.Length, out NumericToken lengthNumeric) ? lengthNumeric.Int : 0;

            return new CryptDictionary(method, @event, length);
        }
    }
}
