namespace UglyToad.PdfPig.Fonts.CompactFontFormat
{
    using System;
    using System.Collections.Generic;
    using Charsets;
    using CharStrings;
    using Dictionaries;

    internal class CompactFontFormatIndividualFontParser
    {
        private readonly CompactFontFormatIndexReader indexReader;
        private readonly CompactFontFormatTopLevelDictionaryReader topLevelDictionaryReader;
        private readonly CompactFontFormatPrivateDictionaryReader privateDictionaryReader;

        public CompactFontFormatIndividualFontParser(CompactFontFormatIndexReader indexReader,
            CompactFontFormatTopLevelDictionaryReader topLevelDictionaryReader,
            CompactFontFormatPrivateDictionaryReader privateDictionaryReader)
        {
            this.indexReader = indexReader;
            this.topLevelDictionaryReader = topLevelDictionaryReader;
            this.privateDictionaryReader = privateDictionaryReader;
        }

        public void Parse(CompactFontFormatData data, string name, byte[] topDictionaryIndex, string[] stringIndex)
        {
            var individualData = new CompactFontFormatData(topDictionaryIndex);

            var topDictionary = topLevelDictionaryReader.Read(individualData, stringIndex);

            var privateDictionary = new CompactFontFormatPrivateDictionary();

            if (topDictionary.PrivateDictionarySizeAndOffset.Item2 >= 0)
            {
                data.Seek(topDictionary.PrivateDictionarySizeAndOffset.Item2);

                privateDictionary = privateDictionaryReader.Read(data, stringIndex);
            }

            if (topDictionary.CharStringsOffset < 0)
            {
                throw new InvalidOperationException("Expected CFF to contain a CharString offset.");
            }

            data.Seek(topDictionary.CharStringsOffset);

            var charStringIndex = indexReader.ReadDictionaryData(data);

            object charset = null;

            if (topDictionary.IsCidFont && topDictionary.CharSetOffset >= 0 && topDictionary.CharSetOffset <= 2)
            {
                switch (topDictionary.CharSetOffset)
                {
                    case 0:
                        charset = CompactFontFormatIsoAdobeCharset.Value;
                        break;
                    case 1:
                        charset = CompactFontFormatExpertCharset.Value;
                        break;
                    case 2:
                        charset = CompactFontFormatExpertSubsetCharset.Value;
                        break;
                }
            }
            else
            {
                data.Seek(topDictionary.CharSetOffset);

                var format = data.ReadCard8();

                switch (format)
                {
                    case 0:
                        {
                            var glyphToNamesAndStringId = new List<(int glyphId, int stringId, string name)>();

                            for (var glyphId = 1; glyphId < charStringIndex.Length; glyphId++)
                            {
                                var stringId = data.ReadSid();
                                glyphToNamesAndStringId.Add((glyphId, stringId, ReadString(stringId, stringIndex)));
                            }

                            charset = new CompactFontFormatFormat0Charset(glyphToNamesAndStringId);

                            break;
                        }
                    case 1:
                    case 2:
                        {
                            var glyphToNamesAndStringId = new List<(int glyphId, int stringId, string name)>();

                            for (var glyphId = 1; glyphId < charStringIndex.Length; glyphId++)
                            {
                                var firstSid = data.ReadSid();
                                var numberInRange = format == 1 ? data.ReadCard8() : data.ReadCard16();

                                glyphToNamesAndStringId.Add((glyphId, firstSid, ReadString(firstSid, stringIndex)));
                                glyphId++;
                                for (var i = 0; i < numberInRange; i++)
                                {
                                    glyphToNamesAndStringId.Add((glyphId, firstSid + i + 1, ReadString(firstSid, stringIndex)));
                                    glyphId++;
                                }
                            }

                            if (format == 1)
                            {

                                charset = new CompactFontFormatFormat1Charset(glyphToNamesAndStringId);
                            }
                            else
                            {
                                charset = new CompactFontFormatFormat2Charset(glyphToNamesAndStringId);
                            }

                            break;
                        }
                    default:
                        throw new InvalidOperationException($"Unrecognized format for the Charset table in a CFF font. Got: {format}.");
                }
            }

            data.Seek(topDictionary.CharStringsOffset);

            Type2CharStrings charStrings;
            switch (topDictionary.CharStringType)
            {
                case CompactFontFormatCharStringType.Type1:
                    throw new NotImplementedException();
                case CompactFontFormatCharStringType.Type2:
                    charStrings = Type2CharStringParser.Parse(charStringIndex);
                    break;
                default:
                    throw new ArgumentOutOfRangeException($"Unexpected CharString type in CFF font: {topDictionary.CharStringType}.");
            }
        }

        private static string ReadString(int index, string[] stringIndex)
        {
            if (index >= 0 && index <= 390)
            {
                return CompactFontFormatStandardStrings.GetName(index);
            }
            if (index - 391 < stringIndex.Length)
            {
                return stringIndex[index - 391];
            }

            // technically this maps to .notdef, but we need a unique sid name
            return "SID" + index;
        }
    }
}
