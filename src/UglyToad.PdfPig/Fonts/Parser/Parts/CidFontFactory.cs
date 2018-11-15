namespace UglyToad.PdfPig.Fonts.Parser.Parts
{
    using System;
    using System.Collections.Generic;
    using CidFonts;
    using Exceptions;
    using Filters;
    using Geometry;
    using IO;
    using PdfPig.Parser.Parts;
    using Tokenization.Scanner;
    using Tokenization.Tokens;
    using TrueType;
    using TrueType.Parser;
    using Util;

    internal class CidFontFactory
    {
        private readonly FontDescriptorFactory descriptorFactory;
        private readonly TrueTypeFontParser trueTypeFontParser;
        private readonly IFilterProvider filterProvider;
        private readonly IPdfTokenScanner pdfScanner;

        public CidFontFactory(IPdfTokenScanner pdfScanner, FontDescriptorFactory descriptorFactory, TrueTypeFontParser trueTypeFontParser, 
            IFilterProvider filterProvider)
        {
            this.descriptorFactory = descriptorFactory;
            this.trueTypeFontParser = trueTypeFontParser;
            this.filterProvider = filterProvider;
            this.pdfScanner = pdfScanner;
        }

        public ICidFont Generate(DictionaryToken dictionary, bool isLenientParsing)
        {
            var type = dictionary.GetNameOrDefault(NameToken.Type);
            if (!NameToken.Font.Equals(type))
            {
                throw new InvalidFontFormatException($"Expected \'Font\' dictionary but found \'{type}\'");
            }

            var widths = ReadWidths(dictionary);
            var verticalWritingMetrics = ReadVerticalDisplacements(dictionary);

            FontDescriptor descriptor = null;
            if (TryGetFontDescriptor(dictionary, out var descriptorDictionary))
            {
                descriptor = descriptorFactory.Generate(descriptorDictionary, pdfScanner, isLenientParsing);
            }

            var fontProgram = ReadDescriptorFile(descriptor);

            var baseFont = dictionary.GetNameOrDefault(NameToken.BaseFont);

            var systemInfo = GetSystemInfo(dictionary);
            
            var subType = dictionary.GetNameOrDefault(NameToken.Subtype);
            if (NameToken.CidFontType0.Equals(subType))
            {
                //return new PDCIDFontType0(dictionary, parent);
            }

            if (NameToken.CidFontType2.Equals(subType))
            {
                var cidToGid = GetCharacterIdentifierToGlyphIndexMap(dictionary, isLenientParsing);

                return new Type2CidFont(type, subType, baseFont, systemInfo, descriptor, fontProgram, verticalWritingMetrics, widths, cidToGid);
            }

            return null;
        }
        
        private bool TryGetFontDescriptor(DictionaryToken dictionary, out DictionaryToken descriptorDictionary)
        {
            descriptorDictionary = null;

            if (!dictionary.TryGet(NameToken.FontDesc, out var baseValue))
            {
                return false;
            }

            var descriptor = DirectObjectFinder.Get<DictionaryToken>(baseValue, pdfScanner);
            
            descriptorDictionary = descriptor;

            return true;
        }

        private ICidFontProgram ReadDescriptorFile(FontDescriptor descriptor)
        {
            if (descriptor?.FontFile == null)
            {
                return null;
            }

            var fontFileStream = DirectObjectFinder.Get<StreamToken>(descriptor.FontFile.ObjectKey, pdfScanner);

            if (fontFileStream == null)
            {
                return null;
            }

            var fontFile = fontFileStream.Decode(filterProvider);
            
            switch (descriptor.FontFile.FileType)
            {
                case DescriptorFontFile.FontFileType.TrueType:
                    var input = new TrueTypeDataBytes(new ByteArrayInputBytes(fontFile));
                    return trueTypeFontParser.Parse(input);
                default:
                    throw new NotSupportedException("Currently only TrueType fonts are supported.");
            }
        }

        private static IReadOnlyDictionary<int, decimal> ReadWidths(DictionaryToken dict)
        {
            var widths = new Dictionary<int, decimal>();

            if (!dict.TryGet(NameToken.W, out var widthsItem) || !(widthsItem is ArrayToken widthArray))
            {
                return widths;
            }

            int size = widthArray.Data.Count;
            int counter = 0;
            while (counter < size)
            {
                var firstCode = (NumericToken)widthArray.Data[counter++];
                var next = widthArray.Data[counter++];
                if (next is ArrayToken array)
                {
                    int startRange = firstCode.Int;
                    int arraySize = array.Data.Count;

                    for (int i = 0; i < arraySize; i++)
                    {
                        var width = (NumericToken)array.Data[i];
                        widths[startRange + i] = width.Data;
                    }
                }
                else
                {
                    var secondCode = (NumericToken)next;
                    var rangeWidth = (NumericToken)widthArray.Data[counter++];
                    int startRange = firstCode.Int;
                    int endRange = secondCode.Int;
                    var width = rangeWidth.Data;
                    for (var i = startRange; i <= endRange; i++)
                    {
                        widths[i] = width;
                    }
                }
            }

            return widths;
        }

        private static VerticalWritingMetrics ReadVerticalDisplacements(DictionaryToken dict)
        {
            var verticalDisplacements = new Dictionary<int, decimal>();
            var positionVectors = new Dictionary<int, PdfVector>();

            VerticalVectorComponents dw2;
            if (!dict.TryGet(NameToken.Dw2, out var dw2Token) || !(dw2Token is ArrayToken arrayVerticalComponents))
            {
                dw2 = new VerticalVectorComponents(880, -1000);
            }
            else
            {
                var position = ((NumericToken)arrayVerticalComponents.Data[0]).Data;
                var displacement = ((NumericToken)arrayVerticalComponents.Data[1]).Data;

                dw2 = new VerticalVectorComponents(position, displacement);
            }

            // vertical metrics for individual CIDs.
            if (dict.TryGet(NameToken.W2, out var w2Token) && w2Token is ArrayToken w2)
            {
                for (var i = 0; i < w2.Data.Count; i++)
                {
                    var c = (NumericToken)w2.Data[i];
                    var next = w2.Data[++i];

                    if (next is ArrayToken array)
                    {
                        for (int j = 0; j < array.Data.Count; j++)
                        {
                            int cid = c.Int + j;
                            var w1y = (NumericToken)array.Data[j];
                            var v1x = (NumericToken)array.Data[++j];
                            var v1y = (NumericToken)array.Data[++j];

                            verticalDisplacements[cid] = w1y.Data;

                            positionVectors[cid] = new PdfVector(v1x.Data, v1y.Data);
                        }
                    }
                    else
                    {
                        int first = c.Int;
                        int last = ((NumericToken)next).Int;
                        var w1y = (NumericToken)w2.Data[++i];
                        var v1x = (NumericToken)w2.Data[++i];
                        var v1y = (NumericToken)w2.Data[++i];

                        for (var cid = first; cid <= last; cid++)
                        {
                            verticalDisplacements[cid] = w1y.Data;

                            positionVectors[cid] = new PdfVector(v1x.Data, v1y.Data);
                        }
                    }
                }
            }

            return new VerticalWritingMetrics(dw2, verticalDisplacements, positionVectors);
        }

        private CharacterIdentifierSystemInfo GetSystemInfo(DictionaryToken dictionary)
        {
            if(!dictionary.TryGet(NameToken.CidSystemInfo, out var cidEntry))
            {
                throw new InvalidFontFormatException($"No CID System Info was found in the CID Font dictionary: {dictionary}");
            }

            if (cidEntry is DictionaryToken cidDictionary)
            {
                
            }
            else
            {
                cidDictionary =
                    DirectObjectFinder.Get<DictionaryToken>(cidEntry, pdfScanner);
            }

            var registry = SafeKeyAccess(cidDictionary, NameToken.Registry);
            var ordering = SafeKeyAccess(cidDictionary, NameToken.Ordering);
            var supplement = cidDictionary.GetIntOrDefault(NameToken.Supplement);

            return new CharacterIdentifierSystemInfo(registry, ordering, supplement);
        }

        private CharacterIdentifierToGlyphIndexMap GetCharacterIdentifierToGlyphIndexMap(DictionaryToken dictionary, bool isLenientParsing)
        {
            if (!dictionary.TryGet(NameToken.CidToGidMap, out var entry))
            {
                return new CharacterIdentifierToGlyphIndexMap();
            }

            if (entry is NameToken name)
            {
                if (!name.Equals(NameToken.Identity) && !isLenientParsing)
                {
                    throw new InvalidOperationException($"The CIDToGIDMap in a Type 0 font should have the value /Identity, instead got: {name}.");
                }

                return new CharacterIdentifierToGlyphIndexMap();
            }

            var stream = DirectObjectFinder.Get<StreamToken>(entry, pdfScanner);

            var bytes = stream.Decode(filterProvider);

            return new CharacterIdentifierToGlyphIndexMap(bytes);
        }

        private string SafeKeyAccess(DictionaryToken dictionary, NameToken keyName)
        {
            if (!dictionary.TryGet(keyName, out var token))
            {
                return string.Empty;
            }

            if (token is StringToken str)
            {
                return str.Data;
            }

            if (token is HexToken hex)
            {
                return hex.Data;
            }

            if (token is IndirectReferenceToken obj)
            {
                return DirectObjectFinder.Get<StringToken>(obj, pdfScanner).Data;
            }

            return string.Empty;
        }
    }
}
