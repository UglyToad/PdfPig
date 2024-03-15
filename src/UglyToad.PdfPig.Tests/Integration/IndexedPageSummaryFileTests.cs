namespace UglyToad.PdfPig.Tests.Integration;

public class IndexedPageSummaryFileTests
{
    private static string GetFilename()
    {
        return IntegrationHelpers.GetDocumentPath("FICTIF_TABLE_INDEX.pdf");
    }

    [Fact]
    public void HasCorrectNumberOfPages()
    {
        using (var document = PdfDocument.Open(GetFilename()))
        {
            Assert.Equal(14, document.NumberOfPages);
        }
    }

    [Fact]
    public void GetPagesWorks()
    {
        using (var document = PdfDocument.Open(GetFilename()))
        {
            var pageCount = document.GetPages().Count();

            Assert.Equal(14, pageCount);
        }
    }

    [Theory]
    [InlineData("M. HERNANDEZ DANIEL", 1)]
    [InlineData("M. HERNANDEZ DANIEL", 2)]
    [InlineData("Mme ALIBERT CHLOE AA", 3)]
    [InlineData("Mme ALIBERT CHLOE AA", 4)]
    [InlineData("M. SIMPSON BART AAA", 5)]
    [InlineData("M. SIMPSON BART AAA", 6)]
    [InlineData("M. BOND JAMES A", 7)]
    [InlineData("M. BOND JAMES A", 8)]
    [InlineData("M. DE BALZAC HONORE", 9)]
    [InlineData("M. DE BALZAC HONORE", 10)]
    [InlineData("M. STALLONE SILVESTER", 11)]
    [InlineData("M. STALLONE SILVESTER", 12)]
    [InlineData("M. SCOTT MICHAEL", 13)]
    [InlineData("M. SCOTT MICHAEL", 14)]
    public void CheckSpecificNamesPresence_InIndexedPageNumbersFile(string searchedName, int pageNumber)
    {
        using var document = PdfDocument.Open(GetFilename());
        var page = document.GetPage(pageNumber);
        Assert.Contains(searchedName, page.Text);
    }
}