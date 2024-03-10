namespace UglyToad.PdfPig.Tests.Integration
{
    using PdfPig.Core;
    using PdfPig.Tokens;
    using Xunit;

    public class DocumentInformationTests
    {
        [Fact]
        public void CanReadDocumentInformation()
        {
            var path = IntegrationHelpers.GetSpecificTestDocumentPath("custom-properties.pdf");

            using (var document = PdfDocument.Open(path))
            {
                var information = document.Information;

                Assert.Equal("Writer", information.Creator);
                Assert.Equal("MoreKeywords", information.Keywords);
                Assert.Equal("LibreOffice 6.1", information.Producer);
                Assert.Equal("TestSubject", information.Subject);
                Assert.Equal("TestTitle", information.Title);

                var infoDictionary = information.DocumentInformationDictionary;

                var nameToken = NameToken.Create("CustomProperty1");
                Assert.True(infoDictionary.TryGet(nameToken, out var valueToken), "first custom property must be present");
                Assert.IsType<StringToken>(valueToken);
                Assert.Equal("Property Value", ((StringToken)valueToken).Data);

                nameToken = NameToken.Create("CustomProperty2");
                Assert.True(infoDictionary.TryGet(nameToken, out var valueToken2), "second custom property must be present");
                Assert.IsType<StringToken>(valueToken2);
                Assert.Equal("Another Property Value", ((StringToken)valueToken2).Data);
            }
        }

        [Fact]
        public void CanReadInvalidDocumentInformation()
        {
            var path = IntegrationHelpers.GetSpecificTestDocumentPath("invalid-pdf-structure-pdfminer-entire-doc.pdf");

            /*
                <<
                /Producer (pdfTeX-1.40.21)
                 Collaborative Neural Rendering Using Anime Character Sheets /Author()/Title()/Subject()/Creator(LaTeX with hyperref)/Keywords()
                /CreationDate (D:20230418010134Z)
                /ModDate (D:20230418010134Z)
                /Trapped /False
                /PTEX.Fullbanner (This is pdfTeX, Version 3.14159265-2.6-1.40.21 (TeX Live 2020) kpathsea version 6.3.2)
                >>
             */

            // Lenient Parsing On -> can process
            using (var document = PdfDocument.Open(path))
            {
                var information = document.Information;

                Assert.Equal("LaTeX with hyperref", information.Creator);
                Assert.Equal("", information.Keywords);
                Assert.Equal("pdfTeX-1.40.21", information.Producer);
                Assert.Equal("", information.Subject);
                Assert.Equal("", information.Title);
                Assert.Equal("", information.Author);
                Assert.Equal("D:20230418010134Z", information.CreationDate);
                Assert.Equal("D:20230418010134Z", information.ModifiedDate);

                var infoDictionary = information.DocumentInformationDictionary;

                var nameToken = NameToken.Create("Trapped");
                Assert.True(infoDictionary.TryGet(nameToken, out var valueToken));
                Assert.IsType<NameToken>(valueToken);
                Assert.Equal("False", ((NameToken)valueToken).Data);

                nameToken = NameToken.Create("PTEX.Fullbanner");
                Assert.True(infoDictionary.TryGet(nameToken, out var valueToken2));
                Assert.IsType<StringToken>(valueToken2);
                Assert.Equal("This is pdfTeX, Version 3.14159265-2.6-1.40.21 (TeX Live 2020) kpathsea version 6.3.2", ((StringToken)valueToken2).Data);
            }

            // Lenient Parsing Off -> throws
            var ex = Assert.Throws<PdfDocumentFormatException>(() => PdfDocument.Open(path, ParsingOptions.LenientParsingOff));
            Assert.Equal("Expected name as dictionary key, instead got: Collaborative", ex.Message);
        }
    }
}
