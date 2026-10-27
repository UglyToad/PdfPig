namespace UglyToad.Examples
{
    using System;
    using System.IO;
    using System.Text;
    using PdfPig.Content;
    using PdfPig.Core;
    using PdfPig.Writer;

    internal static class GenerateDocumentWithEmbeddedFile
    {
        public static void Run(string trueTypeFontPath)
        {
            // PDF/A-3 is the PDF/A standard that allows any file to be embedded, such as the XML of an electronic invoice.
            var builder = new PdfDocumentBuilder
            {
                ArchiveStandard = PdfAStandard.A3B
            };

            var font = builder.AddTrueTypeFont(File.ReadAllBytes(trueTypeFontPath));
            var page = builder.AddPage(PageSize.A4);
            page.AddText("Invoice 2026-001", 12, new PdfPoint(25, page.PageSize.Top - 50), font);

            var invoice = Encoding.UTF8.GetBytes("<?xml version=\"1.0\" encoding=\"UTF-8\"?><Invoice><ID>2026-001</ID></Invoice>");
            builder.AddEmbeddedFile("invoice.xml", invoice, new PdfDocumentBuilder.EmbeddedFileOptions
            {
                MimeType = "text/xml",
                Description = "The invoice as XML",
                ModifiedDate = DateTimeOffset.Now,
                Relationship = AssociatedFileRelationship.Alternative
            });

            var fileBytes = builder.Build();

            try
            {
                var location = AppDomain.CurrentDomain.BaseDirectory;
                var output = Path.Combine(location, "outputWithEmbeddedFile.pdf");
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
