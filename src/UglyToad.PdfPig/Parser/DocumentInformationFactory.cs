namespace UglyToad.PdfPig.Parser
{
    using Content;
    using CrossReference;
    using Parts;
    using Tokenization.Scanner;
    using Tokens;

    /// <summary>
    /// Parse the dictionary from a PDF file trailer.
    /// </summary>
    internal static class DocumentInformationFactory
    {
        /// <summary>
        /// Convert the file trailer dictionary into a <see cref="DocumentInformation"/> instance.
        /// </summary>
        public static DocumentInformation Create(IPdfTokenScanner pdfTokenScanner, TrailerDictionary trailer)
        {
            if (!trailer.Info.HasValue)
            {
                return DocumentInformation.Default;
            }

            var infoParsed = DirectObjectFinder.Get<DictionaryToken>(trailer.Info.Value, pdfTokenScanner);

            var title = GetEntryOrDefault(infoParsed, NameToken.Title);
            var author = GetEntryOrDefault(infoParsed, NameToken.Author);
            var subject = GetEntryOrDefault(infoParsed, NameToken.Subject);
            var keywords = GetEntryOrDefault(infoParsed, NameToken.Keywords);
            var creator = GetEntryOrDefault(infoParsed, NameToken.Creator);
            var producer = GetEntryOrDefault(infoParsed, NameToken.Producer);
            var creationDate = GetEntryOrDefault(infoParsed, NameToken.CreationDate);
            var modifiedDate = GetEntryOrDefault(infoParsed, NameToken.ModDate);

            return new DocumentInformation(infoParsed, title, author, subject,
                keywords, creator, producer, creationDate, modifiedDate);
        }

        private static string GetEntryOrDefault(DictionaryToken infoDictionary, NameToken key)
        {
            if (!infoDictionary.TryGet(key, out var value))
            {
                return null;
            }

            if (value is StringToken str)
            {
                return str.Data;
            }

            if (value is HexToken hex)
            {
                return hex.Data;
            }

            return null;
        }
    }
}
