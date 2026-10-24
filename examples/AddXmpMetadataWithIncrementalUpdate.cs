namespace UglyToad.Examples
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using PdfPig;
    using PdfPig.Tokens;
    using PdfPig.Writer;

    internal static class AddXmpMetadataWithIncrementalUpdate
    {
        public static void Run(string filePath)
        {
            const string xmp =
                "<?xpacket begin=\"﻿\" id=\"W5M0MpCehiHzreSzNTczkc9d\"?>" +
                "<x:xmpmeta xmlns:x=\"adobe:ns:meta/\">" +
                "<rdf:RDF xmlns:rdf=\"http://www.w3.org/1999/02/22-rdf-syntax-ns#\">" +
                "<rdf:Description rdf:about=\"\" xmlns:dc=\"http://purl.org/dc/elements/1.1/\">" +
                "<dc:title><rdf:Alt><rdf:li xml:lang=\"x-default\">Added with an incremental update</rdf:li></rdf:Alt></dc:title>" +
                "</rdf:Description>" +
                "</rdf:RDF>" +
                "</x:xmpmeta>" +
                "<?xpacket end=\"w\"?>";

            using var document = PdfDocument.Open(filePath);

            // The original bytes stay unchanged, the update only appends the new and the replaced objects.
            var builder = new PdfIncrementalUpdateBuilder(document);

            var metadata = builder.AddObject(new StreamToken(new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                { NameToken.Type, NameToken.Metadata },
                { NameToken.Subtype, NameToken.Xml }
            }), Encoding.UTF8.GetBytes(xmp)));

            var catalog = document.Structure.Catalog.CatalogDictionary;
            builder.ReplaceObject(document.Structure.Trailer.Root, catalog.With(NameToken.Metadata, metadata));

            var fileBytes = builder.Build();

            try
            {
                var location = AppDomain.CurrentDomain.BaseDirectory;
                var output = Path.Combine(location, "outputWithIncrementalUpdate.pdf");
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
