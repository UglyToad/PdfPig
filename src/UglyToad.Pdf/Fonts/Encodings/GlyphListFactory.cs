namespace UglyToad.Pdf.Fonts.Encodings
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;

    internal class GlyphListFactory
    {
        public static GlyphList Get(string listName)
        {
            var result = new Dictionary<string, string>();

            using (var resource = typeof(GlyphListFactory).Assembly.GetManifestResourceStream($"UglyToad.Pdf.Resources.GlyphList.{listName}"))
            {
                if (resource == null)
                {
                    throw new ArgumentException($"No embedded glyph list resource was found with the name {listName}.");
                }

                using (var reader = new StreamReader(resource))
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

                        var parts = line.Split(new[] {';'});

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
                            var code = int.Parse(s, NumberStyles.HexNumber);

                            value += char.ConvertFromUtf32(code);
                        }

                        result[key] = value;
                    }
                }
            }

            return new GlyphList(result);
        }
    }
}
