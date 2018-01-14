namespace UglyToad.PdfPig.Tokenization.Tokens
{
    internal class StreamToken : IDataToken<byte[]>
    {
        public DictionaryToken StreamDictionary { get; }

        public byte[] Data { get; }

        public StreamToken(DictionaryToken streamDictionary, byte[] data)
        {
            StreamDictionary = streamDictionary;
            Data = data;
        }
    }
}
