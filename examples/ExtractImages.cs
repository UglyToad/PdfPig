namespace UglyToad.Examples
{
    using System;
    using PdfPig;
    using PdfPig.Content;
    using PdfPig.XObjects;

    internal static class ExtractImages
    {
        public static void Run(string filePath)
        {
            using (var document = PdfDocument.Open(filePath))
            {
                foreach (var page in document.GetPages())
                {
                    foreach (var image in page.GetImages())
                    {
                        if (!image.TryGetBytesAsMemory(out var b))
                        {
                            b = image.RawMemory;
                        }

                        var type = string.Empty;
                        switch (image)
                        {
                            case XObjectImage ximg:
                                type = "XObject";
                                break;
                            case InlineImage inline:
                                type = "Inline";
                                break;
                        }

                        Console.WriteLine($"Image with {b.Length} bytes of type '{type}' on page {page.Number}. Location: {image.Bounds}.");
                    }
                }
            }
        }
    }
}
