namespace UglyToad.PdfPig.Tests.Parser.FileStructure;

using Integration;
using PdfPig.CrossReference;

/// <summary>
/// Regression test for https://github.com/UglyToad/PdfPig/issues/1243 - cross reference streams were always
/// decoded with the <see cref="PdfPig.Filters.DefaultFilterProvider"/>, ignoring
/// <see cref="ParsingOptions.FilterProvider"/>.
/// </summary>
public class XrefStreamFilterProviderTests
{
    private const string DocumentWithXrefStream = "bookmarks-with-accented-characters.pdf";

    [Fact]
    public void XrefStreamIsDecodedWithTheFilterProviderFromParsingOptions()
    {
        var original = File.ReadAllBytes(IntegrationHelpers.GetDocumentPath(DocumentWithXrefStream));

        using (var expected = PdfDocument.Open(original))
        {
            Assert.All(expected.Structure.CrossReferenceTable.Parts, x => Assert.Equal(CrossReferenceType.Stream, x.Type));

            var input = CustomNameFilterProvider.ReplaceFlateDecodeName(original);

            var provider = CustomNameFilterProvider.Create();

            using (var actual = PdfDocument.Open(input, new ParsingOptions { FilterProvider = provider }))
            {
                Assert.NotEmpty(actual.Structure.CrossReferenceTable.Parts);
                Assert.All(actual.Structure.CrossReferenceTable.Parts, x => Assert.Equal(CrossReferenceType.Stream, x.Type));
                Assert.Equal(expected.NumberOfPages, actual.NumberOfPages);

                for (var i = 1; i <= expected.NumberOfPages; i++)
                {
                    Assert.Equal(expected.GetPage(i).Text, actual.GetPage(i).Text);
                }
            }
        }
    }
}
