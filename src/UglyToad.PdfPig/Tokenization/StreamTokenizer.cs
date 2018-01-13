namespace UglyToad.PdfPig.Tokenization
{
    using Cos;
    using Exceptions;
    using IO;
    using Tokens;

    internal class StreamTokenizer
    {
        public object Tokenize(DictionaryToken streamDictionary, IInputBytes inputBytes)
        {
            if (!streamDictionary.TryGetByName(CosName.LENGTH, out var lengthToken))
            {
                throw new PdfDocumentFormatException("The stream dictionary did not define a length: " + streamDictionary);
            }

            return null;
        }
    }
}
