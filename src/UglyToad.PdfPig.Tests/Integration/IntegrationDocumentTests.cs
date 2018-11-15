namespace UglyToad.PdfPig.Tests.Integration
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Xunit;

    public class IntegrationDocumentTests
    {
        [Theory]
        [MemberData(nameof(GetAllDocuments))]
        public void CanReadAllPages(string documentName)
        {
            using (var document = PdfDocument.Open(documentName, new ParsingOptions{ UseLenientParsing = false}))
            {
                for (var i = 0; i < document.NumberOfPages; i++)
                {
                    document.GetPage(i + 1);
                }
            }
        }

        public static IEnumerable<object[]> GetAllDocuments
        {
            get
            {
                var documentFolder = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Integration", "Documents"));

                var files = Directory.GetFiles(documentFolder, "*.pdf");

                return files.Select(x => new object[] {x});
            }
        }
    }
}
