namespace UglyToad.PdfPig.Parser
{
    using Content;
    using CrossReference;
    using Parts;
    using Tokenization.Scanner;
    using Tokens;

    internal class DocumentInformationFactory
    {
        public DocumentInformation Create(IPdfTokenScanner pdfTokenScanner, TrailerDictionary trailer)
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

            return new DocumentInformation(infoParsed, title, author, subject,
                keywords, creator, producer);
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
