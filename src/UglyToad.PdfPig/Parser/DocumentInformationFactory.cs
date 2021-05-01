namespace UglyToad.PdfPig.Parser
{
    using Content;
    using CrossReference;
    using Parts;
    using Tokenization.Scanner;
    using Tokens;
    using UglyToad.PdfPig.Core;

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

            var token = DirectObjectFinder.Get<IToken>(trailer.Info.Value, pdfTokenScanner);
            if (token is DictionaryToken infoParsed)
            {
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
            else if (token is StreamToken streamToken)
            {
                var streamDictionary = streamToken.StreamDictionary;
                if (!streamDictionary.TryGet(NameToken.Type, out NameToken typeNameToken) || typeNameToken != "Metadata")
                {
                    throw new PdfDocumentFormatException($"Unknown document metadata type was found");
                }

                if (!streamDictionary.TryGet(NameToken.Subtype, out NameToken subtypeToken) || subtypeToken != "XML")
                {
                    throw new PdfDocumentFormatException($"Unknown document metadata subtype was found");
                }

                // We are not fully supporting XMP Stream so we left the user fully deserialize the stream
                return DocumentInformation.Default;
            }
            else
            {
                throw new PdfDocumentFormatException($"Unknown document information token was found {token.GetType().Name}");
            }
        }

        private static string GetEntryOrDefault(DictionaryToken infoDictionary, NameToken key)
        {
            if (infoDictionary == null)
            {
                return null;
            }

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
