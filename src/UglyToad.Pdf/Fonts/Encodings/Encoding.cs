namespace UglyToad.Pdf.Fonts.Encodings
{
    using System;
    using System.Collections.Generic;
    using Cos;

    /// <summary>
    /// Maps character codes to glyph names from a PostScript encoding.
    /// </summary>
    internal abstract class Encoding
    {
        protected readonly Dictionary<int, string> CodeToName = new Dictionary<int, string>(250);

        public IReadOnlyDictionary<int, string> CodeToNameMap => CodeToName;

        protected readonly Dictionary<string, int> NameToCode = new Dictionary<string, int>(250);

        public IReadOnlyDictionary<string, int> NameToCodeMap => NameToCode;

        public abstract string EncodingName { get; }

        public bool ContainsName(string name)
        {
            foreach (var keyValuePair in CodeToNameMap)
            {
                if (string.Equals(keyValuePair.Value, name, StringComparison.InvariantCultureIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public bool ContainsCode(int code)
        {
            return CodeToName.ContainsKey(code);
        }

        public string GetName(int code)
        {
            if (!CodeToName.TryGetValue(code, out var name))
            {
                return ".notdef";
            }

            return name;
        }

        protected void Add(int code, string name)
        {
            CodeToName[code] = name;

            if (!NameToCode.ContainsKey(name))
            {
                NameToCode[name] = code;
            }
        }
        
        public static bool TryGetNamedEncoding(CosName name, out Encoding encoding)
        {
            encoding = null;

            if (name == null)
            {
                return false;
            }

            if (name.Equals(CosName.STANDARD_ENCODING))
            {
                encoding = StandardEncoding.Instance;
                return true;
            }

            if (name.Equals(CosName.WIN_ANSI_ENCODING))
            {
                encoding = WinAnsiEncoding.Instance;
                return true;
            }

            if (name.Equals(CosName.MAC_EXPERT_ENCODING))
            {
                encoding = MacExpertEncoding.Instance;
                return true;
            }

            if (name.Equals(CosName.MAC_ROMAN_ENCODING))
            {
                encoding = MacRomanEncoding.Instance;
                return true;
            }

            return false;
        }
    }
}
