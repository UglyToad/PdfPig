namespace UglyToad.PdfPig.Fonts.CompactFontFormat.Charsets
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    internal abstract class CompactFontFormatCharset : ICompactFontFormatCharset
    {
        protected readonly IReadOnlyDictionary<int, (int stringId, string name)> GlyphIdToStringIdAndName;

        public bool IsCidCharset { get; } = false;

        protected CompactFontFormatCharset(IReadOnlyList<(int glyphId, int stringId, string name)> data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            var dictionary = new Dictionary<int, (int stringId, string name)>
            {
                {0, (0, ".notdef")}
            };

            foreach (var tuple in data)
            {
                dictionary[tuple.glyphId] = (tuple.stringId, tuple.name);
            }

            GlyphIdToStringIdAndName = dictionary;
        }

        public virtual string GetNameByGlyphId(int glyphId)
        {
            return GlyphIdToStringIdAndName[glyphId].name;
        }

        public virtual string GetNameByStringId(int stringId)
        {
            return GlyphIdToStringIdAndName.SingleOrDefault(x => x.Value.stringId == stringId).Value.name;
        }

        public virtual int GetStringIdByGlyphId(int glyphId)
        {
            if (GlyphIdToStringIdAndName.TryGetValue(glyphId, out var strings))
            {
                return strings.stringId;
            }

            return 0;
        }

        public int GetGlyphIdByName(string characterName)
        {
            foreach (var keyValuePair in GlyphIdToStringIdAndName)
            {
                if (string.Equals(keyValuePair.Value.name, characterName, StringComparison.Ordinal))
                {
                    return keyValuePair.Key;
                }
            }

            return 0;
        }
    }
}