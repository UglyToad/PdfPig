namespace UglyToad.Pdf.Fonts.Encodings
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text;
    using Exceptions;

    internal class GlyphList
    {
        private const string NotDefined = ".notdef";

        private readonly IReadOnlyDictionary<string, string> nameToUnicode;
        private readonly IReadOnlyDictionary<string, string> unicodeToName;
        
        private readonly Dictionary<string, string> oddNameToUnicodeCache = new Dictionary<string, string>();

        private static readonly Lazy<GlyphList> LazyAdobeGlyphList = new Lazy<GlyphList>(() => GlyphListFactory.Get("glyphlist"));
        public static GlyphList AdobeGlyphList => LazyAdobeGlyphList.Value;

        private static readonly Lazy<GlyphList> LazyZapfDingbatsGlyphList = new Lazy<GlyphList>(() => GlyphListFactory.Get("zapfdingbats"));
        public static GlyphList ZapfDingbats => LazyZapfDingbatsGlyphList.Value;

        public GlyphList(IReadOnlyDictionary<string, string> namesToUnicode)
        {
            nameToUnicode = namesToUnicode;

            var unicodeToNameTemp = new Dictionary<string, string>();

            foreach (var pair in namesToUnicode)
            {
                var forceOverride =
                    WinAnsiEncoding.Instance.ContainsName(pair.Key) ||
                    MacRomanEncoding.Instance.ContainsName(pair.Key) ||
                    MacExpertEncoding.Instance.ContainsName(pair.Key) ||
                    SymbolEncoding.Instance.ContainsName(pair.Key) ||
                    ZapfDingbatsEncoding.Instance.ContainsName(pair.Key);

                if (!unicodeToNameTemp.ContainsKey(pair.Value) || forceOverride)
                {
                    unicodeToNameTemp[pair.Value] = pair.Key;
                }
            }

            unicodeToName = unicodeToNameTemp;
        }

        public string UnicodeCodePointToName(int unicodeValue)
        {
            var value = char.ConvertFromUtf32(unicodeValue);

            if (unicodeToName.TryGetValue(value, out var result))
            {
                return result;
            }

            return NotDefined;
        }

        public string NameToUnicode(string name)
        {
            if (name == null)
            {
                return null;
            }

            if (nameToUnicode.TryGetValue(name, out var unicodeValue))
            {
                return unicodeValue;
            }

            if (oddNameToUnicodeCache.TryGetValue(name, out var result))
            {
                return result;
            }

            string unicode;
            // Remove suffixes
            if (name.IndexOf('.') > 0)
            {
                unicode = NameToUnicode(name.Substring(0, name.IndexOf('.')));
            }
            else if (name.StartsWith("uni") && name.Length == 7)
            {
                // test for Unicode name in the format uniXXXX where X is hex
                int nameLength = name.Length;

                var uniStr = new StringBuilder();

                for (int chPos = 3; chPos + 4 <= nameLength; chPos += 4)
                {
                    int codePoint = int.Parse(name.Substring(chPos, chPos + 4), NumberStyles.HexNumber);

                    if (codePoint > 0xD7FF && codePoint < 0xE000)
                    {
                        throw new InvalidFontFormatException(
                            $"Unicode character name with disallowed code area: {name}");
                    }

                    uniStr.Append((char)codePoint);
                }

                unicode = uniStr.ToString();
            }
            else if (name.StartsWith("u") && name.Length == 5)
            {
                // test for an alternate Unicode name representation uXXXX
                    int codePoint = int.Parse(name.Substring(1), NumberStyles.HexNumber);

                    if (codePoint > 0xD7FF && codePoint < 0xE000)
                {
                    throw new InvalidFontFormatException(
                        $"Unicode character name with disallowed code area: {name}");
                }

                unicode = char.ConvertFromUtf32(codePoint);
            }
            else
            {
                throw new InvalidFontFormatException($"Could not find the unicode glyph for the name {name}.");
            }

            oddNameToUnicodeCache[name] = unicode;

            return unicode;
        }
    }
}

