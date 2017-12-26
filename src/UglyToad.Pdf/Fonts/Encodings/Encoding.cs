namespace UglyToad.Pdf.Fonts.Encodings
{
    using System;
    using System.Collections.Generic;

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
    }
}
