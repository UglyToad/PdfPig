namespace UglyToad.Examples
{
    using System;
    using System.IO;
    using System.Linq;
    using PdfPig.Content;
    using PdfPig.Core;
    using PdfPig.Writer;

    internal static class GeneratePdfA2AFile
    {
        public static void Run(string trueTypeFontPath, string jpgImagePath)
        {
            var builder = new PdfDocumentBuilder
            {
                ArchiveStandard = PdfAStandard.A2A
            };

            var font = builder.AddTrueTypeFont(File.ReadAllBytes(trueTypeFontPath));

            var page = builder.AddPage(PageSize.A4);
            var pageTop = new PdfPoint(0, page.PageSize.Top);

            var letters = page.AddText("This is some text added to the output file near the top of the page.",
                12,
                pageTop.Translate(20, -25),
                font);

            var bottomOfText = letters.Min(x => x.GlyphRectangle.Bottom);

            var imagePlacement = new PdfRectangle(new PdfPoint(50, bottomOfText - 200), 
                new PdfPoint(150, bottomOfText));
            page.AddJpeg(File.ReadAllBytes(jpgImagePath), imagePlacement);

            var fileBytes = builder.Build();

            try
            {
                var location = AppDomain.CurrentDomain.BaseDirectory;
                var output = Path.Combine(location, "outputOfPdfA2A.pdf");
                File.WriteAllBytes(output, fileBytes);
                Console.WriteLine($"File output to: {output}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to write output to file due to error: {ex}.");
            }
        }
    }
}