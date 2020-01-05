namespace UglyToad.PdfPig.Fonts.CompactFontFormat
{
    using System;
    using System.Collections.Generic;
    using Charsets;
    using Encodings;
    using Fonts;

    internal static class CompactFontFormatEncodingReader
    {
        public static Encoding ReadEncoding(CompactFontFormatData data, ICompactFontFormatCharset charset, IReadOnlyList<string> stringIndex)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            var format = data.ReadCard8();

            // A few fonts have multiply encoded glyphs which are indicated by setting the high order bit of the format byte.
            // To get the real format out & with 0111 1111 (0x7f).
            var baseFormat = format & 0x7f;

            switch (baseFormat)
            {
                case 0:
                    return ReadFormat0Encoding(data, charset, stringIndex, format);
                case 1:
                    return ReadFormat1Encoding(data, charset, stringIndex, format);
                default:
                    throw new InvalidFontFormatException($"The provided format {format} for this Compact Font Format encoding was invalid.");
            }
        }

        private static CompactFontFormatFormat0Encoding ReadFormat0Encoding(CompactFontFormatData data, ICompactFontFormatCharset charset, IReadOnlyList<string> stringIndex, byte format)
        {
            var numberOfCodes = data.ReadCard8();

            var values = new List<(int code, int sid, string str)>();
            for (var i = 1; i <= numberOfCodes; i++)
            {
                var code = data.ReadCard8();
                var sid = charset.GetStringIdByGlyphId(i);
                var str = ReadString(sid, stringIndex);
                values.Add((code, sid, str));
            }

            IReadOnlyList<CompactFontFormatBuiltInEncoding.Supplement> supplements = new List<CompactFontFormatBuiltInEncoding.Supplement>();
            if (HasSupplement(format))
            {
                supplements = ReadSupplement(data, stringIndex);
            }

            return new CompactFontFormatFormat0Encoding(values, supplements);
        }

        private static CompactFontFormatFormat1Encoding ReadFormat1Encoding(CompactFontFormatData data, ICompactFontFormatCharset charset, IReadOnlyList<string> stringIndex, byte format)
        {
            var numberOfRanges = data.ReadCard8();

            var fromRanges = new List<(int code, int sid, string str)>();

            var gid = 1;
            for (var i = 0; i < numberOfRanges; i++)
            {
                int rangeFirst = data.ReadCard8();
                int rangeLeft = data.ReadCard8();
                for (var j = 0; j < 1 + rangeLeft; j++)
                {
                    var sid = charset.GetStringIdByGlyphId(gid);
                    var code = rangeFirst + j;
                    var str = ReadString(sid, stringIndex);
                    fromRanges.Add((code, sid, str));
                    gid++;
                }
            }

            IReadOnlyList<CompactFontFormatBuiltInEncoding.Supplement> supplements = new List<CompactFontFormatBuiltInEncoding.Supplement>();
            if (HasSupplement(format))
            {
                supplements = ReadSupplement(data, stringIndex);
            }

            return new CompactFontFormatFormat1Encoding(numberOfRanges, fromRanges, supplements);
        }

        private static IReadOnlyList<CompactFontFormatBuiltInEncoding.Supplement> ReadSupplement(CompactFontFormatData dataInput,
            IReadOnlyList<string> stringIndex)
        {
            var numberOfSupplements = dataInput.ReadCard8();
            var supplements = new CompactFontFormatBuiltInEncoding.Supplement[numberOfSupplements];

            for (var i = 0; i < supplements.Length; i++)
            {
                var code = dataInput.ReadCard8();
                var sid = dataInput.ReadSid();
                var name = ReadString(sid, stringIndex);
                supplements[i] = new CompactFontFormatBuiltInEncoding.Supplement(code, sid, name);
            }

            return supplements;
        }
        
        private static string ReadString(int index, IReadOnlyList<string> stringIndex)
        {
            if (index >= 0 && index <= 390)
            {
                return CompactFontFormatStandardStrings.GetName(index);
            }
            if (index - 391 < stringIndex.Count)
            {
                return stringIndex[index - 391];
            }

            // technically this maps to .notdef, but we need a unique sid name
            return "SID" + index;
        }

        private static bool HasSupplement(byte format)
        {
            // A few fonts have multiply encoded glyphs which are indicated by setting the high order bit of the format byte.
            return (format & 0x80) != 0;
        }
    }
}
