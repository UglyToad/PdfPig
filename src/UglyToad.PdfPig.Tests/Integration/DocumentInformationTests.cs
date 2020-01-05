namespace UglyToad.PdfPig.Tests.Integration
{
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
    }
}
