namespace UglyToad.PdfPig.PdfFonts.Encodings
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;

    internal class GlyphListFactory
    {
        public static GlyphList Get(string listName)
        {
            using (var resource =
                typeof(GlyphListFactory).Assembly.GetManifestResourceStream(
                    $"UglyToad.PdfPig.Resources.GlyphList.{listName}"))
            {
                if (resource == null)
                {
                    throw new ArgumentException($"No embedded glyph list resource was found with the name {listName}.");
                }


                return Read(resource);
            }
        }

        public static GlyphList Read(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            var result = new Dictionary<string, string>();

            using (var reader = new StreamReader(stream))
            {
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();

                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    if (line[0] == '#')
                    {
                        continue;
                    }

                    var parts = line.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

                    if (parts.Length != 2)
                    {
                        throw new InvalidOperationException(
                            $"The line in the glyph list did not match the expected format. Line was: {line}");
                    }

                    var key = parts[0];

                    var values = parts[1].Split(' ');

                    var value = string.Empty;
                    foreach (var s in values)
                    {
                        var code = int.Parse(s, NumberStyles.HexNumber, CultureInfo.InvariantCulture);

                        value += char.ConvertFromUtf32(code);
                    }

                    result[key] = value;
                }
            }

            return new GlyphList(result);
        }
    }
}
