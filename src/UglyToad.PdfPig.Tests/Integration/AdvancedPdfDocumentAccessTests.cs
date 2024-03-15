namespace UglyToad.PdfPig.Tests.Integration
{
    using PdfPig.Tokens;

    public class AdvancedPdfDocumentAccessTests
    {
        [Fact]
        public void ReplacesObjectsFunc()
        {
            var path = IntegrationHelpers.GetDocumentPath("Single Page Simple - from inkscape.pdf");

            using (var document = PdfDocument.Open(path))
            {
                var pg = document.Structure.Catalog.Pages.GetPageNode(1).NodeDictionary;
                var contents = pg.Data[NameToken.Contents] as IndirectReferenceToken;
                document.Advanced.ReplaceIndirectObject(contents.Data, tk =>
                {
                    var dict = new Dictionary<NameToken, IToken>();
                    dict[NameToken.Length] = new NumericToken(0);
                    var replaced = new StreamToken(new DictionaryToken(dict), new List<byte>());
                    return replaced;
                });

                var page = document.GetPage(1);
                Assert.Empty(page.Letters);
            }
        }

        [Fact]
        public void ReplacesObjects()
        {
            var path = IntegrationHelpers.GetDocumentPath("Single Page Simple - from inkscape.pdf");

            using (var document = PdfDocument.Open(path))
            {
                var dict = new Dictionary<NameToken, IToken>();
                dict[NameToken.Length] = new NumericToken(0);
                var replacement = new StreamToken(new DictionaryToken(dict), new List<byte>());

                var pg = document.Structure.Catalog.Pages.GetPageNode(1).NodeDictionary;
                var contents = pg.Data[NameToken.Contents] as IndirectReferenceToken;
                document.Advanced.ReplaceIndirectObject(contents.Data, replacement);

                var page = document.GetPage(1);
                Assert.Empty(page.Letters);
            }
        }
    }
}

