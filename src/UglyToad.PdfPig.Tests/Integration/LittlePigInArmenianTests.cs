namespace UglyToad.PdfPig.Tests.Integration;

using System.Linq;
using Xunit;

public class LittlePigInArmenianTests
{
    [Fact]
    public void CanReadTextCorrectly()
    {
        var path = IntegrationHelpers.GetDocumentPath("little-pig-in-armenian.pdf");

        using var document = PdfDocument.Open(path);

        var page = document.GetPage(1);

        var words = page.GetWords().ToList();

        var textFromWords = string.Join(" ", words.Select(x => x.Text));

        Assert.Equal("փոքրիկ խոզ", textFromWords);
    }
}