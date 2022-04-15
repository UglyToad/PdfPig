namespace UglyToad.PdfPig.Tests.Integration;

using Xunit;

public class Math119FakingDataTests
{
    [Fact]
    public void CombinesDiaeresisForWords()
    {
        using var document = PdfDocument.Open(IntegrationHelpers.GetDocumentPath("Math119FakingData.pdf"));

        var lastPage = document.GetPage(8);

        var words = lastPage.GetWords();


    }
}