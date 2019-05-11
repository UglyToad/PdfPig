namespace UglyToad.PdfPig.Fonts.CompactFontFormat
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Charsets;
    using CharStrings;
    using Dictionaries;
    using Encodings;
    using Exceptions;
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

            ICompactFontFormatCharset charset;
            if (topDictionary.CharSetOffset >= 0)
            {
                var charsetId = topDictionary.CharSetOffset;
                if (!topDictionary.IsCidFont && charsetId == 0)
                {
                    charset = CompactFontFormatIsoAdobeCharset.Value;
                }
                else if (!topDictionary.IsCidFont && charsetId == 1)
                {
                    charset = CompactFontFormatExpertCharset.Value;
                }
                else if (!topDictionary.IsCidFont && charsetId == 2)
                {
                    charset = CompactFontFormatExpertSubsetCharset.Value;
                }
                else
                {
                    charset = ReadCharset(data, topDictionary, charStringIndex, stringIndex);
                }
            }
            else
            {
                if (topDictionary.IsCidFont)
                {
                    // a CID font with no charset does not default to any predefined charset
                    charset = new CompactFontFormatEmptyCharset(charStringIndex.Count);
                }
                else
                {
                    charset = CompactFontFormatIsoAdobeCharset.Value;
                }
            }

            data.Seek(topDictionary.CharStringsOffset);

            Type2CharStrings charStrings;
            switch (topDictionary.CharStringType)
            {
                case CompactFontFormatCharStringType.Type1:
                    throw new NotImplementedException("Type 1 CharStrings are not currently supported in CFF font.");
                case CompactFontFormatCharStringType.Type2:
                    charStrings = Type2CharStringParser.Parse(charStringIndex, localSubroutines, globalSubroutineIndex, charset);
                    break;
                default:
                    throw new ArgumentOutOfRangeException($"Unexpected CharString type in CFF font: {topDictionary.CharStringType}.");
            }

            if (topDictionary.IsCidFont)
            {
                return ReadCidFont(data, topDictionary, charStringIndex.Count, stringIndex, privateDictionary, charset, Union<Type1CharStrings, Type2CharStrings>.Two(charStrings));
            }

            var encoding = topDictionary.EncodingOffset;

            Encoding fontEncoding = null;
            if (encoding != CompactFontFormatTopLevelDictionary.UnsetOffset)
            {
                if (encoding == 0)
                {
                    fontEncoding = CompactFontFormatStandardEncoding.Instance;
                }
                else if (encoding == 1)
                {
                    fontEncoding = CompactFontFormatExpertEncoding.Instance;
                }
                else
                {
                    data.Seek(encoding);
                    fontEncoding = CompactFontFormatEncodingReader.ReadEncoding(data, charset, stringIndex);
                }
            }

            return new CompactFontFormatFont(topDictionary, privateDictionary, charset, Union<Type1CharStrings, Type2CharStrings>.Two(charStrings), fontEncoding);
        }

        private static ICompactFontFormatCharset ReadCharset(CompactFontFormatData data, CompactFontFormatTopLevelDictionary topDictionary,
        CompactFontFormatIndex charStringIndex, IReadOnlyList<string> stringIndex)
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

                        return new CompactFontFormatFormat0Charset(glyphToNamesAndStringId);
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

                            return new CompactFontFormatFormat1Charset(glyphToNamesAndStringId);
                        }

                        return new CompactFontFormatFormat2Charset(glyphToNamesAndStringId);
                    }
                default:
                    throw new InvalidOperationException($"Unrecognized format for the Charset table in a CFF font. Got: {format}.");
            }
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

        private CompactFontFormatCidFont ReadCidFont(CompactFontFormatData data, CompactFontFormatTopLevelDictionary topLevelDictionary,
            int numberOfGlyphs,
            IReadOnlyList<string> stringIndex,
            CompactFontFormatPrivateDictionary privateDictionary,
            ICompactFontFormatCharset charset,
            Union<Type1CharStrings, Type2CharStrings> charstrings)
        {
            var offset = topLevelDictionary.CidFontOperators.FontDictionaryArray;

            data.Seek(offset);

            var fontDict = indexReader.ReadDictionaryData(data);

            var privateDictionaries = new List<CompactFontFormatPrivateDictionary>();
            var fontDictionaries = new List<CompactFontFormatTopLevelDictionary>();
            var fontLocalSubroutines = new List<CompactFontFormatIndex>();
            foreach (var index in fontDict)
            {
                var topLevelDictionaryCid = topLevelDictionaryReader.Read(new CompactFontFormatData(index), stringIndex);

                if (!topLevelDictionaryCid.PrivateDictionaryLocation.HasValue)
                {
                    throw new InvalidFontFormatException("The CID keyed Compact Font Format font did not contain a private dictionary for the font dictionary.");
                }

                var privateDictionaryBytes = data.SnapshotPortion(topLevelDictionaryCid.PrivateDictionaryLocation.Value.Offset,
                    topLevelDictionaryCid.PrivateDictionaryLocation.Value.Size);

                var privateDictionaryCid = privateDictionaryReader.Read(privateDictionaryBytes, stringIndex);

                // CFFParser.java line 625 - read the local subroutines.
                if (privateDictionaryCid.LocalSubroutineOffset.HasValue && privateDictionaryCid.LocalSubroutineOffset.Value > 0)
                {
                    data.Seek(topLevelDictionaryCid.PrivateDictionaryLocation.Value.Offset + privateDictionaryCid.LocalSubroutineOffset.Value);
                    var localSubroutines = indexReader.ReadDictionaryData(data);
                    fontLocalSubroutines.Add(localSubroutines);
                }

                fontDictionaries.Add(topLevelDictionaryCid);
                privateDictionaries.Add(privateDictionaryCid);
            }

            data.Seek(topLevelDictionary.CidFontOperators.FontDictionarySelect);

            var format = data.ReadCard8();

            ICompactFontFormatFdSelect fdSelect;
            switch (format)
            {
                case 0:
                    {
                        fdSelect = ReadFormat0FdSelect(data, numberOfGlyphs, topLevelDictionary.CidFontOperators.Ros);
                        break;
                    }
                case 3:
                    {
                        fdSelect = ReadFormat3FdSelect(data, topLevelDictionary.CidFontOperators.Ros);
                        break;
                    }
                default:
                    throw new InvalidFontFormatException($"Invalid Font Dictionary Select format: {format}.");
            }

            return new CompactFontFormatCidFont(topLevelDictionary, privateDictionary, charset, charstrings,
                fontDictionaries, privateDictionaries, fontLocalSubroutines, fdSelect);
        }

        private static CompactFontFormat0FdSelect ReadFormat0FdSelect(CompactFontFormatData data, int numberOfGlyphs,
            RegistryOrderingSupplement registryOrderingSupplement)
        {
            var dictionaries = new int[numberOfGlyphs];

            for (var i = 0; i < numberOfGlyphs; i++)
            {
                dictionaries[i] = data.ReadCard8();
            }

            return new CompactFontFormat0FdSelect(registryOrderingSupplement, dictionaries);
        }

        private static CompactFontFormat3FdSelect ReadFormat3FdSelect(CompactFontFormatData data, RegistryOrderingSupplement registryOrderingSupplement)
        {
            var numberOfRanges = data.ReadCard16();
            var ranges = new CompactFontFormat3FdSelect.Range3[numberOfRanges];

            for (var i = 0; i < numberOfRanges; i++)
            {
                var first = data.ReadCard16();
                var dictionary = data.ReadCard8();

                ranges[i] = new CompactFontFormat3FdSelect.Range3(first, dictionary);
            }

            var sentinel = data.ReadCard16();

            return new CompactFontFormat3FdSelect(registryOrderingSupplement, ranges, sentinel);
        }
    }

    internal interface ICompactFontFormatFdSelect
    {
        int GetFontDictionaryIndex(int glyphId);
    }

    internal class CompactFontFormat0FdSelect : ICompactFontFormatFdSelect
    {
        public RegistryOrderingSupplement RegistryOrderingSupplement { get; }

        public IReadOnlyList<int> FontDictionaries { get; }

        public CompactFontFormat0FdSelect(RegistryOrderingSupplement registryOrderingSupplement, IReadOnlyList<int> fontDictionaries)
        {
            RegistryOrderingSupplement = registryOrderingSupplement ?? throw new ArgumentNullException(nameof(registryOrderingSupplement));
            FontDictionaries = fontDictionaries ?? throw new ArgumentNullException(nameof(fontDictionaries));
        }

        public int GetFontDictionaryIndex(int glyphId)
        {
            throw new NotImplementedException();
        }
    }

    internal class CompactFontFormat3FdSelect : ICompactFontFormatFdSelect
    {
        public RegistryOrderingSupplement RegistryOrderingSupplement { get; }

        public IReadOnlyList<Range3> Ranges { get; }

        public int Sentinel { get; }

        public CompactFontFormat3FdSelect(RegistryOrderingSupplement registryOrderingSupplement, IReadOnlyList<Range3> ranges, int sentinel)
        {
            RegistryOrderingSupplement = registryOrderingSupplement ?? throw new ArgumentNullException(nameof(registryOrderingSupplement));
            Ranges = ranges ?? throw new ArgumentNullException(nameof(ranges));
            Sentinel = sentinel;
        }

        public int GetFontDictionaryIndex(int glyphId)
        {
            throw new NotImplementedException();
        }

        internal struct Range3
        {
            public int First { get; }

            public int FontDictionary { get; }

            public Range3(int first, int fontDictionary)
            {
                First = first;
                FontDictionary = fontDictionary;
            }

            public override string ToString()
            {
                return $"First {First}, Dictionary {FontDictionary}.";
            }
        }
    }



}
