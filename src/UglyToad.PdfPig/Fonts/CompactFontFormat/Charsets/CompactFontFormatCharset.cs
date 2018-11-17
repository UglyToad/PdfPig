namespace UglyToad.PdfPig.Fonts.CompactFontFormat.Charsets
{
    using System;
    using System.Collections.Generic;

    internal abstract class CompactFontFormatCharset : ICompactFontFormatCharset
    {
        protected readonly IReadOnlyDictionary<int, (int stringId, string name)> GlyphIdToStringIdAndName;

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
            throw new NotImplementedException();
        }

        public virtual string GetStringIdByGlyphId(int glyphId)
        {
            throw new NotImplementedException();
        }
    }
}