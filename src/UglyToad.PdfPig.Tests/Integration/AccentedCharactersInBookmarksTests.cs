namespace UglyToad.PdfPig.Tests.Integration;

public class AccentedCharactersInBookmarksTests
{
    [Fact]
    public void CanReadAccentedBookmarksCorrectly()
    {
        var path = IntegrationHelpers.GetDocumentPath("bookmarks-with-accented-characters.pdf");

        using var document = PdfDocument.Open(path);

        var isFound = document.TryGetBookmarks(out var bookmarks);

        Assert.True(isFound);

        var nodes = bookmarks.GetNodes().Select(x => x.Title).ToList();

        Assert.Equal(new[]
            {
                "ž",
                "žč",
                "žđ",
                "žć",
                "žš",
                "ž ajklyghvbnmxcseqwuioprtzdf",
                "š",
                "šč",
                "šđ",
                "šć",
                "šž",
                "š ajklyghvbnmxcseqwuioprtzdf"
            },
            nodes);
    }

    [Fact]
    public void CanReadContainerBookmarksCorrectly()
    {
        var path = IntegrationHelpers.GetDocumentPath("dotnet-ai.pdf");

        using var document = PdfDocument.Open(path);
        var isFound = document.TryGetBookmarks(out var bookmarks, false);
        Assert.True(isFound);
        Assert.True(bookmarks.Roots.Count == 3);
        isFound = document.TryGetBookmarks(out bookmarks, true);
        Assert.True(isFound);
        Assert.True(bookmarks.Roots.Count > 3);
    }
}