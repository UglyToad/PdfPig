namespace UglyToad.PdfPig.Fonts
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using Util;

    internal class GlyphListFactory
    {
        public static GlyphList Get(string listName)
        {
            using (var resource =
                typeof(GlyphListFactory).Assembly.GetManifestResourceStream(
                    $"UglyToad.PdfPig.Fonts.Resources.GlyphList.{listName}"))
            {
                if (resource == null)
                {
                    throw new ArgumentException($"No embedded glyph list resource was found with the name {listName}.");
                }

                int? capacity = null;
                // Prevent too much wasted memory capacity for Adobe GlyphList
                if (string.Equals("glyphlist", listName, StringComparison.OrdinalIgnoreCase))
                {
                    capacity = 4300;
                }

                return ReadInternal(resource, capacity);
            }
        }

        public static GlyphList Read(Stream stream)
        {
            return ReadInternal(stream);
        }

        private static readonly char[] Semicolon = [';'];

        private static GlyphList ReadInternal(Stream stream, int? defaultDictionaryCapacity = 0)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            var result = defaultDictionaryCapacity.HasValue ? new Dictionary<string, string>(defaultDictionaryCapacity.Value) : [];


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

                    var parts = line.Split(Semicolon, StringSplitOptions.RemoveEmptyEntries);

                    if (parts.Length != 2)
                    {
                        throw new InvalidOperationException(
                            $"The line in the glyph list did not match the expected format. Line was: {line}");
                    }

                    var key = parts[0];

                    var valueReader = new StringSplitter(parts[1].AsSpan(), ' ');
                    var value = string.Empty;

                    while (valueReader.TryRead(out var s))
                    {
#if NET6_0_OR_GREATER
                        var code = int.Parse(s, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
#else
                        var code = int.Parse(s.ToString(), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
#endif
                        value += char.ConvertFromUtf32(code);
                    }

                    result[key] = value;
                }
            }

            return new GlyphList(result);
        }
    }
}
