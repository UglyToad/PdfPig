namespace UglyToad.Examples
{
    using System;
    using System.IO;
    using System.Text;
    using PdfPig;
    using PdfPig.Writer;

    internal static class AddEmbeddedFileWithIncrementalUpdate
    {
        public static void Run(string filePath)
        {
            using var document = PdfDocument.Open(filePath);

            // The original bytes stay unchanged, the update only appends the embedded file and the changed objects.
            var builder = new PdfIncrementalUpdateBuilder(document);

            // The document must not have a file of the same name yet, otherwise remove it first with RemoveEmbeddedFile.
            builder.AddEmbeddedFile("result.json", Encoding.UTF8.GetBytes("{\"total\": 119.00}"), new EmbeddedFileOptions
            {
                MimeType = "application/json",
                Description = "The result of the analysis"
            });

            var fileBytes = builder.Build();

            try
            {
                var location = AppDomain.CurrentDomain.BaseDirectory;
                var output = Path.Combine(location, "outputWithIncrementalEmbeddedFile.pdf");
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
