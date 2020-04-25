namespace UglyToad.Examples
{
    using System;
    using System.IO;
    using System.Linq;
    using PdfPig.Writer;

    internal static class MergePdfDocuments
    {
        public static void Run(string filePath1, string filePath2, string filePath3)
        {
            var fileBytes = new[] { filePath1, filePath2, filePath3 }
                .Select(File.ReadAllBytes).ToList();

            var resultFileBytes = PdfMerger.Merge(fileBytes);

            try
            {
                var location = AppDomain.CurrentDomain.BaseDirectory;
                var output = Path.Combine(location, "outputOfMerge.pdf");
                File.WriteAllBytes(output, resultFileBytes);
                Console.WriteLine($"File output to: {output}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to write output to file due to error: {ex}.");
            }
        }
    }
}
