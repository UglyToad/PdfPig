namespace UglyToad.Examples
{
    using System;
    using PdfPig;
    using PdfPig.DocumentLayoutAnalysis.TextExtractor;

    internal static class ExtractTextWithNewlines
    {
        public static void Run(string filePath)
        {
            using (var document = PdfDocument.Open(filePath))
            {
                foreach (var page in document.GetPages())
                {
                    var text = ContentOrderTextExtractor.GetText(page, true);

                    Console.WriteLine(text);
                }
            }
        }
    }
}
