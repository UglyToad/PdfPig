namespace UglyToad.PdfPig.Tests.Integration
{
    using System.Diagnostics;

    /// <summary>
    /// A class for testing files which are not checked in to source control.
    /// </summary>
    public class LocalTests
    {
        [Fact]
        public void Tests()
        {
            var file = File.ReadAllBytes(@"D:\temp\200708170550023.pdf");
            using (var document = PdfDocument.Open(file, new ParsingOptions { UseLenientParsing = false }))
            {
                for (var i = 1; i <= document.NumberOfPages; i++)
                {
                    var page = document.GetPage(i);
                    var text = page.Text;
                    Trace.WriteLine(text);
                }
            }
        }
    }
}