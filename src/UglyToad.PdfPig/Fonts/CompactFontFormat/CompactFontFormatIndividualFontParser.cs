namespace UglyToad.PdfPig.Fonts.CompactFontFormat
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Charsets;
    using CharStrings;
    using Dictionaries;
    using Type1.CharStrings;
    using Util;

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

        public CompactFontFormatFont Parse(CompactFontFormatData data, string name, IReadOnlyList<byte> topDictionaryIndex, IReadOnlyList<string> stringIndex,
            CompactFontFormatIndex globalSubroutineIndex)
        {
            var individualData = new CompactFontFormatData(topDictionaryIndex.ToArray());

            var topDictionary = topLevelDictionaryReader.Read(individualData, stringIndex);

            var privateDictionary = CompactFontFormatPrivateDictionary.GetDefault();

            if (topDictionary.PrivateDictionaryLocation.HasValue && topDictionary.PrivateDictionaryLocation.Value.Size > 0)
            {
                var privateDictionaryBytes = data.SnapshotPortion(topDictionary.PrivateDictionaryLocation.Value.Offset,
                    topDictionary.PrivateDictionaryLocation.Value.Size);

                privateDictionary = privateDictionaryReader.Read(privateDictionaryBytes, stringIndex);
            }

            if (topDictionary.CharStringsOffset < 0)
            {
                throw new InvalidOperationException("Expected CFF to contain a CharString offset.");
            }

            var localSubroutines = CompactFontFormatIndex.None;
            if (privateDictionary.LocalSubroutineOffset.HasValue && topDictionary.PrivateDictionaryLocation.HasValue)
            {
                data.Seek(privateDictionary.LocalSubroutineOffset.Value + topDictionary.PrivateDictionaryLocation.Value.Offset);

                localSubroutines = indexReader.ReadDictionaryData(data);
            }

            data.Seek(topDictionary.CharStringsOffset);

            var charStringIndex = indexReader.ReadDictionaryData(data);

            ICompactFontFormatCharset charset = null;

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

                            for (var glyphId = 1; glyphId < charStringIndex.Count; glyphId++)
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

                            for (var glyphId = 1; glyphId < charStringIndex.Count; glyphId++)
                            {
                                var firstSid = data.ReadSid();
                                var numberInRange = format == 1 ? data.ReadCard8() : data.ReadCard16();

                                glyphToNamesAndStringId.Add((glyphId, firstSid, ReadString(firstSid, stringIndex)));
                                for (var i = 0; i < numberInRange; i++)
                                {
                                    glyphId++;
                                    var sid = firstSid + i + 1;
                                    glyphToNamesAndStringId.Add((glyphId, sid, ReadString(sid, stringIndex)));
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
                    charStrings = Type2CharStringParser.Parse(charStringIndex, localSubroutines, globalSubroutineIndex, charset);
                    break;
                default:
                    throw new ArgumentOutOfRangeException($"Unexpected CharString type in CFF font: {topDictionary.CharStringType}.");
            }

            return new CompactFontFormatFont(topDictionary, privateDictionary, charset, Union<Type1CharStrings, Type2CharStrings>.Two(charStrings));
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

            // technically this maps to .notdef, but PDFBox uses this
            return "SID" + index;
        }
    }
}
