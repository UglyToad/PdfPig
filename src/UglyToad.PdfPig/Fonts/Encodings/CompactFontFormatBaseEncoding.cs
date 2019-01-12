namespace UglyToad.PdfPig.Fonts.Encodings
{
    using System.Collections.Generic;
    using CompactFontFormat;

    internal abstract class CompactFontFormatBaseEncoding : Encoding
    {
        private readonly Dictionary<int, string> codeToNameMap = new Dictionary<int, string>(250);

        public override string EncodingName { get; } = "CFF";

        /// <summary>
        /// Returns the PostScript name of the glyph for the given character code.
        /// </summary>
        public override string GetName(int code)
        {
            if (!codeToNameMap.TryGetValue(code, out var name))
            {
                return ".notdef";
            }

            return name;
        }

        public void Add(int code, int sid, string name)
        {
            codeToNameMap[code] = name;
            Add(code, name);
        }

        protected void Add(int code, int sid)
        {
            var name = CompactFontFormatStandardStrings.GetName(sid);
            codeToNameMap[code] = name;
            Add(code, name);
        }
    }
}
