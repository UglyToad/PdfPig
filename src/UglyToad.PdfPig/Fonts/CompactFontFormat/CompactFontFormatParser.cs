namespace UglyToad.PdfPig.Fonts.CompactFontFormat
{
    using System;
    using Util;

    internal class CompactFontFormatParser
    {
        private const string TagOtto = "OTTO";
        private const string TagTtcf = "ttcf";
        private const string TagTtfonly = "\u0000\u0001\u0000\u0000";

        private readonly CompactFontFormatIndividualFontParser individualFontParser;
        private readonly CompactFontFormatIndexReader indexReader;

        public CompactFontFormatParser(CompactFontFormatIndividualFontParser individualFontParser, CompactFontFormatIndexReader indexReader)
        {
            this.individualFontParser = individualFontParser;
            this.indexReader = indexReader;
        }

        public void Parse(CompactFontFormatData data)
        {
            var tag = ReadTag(data);

            switch (tag)
            {
                case TagOtto:
                    throw new NotSupportedException("Currently tagged CFF data is not supported.");
                case TagTtcf:
                    throw new NotSupportedException("True Type Collection fonts are not supported.");
                case TagTtfonly:
                    throw new NotSupportedException("OpenType fonts containing a true type font are not supported.");
                default:
                    data.Seek(0);
                    break;
            }

            var header = ReadHeader(data);

            var fontNames = ReadStringIndex(data);

            var topLevelDict = indexReader.ReadDictionaryData(data);

            var stringIndex = ReadStringIndex(data);

            var globalSubroutineIndex = indexReader.ReadDictionaryData(data);

            for (var i = 0; i < fontNames.Length; i++)
            {
                var fontName = fontNames[i];

                individualFontParser.Parse(data, fontName, topLevelDict[i], stringIndex, globalSubroutineIndex);
            }
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
        private string[] ReadStringIndex(CompactFontFormatData data)
        {
            var index = indexReader.ReadIndex(data);

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
