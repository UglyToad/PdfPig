namespace UglyToad.PdfPig.Fonts.CompactFontFormat
{
    using System;
    using System.Collections.Generic;
    using Core;
    using Dictionaries;
    using TrueType;
    using TrueType.Parser;

    /// <summary>
    /// Parse the Compact Font Format (CFF).
    /// </summary>
    public static class CompactFontFormatParser
    {
        private const string TagOtto = "OTTO";
        private const string TagTtcf = "ttcf";
        private const string TagTtfonly = "\u0000\u0001\u0000\u0000";
        
        /// <summary>
        /// Read the Compact Font Format font from the input data.
        /// </summary>
        public static CompactFontFormatFontCollection Parse(CompactFontFormatData data)
        {
            var tag = ReadTag(data);

            switch (tag)
            {
                // An OpenType font containing CFF data: read its CFF table.
                case TagOtto:
                    return Parse(ReadOpenTypeCffTable(data));
                case TagTtcf:
                    throw new NotSupportedException("True Type Collection fonts are not currently supported.");
                // A TrueType sfnt version: normally glyf outlines, which are not CFF, but some fonts mislabelled this way
                // have a CFF table, as TrueTypeFontParser also allows. Read it if there is one.
                case TagTtfonly:
                    return Parse(ReadOpenTypeCffTable(data));
                default:
                    data.Seek(0);
                    break;
            }

            var header = ReadHeader(data);

            var fontNames = ReadStringIndex(data);

            var topLevelDictionaryIndex = CompactFontFormatIndexReader.ReadDictionaryData(data);

            var stringIndex = ReadStringIndex(data);

            var globalSubroutineIndex = CompactFontFormatIndexReader.ReadDictionaryData(data);

            var fonts = new Dictionary<string, CompactFontFormatFont>();

            var individualFontParser = new CompactFontFormatIndividualFontParser(new CompactFontFormatTopLevelDictionaryReader(), new CompactFontFormatPrivateDictionaryReader());

            for (var i = 0; i < fontNames.Length; i++)
            {
                var fontName = fontNames[i];

                fonts[fontName] = individualFontParser.Parse(data, fontName, topLevelDictionaryIndex[i].Span, stringIndex, globalSubroutineIndex);
            }

            return new CompactFontFormatFontCollection(header, fonts);
        }

        /// <summary>
        /// Get the <c>'CFF '</c> table of an OpenType font, which <paramref name="data"/> holds from its start, from the
        /// font's table directory.
        /// </summary>
        private static CompactFontFormatData ReadOpenTypeCffTable(CompactFontFormatData data)
        {
            var tables = TrueTypeFontParser.ReadTableDirectory(new TrueTypeDataBytes(new MemoryInputBytes(data.Memory)), out _);

            if (tables.TryGetValue(TrueTypeHeaderTable.Cff, out var cff))
            {
                if ((long)cff.Offset + cff.Length > data.Length)
                {
                    throw new InvalidFontFormatException($"The CFF table (offset {cff.Offset}, length {cff.Length}) lies outside the {data.Length} bytes of the OpenType font.");
                }

                return data.SnapshotPortion((int)cff.Offset, (int)cff.Length);
            }

            throw new NotSupportedException(tables.ContainsKey("CFF2")
                ? "The OpenType font has CFF2 outlines, which are not supported."
                : "The OpenType font has no CFF table.");
        }

        private static string ReadTag(CompactFontFormatData data)
        {
            var tag = data.ReadString(4, OtherEncodings.Iso88591);

            return tag;
        }

        private static CompactFontFormatHeader ReadHeader(CompactFontFormatData data)
        {
            var major = data.ReadCard8();
            var minor = data.ReadCard8();
            var headerSize = data.ReadCard8();
            var offsetSize = data.ReadOffsize();

            return new CompactFontFormatHeader(major, minor, headerSize, offsetSize);
        }

        /// <summary>
        /// Reads indexed string data.
        /// </summary>
        private static string[] ReadStringIndex(CompactFontFormatData data)
        {
            var index = CompactFontFormatIndexReader.ReadIndex(data);

            if (index.Length == 0)
            {
                return [];
            }

            var count = index.Length - 1;

            var result = new string[count];

            for (var i = 0; i < count; i++)
            {
                var length = index[i + 1] - index[i];

                if (length < 0)
                {
                    throw new InvalidOperationException($"Negative object length {length} at {i}. Current position: {data.Position}.");
                }

                result[i] = data.ReadString(length, OtherEncodings.Iso88591);
            }

            return result;
        }
    }
}
