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
            var file = @"D:\temp\digitalcorpora\0001\0001849.pdf";
            try
            {
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
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error parsing: {Path.GetFileName(file)}.", ex);
            }
        }
    }
}