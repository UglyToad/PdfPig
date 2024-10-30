namespace UglyToad.PdfPig.Tests.Integration;

using Annotations;

public class AnnotationReplyToTests
{
    private static string GetFilename()
    {
        return IntegrationHelpers.GetDocumentPath("annotation-comments.pdf");
    }

    [Fact]
    public void HasCorrectNumberOfAnnotations()
    {
        using var document = PdfDocument.Open(GetFilename());

        var page = document.GetPage(1);

        var annotations = page.GetAnnotations().ToArray();

        Assert.Equal(4, annotations.Length);

        Assert.Equal(AnnotationType.Text, annotations[0].Type);
        Assert.Equal(AnnotationType.Popup, annotations[1].Type);
        Assert.Equal(AnnotationType.Text, annotations[2].Type);
        Assert.Equal(AnnotationType.Popup, annotations[3].Type);
    }

    [Fact]
    public void SecondTextReplyToFirst()
    {
        using var document = PdfDocument.Open(GetFilename());

        var page = document.GetPage(1);

        var annotations = page.GetAnnotations().ToArray();

        Assert.Equal(annotations[0], annotations[2].InReplyTo);
    }

}