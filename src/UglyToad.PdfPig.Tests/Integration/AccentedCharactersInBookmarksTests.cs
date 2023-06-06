namespace UglyToad.PdfPig.Tests.Integration;

using System.Linq;
using Xunit;

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
}