namespace UglyToad.Examples
{
    using System;
    using System.Linq;
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
                        switch (image)
                        {
                            case XObjectImage ximg:
                                byte[] b;
                                try
                                {
                                    b = ximg.Bytes.ToArray();
                                }
                                catch
                                {
                                    b = ximg.RawBytes.ToArray();
                                }

                                Console.WriteLine($"Image with {b.Length} bytes and dictionary {ximg.ImageDictionary}.");
                                break;
                            case InlineImage inline:
                                Console.WriteLine($"Inline image: {inline.RawBytes.Count} bytes.");
                                break;
                        }
                    }
                }
            }
        }
    }
}
