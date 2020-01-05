namespace UglyToad.PdfPig.Fonts.Encodings
{
    using System.Collections.Generic;
    using Tokens;

    /// <summary>
    /// Maps character codes to glyph names from a PostScript encoding.
    /// </summary>
    public abstract class Encoding
    {
        /// <summary>
        /// Mutable code to name map.
        /// </summary>
        protected readonly Dictionary<int, string> CodeToName = new Dictionary<int, string>(250);

        /// <summary>
        /// Maps from character codes to names.
        /// </summary>
        public IReadOnlyDictionary<int, string> CodeToNameMap => CodeToName;

        /// <summary>
        /// Mutable name to code map.
        /// </summary>
        protected readonly Dictionary<string, int> NameToCode = new Dictionary<string, int>(250);

        /// <summary>
        /// Maps from names to character cocdes.
        /// </summary>
        public IReadOnlyDictionary<string, int> NameToCodeMap => NameToCode;

        /// <summary>
        /// The name of this encoding.
        /// </summary>
        public abstract string EncodingName { get; }

        /// <summary>
        /// Whether this encoding contains a code for the name.
        /// </summary>
        public bool ContainsName(string name)
        {
            return NameToCode.ContainsKey(name);
        }

        /// <summary>
        /// Whether this encoding contains a name for the code.
        /// </summary>
        public bool ContainsCode(int code)
        {
            return CodeToName.ContainsKey(code);
        }
        
        /// <summary>
        /// Get the character name corresponding to the given code.
        /// </summary>
        public virtual string GetName(int code)
        {
            if (!CodeToName.TryGetValue(code, out var name))
            {
                return ".notdef";
            }

            return name;
        }

        /// <summary>
        /// Add a character code and name pair.
        /// </summary>
        protected void Add(int code, string name)
        {
            CodeToName[code] = name;

            if (!NameToCode.ContainsKey(name))
            {
                NameToCode[name] = code;
            }
        }
        
        /// <summary>
        /// Get a known encoding instance with the given name.
        /// </summary>
        public static bool TryGetNamedEncoding(NameToken name, out Encoding encoding)
        {
            encoding = null;

            if (name == null)
            {
                return false;
            }

            if (name.Equals(NameToken.StandardEncoding))
            {
                encoding = StandardEncoding.Instance;
                return true;
            }

            if (name.Equals(NameToken.WinAnsiEncoding))
            {
                encoding = WinAnsiEncoding.Instance;
                return true;
            }

            if (name.Equals(NameToken.MacExpertEncoding))
            {
                encoding = MacExpertEncoding.Instance;
                return true;
            }

            if (name.Equals(NameToken.MacRomanEncoding))
            {
                encoding = MacRomanEncoding.Instance;
                return true;
            }

            return false;
        }
    }
}
