namespace UglyToad.PdfPig.Fonts
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using Util;

    internal static class GlyphListFactory
    {
#if NET
        private const char Semicolon = ';';
#else
        private static readonly char[] Semicolon = [';'];
#endif

        public static GlyphList Get(params string[] listNames)
        {
            var result = new Dictionary<string, string>(listNames.Any(n => string.Equals("glyphlist", n, StringComparison.OrdinalIgnoreCase)) ? 4300 : 0);

            foreach (var listName in listNames)
            {
                using (var resource =
                       typeof(GlyphListFactory).Assembly.GetManifestResourceStream(
                           $"UglyToad.PdfPig.Fonts.Resources.GlyphList.{listName}"))
                {
                    if (resource == null)
                    {
                        throw new ArgumentException($"No embedded glyph list resource was found with the name {listName}.");
                    }

                    ReadInternal(resource, result);
                }
            }

#if NET
            result.TrimExcess();
#endif
            return new GlyphList(result);
        }

        public static GlyphList Read(Stream stream)
        {
            var result = new Dictionary<string, string>();
            ReadInternal(stream, result);
            return new GlyphList(result);
        }

        private static void ReadInternal(Stream stream, Dictionary<string, string> result)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

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

                    System.Diagnostics.Debug.Assert(!result.ContainsKey(key));
                    result[key] = value;
                }
            }
        }
    }
}
