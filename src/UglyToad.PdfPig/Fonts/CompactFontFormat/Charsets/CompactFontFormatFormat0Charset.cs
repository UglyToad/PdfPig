namespace UglyToad.PdfPig.Fonts.CompactFontFormat.Charsets
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// A Charset from a Compact Font Format font file best for fonts with relatively unordered string ids.
    /// </summary>
    internal class CompactFontFormatFormat0Charset
    {
        private readonly IReadOnlyDictionary<int, (int stringId, string name)> glyphIdToStringIdAndName;

        public CompactFontFormatFormat0Charset(IReadOnlyList<(int glyphId, int stringId, string name)> data)
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

            glyphIdToStringIdAndName = dictionary;
        }
    }

    /// <summary>
    /// A Charset from a Compact Font Format font file best for fonts with well ordered string ids.
    /// </summary>
    internal class CompactFontFormatFormat1Charset
    {
        private readonly IReadOnlyDictionary<int, (int stringId, string name)> glyphIdToStringIdAndName;

        public CompactFontFormatFormat1Charset(IReadOnlyList<(int glyphId, int stringId, string name)> data)
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

            glyphIdToStringIdAndName = dictionary;
        }
    }

    /// <summary>
    /// A Charset from a Compact Font Format font file best for fonts with a large number of well ordered string ids.
    /// </summary>
    internal class CompactFontFormatFormat2Charset
    {
        private readonly IReadOnlyDictionary<int, (int stringId, string name)> glyphIdToStringIdAndName;

        public CompactFontFormatFormat2Charset(IReadOnlyList<(int glyphId, int stringId, string name)> data)
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

            glyphIdToStringIdAndName = dictionary;
        }
    }
}
