namespace UglyToad.PdfPig.PdfFonts.Encodings
{
    using System.Collections.Generic;
    using Tokens;

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
            return NameToCode.ContainsKey(name);
        }

        public bool ContainsCode(int code)
        {
            return CodeToName.ContainsKey(code);
        }

        public virtual string GetName(int code)
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
