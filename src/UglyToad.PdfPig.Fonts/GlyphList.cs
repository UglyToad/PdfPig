namespace UglyToad.PdfPig.Fonts
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text;
    using Encodings;

    /// <summary>
    /// A list which maps PostScript glyph names to unicode values.
    /// </summary>
    public class GlyphList
    {
        private const string NotDefined = ".notdef";

        private readonly IReadOnlyDictionary<string, string> nameToUnicode;
        private readonly IReadOnlyDictionary<string, string> unicodeToName;

        private readonly Dictionary<string, string> oddNameToUnicodeCache = new Dictionary<string, string>();

        private static readonly Lazy<GlyphList> LazyAdobeGlyphList = new Lazy<GlyphList>(() => GlyphListFactory.Get("glyphlist"));

        /// <summary>
        /// The Adobe Glyph List.
        /// </summary>
        public static GlyphList AdobeGlyphList => LazyAdobeGlyphList.Value;

        private static readonly Lazy<GlyphList> LazyAdditionalGlyphList = new Lazy<GlyphList>(() => GlyphListFactory.Get("additional"));

        /// <summary>
        /// An extension to the Adobe Glyph List.
        /// </summary>
        public static GlyphList AdditionalGlyphList => LazyAdditionalGlyphList.Value;

        private static readonly Lazy<GlyphList> LazyZapfDingbatsGlyphList = new Lazy<GlyphList>(() => GlyphListFactory.Get("zapfdingbats"));

        /// <summary>
        /// Zapf Dingbats.
        /// </summary>
        public static GlyphList ZapfDingbats => LazyZapfDingbatsGlyphList.Value;

        internal GlyphList(IReadOnlyDictionary<string, string> namesToUnicode)
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

        /// <summary>
        /// Get the name for the unicode code point value.
        /// </summary>
        public string UnicodeCodePointToName(int unicodeValue)
        {
            var value = char.ConvertFromUtf32(unicodeValue);

            if (unicodeToName.TryGetValue(value, out var result))
            {
                return result;
            }

            return NotDefined;
        }

        /// <summary>
        /// Get the unicode value for the glyph name.
        /// </summary>
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

                var foundUnicode = true;
                for (int chPos = 3; chPos + 4 <= nameLength; chPos += 4)
                {
                    if (!int.TryParse(name.Substring(chPos, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var codePoint))
                    {
                        foundUnicode = false;
                        break;
                    }

                    if (codePoint > 0xD7FF && codePoint < 0xE000)
                    {
                        throw new InvalidFontFormatException($"Unicode character name with disallowed code area: {name}");
                    }

                    uniStr.Append((char)codePoint);
                }

                if (!foundUnicode)
                {
                    return null;
                }

                unicode = uniStr.ToString();
            }
            else if (name.StartsWith("u") && name.Length == 5)
            {
                // test for an alternate Unicode name representation uXXXX
                var codePoint = int.Parse(name.Substring(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture);

                if (codePoint > 0xD7FF && codePoint < 0xE000)
                {
                    throw new InvalidFontFormatException(
                        $"Unicode character name with disallowed code area: {name}");
                }

                unicode = char.ConvertFromUtf32(codePoint);
            }
            else
            {
                return null;
            }

            oddNameToUnicodeCache[name] = unicode;

            return unicode;
        }
    }
}
