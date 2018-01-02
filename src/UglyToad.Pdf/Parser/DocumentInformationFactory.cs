namespace UglyToad.Pdf.Parser
{
    using Content;
    using ContentStream;
    using Cos;
    using IO;

    internal class DocumentInformationFactory
    {
        public DocumentInformation Create(IPdfObjectParser pdfObjectParser,
            PdfDictionary rootDictionary, IRandomAccessRead reader, 
            bool isLenientParsing)
        {
            if (!rootDictionary.TryGetItemOfType(CosName.INFO, out CosObject infoBase))
            {
                return DocumentInformation.Default;
            }

            var infoParsed = pdfObjectParser.Parse(infoBase.ToIndirectReference(), reader, isLenientParsing);

            if (!(infoParsed is PdfDictionary infoDictionary))
            {
                return DocumentInformation.Default;
            }

            var title = GetEntryOrDefault(infoDictionary, CosName.TITLE);
            var author = GetEntryOrDefault(infoDictionary, CosName.AUTHOR);
            var subject = GetEntryOrDefault(infoDictionary, CosName.SUBJECT);
            var keywords = GetEntryOrDefault(infoDictionary, CosName.KEYWORDS);
            var creator = GetEntryOrDefault(infoDictionary, CosName.CREATOR);
            var producer = GetEntryOrDefault(infoDictionary, CosName.PRODUCER);

            return new DocumentInformation(title, author, subject,
                keywords, creator, producer);
        }

        private static string GetEntryOrDefault(PdfDictionary infoDictionary, CosName key)
        {
            if (infoDictionary.TryGetItemOfType(key, out CosString str))
            {
                return str.GetAscii();
            }

            return null;
        }
    }
}
