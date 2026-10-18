namespace UglyToad.Examples
{
    using System;
    using System.IO;
    using System.Linq;
    using PdfPig.Content;
    using PdfPig.Core;
    using PdfPig.Writer;

    internal static class GenerateDocumentWithCompositeFont
    {
        public static void Run(string trueTypeFontPath)
        {
            var builder = new PdfDocumentBuilder();

            // A composite font uses 2-byte character codes, so the text is not limited to 255 distinct characters.
            var font = builder.AddCompositeTrueTypeFont(File.ReadAllBytes(trueTypeFontPath));

            var page = builder.AddPage(PageSize.A4);
            var position = new PdfPoint(25, page.PageSize.Top - 50);

            page.AddText("Grüße, Привет, Γεια σου!", 12, position, font);

            // The 318 printable characters of Basic Latin, Latin-1 Supplement and Latin Extended-A.
            var characters = new string(Enumerable.Range(0x21, 94)
                .Concat(Enumerable.Range(0xA0, 224))
                .Select(x => (char)x)
                .ToArray());

            for (var i = 0; i < characters.Length; i += 53)
            {
                position = position.Translate(0, -20);
                page.AddText(characters.Substring(i, 53), 12, position, font);
            }

            var fileBytes = builder.Build();

            try
            {
                var location = AppDomain.CurrentDomain.BaseDirectory;
                var output = Path.Combine(location, "outputWithCompositeFont.pdf");
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