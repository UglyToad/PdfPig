namespace UglyToad.PdfPig.Fonts
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text;
    using Encodings;
    using UglyToad.PdfPig.Util;

    /// <summary>
    /// A list which maps PostScript glyph names to unicode values.
    /// </summary>
    public class GlyphList
    {
        /// <summary>
        /// <c>.notdef</c> name.
        /// </summary>
        public const string NotDefined = ".notdef";

        private readonly IReadOnlyDictionary<string, string> nameToUnicode;
        private readonly IReadOnlyDictionary<string, string> unicodeToName;

        private readonly Dictionary<string, string> oddNameToUnicodeCache = new Dictionary<string, string>();

        private static readonly Lazy<GlyphList> LazyAdobeGlyphList = new Lazy<GlyphList>(() => GlyphListFactory.Get("glyphlist", "additional"));

        /// <summary>
        /// The Adobe Glyph List (includes an extension to the Adobe Glyph List.).
        /// </summary>
        public static GlyphList AdobeGlyphList => LazyAdobeGlyphList.Value;

        private static readonly Lazy<GlyphList> LazyZapfDingbatsGlyphList = new Lazy<GlyphList>(() => GlyphListFactory.Get("zapfdingbats"));
        
        /// <summary>
        /// Zapf Dingbats.
        /// </summary>
        public static GlyphList ZapfDingbats => LazyZapfDingbatsGlyphList.Value;

        internal GlyphList(IReadOnlyDictionary<string, string> namesToUnicode)
        {
            nameToUnicode = namesToUnicode;

            var unicodeToNameTemp = new Dictionary<string, string>(namesToUnicode.Count);

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
        /// See <see href="https://github.com/adobe-type-tools/agl-specification"/>.
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

            string? unicode;
            // 1. Drop all the characters from the glyph name starting with the first occurrence of a period (U+002E FULL STOP), if any.
            if (name.IndexOf('.') > 0)
            {
                unicode = NameToUnicode(name.Substring(0, name.IndexOf('.')));
            }
            // 2. Split the remaining string into a sequence of components, using underscore (U+005F LOW LINE) as the delimiter.
            else if (name.IndexOf('_') > 0)
            {
                /*
                 * MOZILLA-3136-0.pdf
                 * 68-1990-01_A.pdf
                 * TIKA-2054-0.pdf
                 */
                var sb = new StringBuilder();
                foreach (var s in name.Split('_'))
                {
                    sb.Append(NameToUnicode(s));
                }

                unicode = sb.ToString();
            }
            // Otherwise, if the component is of the form ‘uni’ (U+0075, U+006E, and U+0069) followed by a sequence of uppercase hexadecimal
            // digits (0–9 and A–F, meaning U+0030 through U+0039 and U+0041 through U+0046), if the length of that sequence is a multiple
            // of four, and if each group of four digits represents a value in the ranges 0000 through D7FF or E000 through FFFF, then
            // interpret each as a Unicode scalar value and map the component to the string made of those scalar values. Note that the range
            // and digit-length restrictions mean that the ‘uni’ glyph name prefix can be used only with UVs in the Basic Multilingual Plane (BMP).
            else if (name.StartsWith("uni") && (name.Length - 3) % 4 == 0)
            {
                // test for Unicode name in the format uniXXXX where X is hex
                int nameLength = name.Length;

                var uniStr = new StringBuilder();

                for (int chPos = 3; chPos + 4 <= nameLength; chPos += 4)
                {
                    if (!int.TryParse(name.AsSpanOrSubstring(chPos, 4),
                            NumberStyles.HexNumber,
                            CultureInfo.InvariantCulture,
                            out var codePoint))
                    {
                        return null;
                    }

                    if (codePoint > 0xD7FF && codePoint < 0xE000)
                    {
                        throw new InvalidFontFormatException($"Unicode character name with disallowed code area: {name}");
                    }

                    uniStr.Append((char)codePoint);
                }

                unicode = uniStr.ToString();
            }
            // Otherwise, if the component is of the form ‘u’ (U+0075) followed by a sequence of four to six uppercase hexadecimal digits (0–9
            // and A–F, meaning U+0030 through U+0039 and U+0041 through U+0046), and those digits represents a value in the ranges 0000 through
            // D7FF or E000 through 10FFFF, then interpret it as a Unicode scalar value and map the component to the string made of this scalar value.
            else if (name.StartsWith("u", StringComparison.Ordinal) && name.Length >= 5 && name.Length <= 7)
            {
                var codePoint = int.Parse(name.AsSpanOrSubstring(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture);

                if (codePoint > 0xD7FF && codePoint < 0xE000)
                {
                    throw new InvalidFontFormatException($"Unicode character name with disallowed code area: {name}");
                }

                unicode = char.ConvertFromUtf32(codePoint);
            }
            // Ad-hoc special cases
            else if (name.StartsWith("c", StringComparison.OrdinalIgnoreCase) && name.Length >= 3 && name.Length <= 4)
            {
                // name representation cXXX
                var codePoint = int.Parse(name.AsSpanOrSubstring(1), NumberStyles.Integer, CultureInfo.InvariantCulture);
                unicode = char.ConvertFromUtf32(codePoint);
            }
            // Otherwise, map the component to an empty string.
            else
            {
                return null;
            }

            oddNameToUnicodeCache[name] = unicode;

            return unicode;
        }
    }
}
