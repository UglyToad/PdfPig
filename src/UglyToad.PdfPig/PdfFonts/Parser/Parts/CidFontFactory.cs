namespace UglyToad.PdfPig.PdfFonts.Parser.Parts
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using CidFonts;
    using Core;
    using Filters;
    using Fonts;
    using Fonts.CompactFontFormat;
    using Fonts.TrueType;
    using Fonts.TrueType.Parser;
    using Geometry;
    using PdfPig.Parser.Parts;
    using Tokenization.Scanner;
    using Tokens;
    using UglyToad.PdfPig.Logging;
    using Util;

    internal sealed class CidFontFactory
    {
        private readonly ILookupFilterProvider filterProvider;
        private readonly IPdfTokenScanner pdfScanner;
        private readonly ILog logger;

        public CidFontFactory(ILog log, IPdfTokenScanner pdfScanner, ILookupFilterProvider filterProvider)
        {
            this.logger = log;
            this.pdfScanner = pdfScanner;
            this.filterProvider = filterProvider;
        }

        public ICidFont? Generate(DictionaryToken dictionary)
        {
            var type = dictionary.GetNameOrDefault(NameToken.Type);
            if (!NameToken.Font.Equals(type))
            {
                throw new InvalidFontFormatException($"Expected \'Font\' dictionary but found \'{type}\'");
            }

            var widths = ReadWidths(dictionary);

            var defaultWidth = default(double?);
            if (dictionary.TryGet(NameToken.Dw, pdfScanner, out NumericToken? defaultWidthToken))
            {
                defaultWidth = defaultWidthToken.Double;
            }

            var verticalWritingMetrics = ReadVerticalDisplacements(dictionary, pdfScanner);

            FontDescriptor? descriptor = null;
            if (TryGetFontDescriptor(dictionary, out var descriptorDictionary))
            {
                descriptor = FontDescriptorFactory.Generate(descriptorDictionary, pdfScanner);
            }

            ICidFontProgram? fontProgram = null;
            try
            {
                fontProgram = ReadDescriptorFile(descriptor);
            }
            catch (Exception ex)
            {
                logger.Error($"Invalid descriptor in CID font named '{descriptor?.FontName}': {ex.Message}.");
            }

            var baseFont = dictionary.GetNameOrDefault(NameToken.BaseFont);

            var systemInfo = GetSystemInfo(dictionary);

            var subType = dictionary.GetNameOrDefault(NameToken.Subtype);
            if (NameToken.CidFontType0.Equals(subType))
            {
                return new Type0CidFont(fontProgram!, type!, subType!, baseFont!, systemInfo, descriptor!, verticalWritingMetrics, widths, defaultWidth);
            }

            if (NameToken.CidFontType2.Equals(subType))
            {
                var cidToGid = GetCharacterIdentifierToGlyphIndexMap(dictionary);

                return new Type2CidFont(type!, subType!, baseFont!, systemInfo, descriptor!, fontProgram, verticalWritingMetrics, widths, defaultWidth, cidToGid);
            }

            return null;
        }

        private bool TryGetFontDescriptor(DictionaryToken dictionary, [NotNullWhen(true)] out DictionaryToken? descriptorDictionary)
        {
            return dictionary.TryGet(NameToken.FontDescriptor, pdfScanner, out descriptorDictionary);
        }

        private ICidFontProgram? ReadDescriptorFile(FontDescriptor? descriptor)
        {
            if (descriptor?.FontFile is null)
            {
                return null;
            }

            var fontFileStream = DirectObjectFinder.Get<StreamToken>(descriptor.FontFile.ObjectKey, pdfScanner);

            if (fontFileStream is null)
            {
                return null;
            }

            var fontFile = fontFileStream.Decode(filterProvider, pdfScanner);

            switch (descriptor.FontFile.FileType)
            {
                case DescriptorFontFile.FontFileType.TrueType:
                    {
                        if (IsTrueTypeCff(fontFile.Span))
                        {
                            logger.Warn("The CID TrueType font has the signature of a CFF font. Using CID CFF instead.");
                            var font = CompactFontFormatParser.Parse(new CompactFontFormatData(fontFile));
                            return new PdfCidCompactFontFormatFont(font);
                        }
                        
                        var input = new TrueTypeDataBytes(new MemoryInputBytes(fontFile));
                        var ttf = TrueTypeFontParser.Parse(input);
                        return new PdfCidTrueTypeFont(ttf);
                    }
                case DescriptorFontFile.FontFileType.FromSubtype:
                    {
                        if (!DirectObjectFinder.TryGet(descriptor.FontFile.ObjectKey, pdfScanner, out StreamToken? str))
                        {
                            throw new NotSupportedException("Cannot read CID font from subtype.");
                        }

                        if (!str.StreamDictionary.TryGet(NameToken.Subtype, out NameToken? subtypeName))
                        {
                            throw new PdfDocumentFormatException($"The font file stream did not contain a subtype entry: {str.StreamDictionary}.");
                        }

                        if (subtypeName == NameToken.CidFontType0C
                            || subtypeName == NameToken.Type1C)
                        {
                            var bytes = str.Decode(filterProvider, pdfScanner);
                            var font = CompactFontFormatParser.Parse(new CompactFontFormatData(bytes));
                            return new PdfCidCompactFontFormatFont(font);
                        }

                        if (subtypeName == NameToken.OpenType)
                        {
                            var bytes = str.Decode(filterProvider, pdfScanner);
                            var ttf = TrueTypeFontParser.Parse(new TrueTypeDataBytes(new MemoryInputBytes(bytes)));
                            return new PdfCidTrueTypeFont(ttf);
                        }

                        throw new PdfDocumentFormatException($"Unexpected subtype for CID font: {subtypeName}.");
                    }
                default:
                    throw new NotSupportedException("Currently only TrueType fonts are supported.");
            }
        }

        private IReadOnlyDictionary<int, double> ReadWidths(DictionaryToken dict)
        {
            var widths = new Dictionary<int, double>();

            if (!dict.TryGet(NameToken.W, pdfScanner, out ArrayToken? widthArray))
            {
                return widths;
            }

            var size = widthArray.Data.Count;
            var counter = 0;
            while (counter < size)
            {
                var firstCode = DirectObjectFinder.Get<NumericToken>(widthArray.Data[counter++], pdfScanner);
                var next = widthArray.Data[counter++];
                if (DirectObjectFinder.TryGet(next, pdfScanner, out ArrayToken? array))
                {
                    var startRange = firstCode.Int;
                    var arraySize = array.Data.Count;

                    for (var i = 0; i < arraySize; i++)
                    {
                        var width = DirectObjectFinder.Get<NumericToken>(array.Data[i], pdfScanner);
                        widths[startRange + i] = width.Double;
                    }
                }
                else
                {
                    var secondCode = DirectObjectFinder.Get<NumericToken>(next, pdfScanner);
                    var rangeWidth = DirectObjectFinder.Get<NumericToken>(widthArray.Data[counter++], pdfScanner);
                    var startRange = firstCode.Int;
                    var endRange = secondCode.Int;
                    var width = rangeWidth.Double;
                    for (var i = startRange; i <= endRange; i++)
                    {
                        widths[i] = width;
                    }
                }
            }

            return widths;
        }

        private static VerticalWritingMetrics ReadVerticalDisplacements(DictionaryToken dict, IPdfTokenScanner pdfScanner)
        {
            var verticalDisplacements = new Dictionary<int, double>();
            var positionVectors = new Dictionary<int, PdfVector>();

            // The default position vector and displacement vector are specified by the DW2 entry.
            VerticalVectorComponents dw2;
            if (!dict.TryGet(NameToken.Dw2, out var dw2Token) || !(dw2Token is ArrayToken arrayVerticalComponents))
            {
                dw2 = VerticalVectorComponents.Default;
            }
            else
            {
                var position = ((NumericToken)arrayVerticalComponents.Data[0]).Double;
                var displacement = ((NumericToken)arrayVerticalComponents.Data[1]).Double;

                dw2 = new VerticalVectorComponents(position, displacement);
            }

            // vertical metrics for individual CIDs.
            if (dict.TryGet(NameToken.W2, pdfScanner, out ArrayToken? w2))
            {
                for (var i = 0; i < w2.Data.Count; i++)
                {
                    var c = DirectObjectFinder.Get<NumericToken>(w2.Data[i], pdfScanner);
                    var next = w2.Data[++i];
                    if (DirectObjectFinder.TryGet(next, pdfScanner, out ArrayToken? array))
                    {
                        for (var j = 0; j < array.Data.Count; j++)
                        {
                            var cid = c.Int + j;
                            // ReSharper disable InconsistentNaming
                            var w1y = DirectObjectFinder.Get<NumericToken>(array.Data[j], pdfScanner);
                            var v1x = DirectObjectFinder.Get<NumericToken>(array.Data[++j], pdfScanner);
                            var v1y = DirectObjectFinder.Get<NumericToken>(array.Data[++j], pdfScanner);

                            verticalDisplacements[cid] = w1y.Double;

                            positionVectors[cid] = new PdfVector(v1x.Double, v1y.Double);
                        }
                    }
                    else
                    {
                        var first = c.Int;
                        var last = ((NumericToken)next).Int;
                        var w1y = DirectObjectFinder.Get<NumericToken>(w2.Data[++i], pdfScanner);
                        var v1x = DirectObjectFinder.Get<NumericToken>(w2.Data[++i], pdfScanner);
                        var v1y = DirectObjectFinder.Get<NumericToken>(w2.Data[++i], pdfScanner);
                        // ReSharper restore InconsistentNaming

                        for (var cid = first; cid <= last; cid++)
                        {
                            verticalDisplacements[cid] = w1y.Double;

                            positionVectors[cid] = new PdfVector(v1x.Double, v1y.Double);
                        }
                    }
                }
            }
            
            return new VerticalWritingMetrics(dw2, verticalDisplacements, positionVectors);
        }

        private CharacterIdentifierSystemInfo GetSystemInfo(DictionaryToken dictionary)
        {
            if (!dictionary.TryGet(NameToken.CidSystemInfo, out var cidEntry))
            {
                throw new InvalidFontFormatException($"No CID System Info was found in the CID Font dictionary: {dictionary}");
            }

            if (!(cidEntry is DictionaryToken cidDictionary))
            {
                cidDictionary = DirectObjectFinder.Get<DictionaryToken>(cidEntry, pdfScanner);
            }

            var registry = SafeKeyAccess(cidDictionary, NameToken.Registry);
            var ordering = SafeKeyAccess(cidDictionary, NameToken.Ordering);
            var supplement = cidDictionary.GetIntOrDefault(NameToken.Supplement);

            return new CharacterIdentifierSystemInfo(registry, ordering, supplement);
        }

        private CharacterIdentifierToGlyphIndexMap GetCharacterIdentifierToGlyphIndexMap(DictionaryToken dictionary)
        {
            if (!dictionary.TryGet(NameToken.CidToGidMap, out var entry))
            {
                return new CharacterIdentifierToGlyphIndexMap();
            }

            if (DirectObjectFinder.TryGet(entry, pdfScanner, out NameToken? _))
            {
                return new CharacterIdentifierToGlyphIndexMap();
            }

            if (!DirectObjectFinder.TryGet(entry, pdfScanner, out StreamToken? stream))
            {
                throw new PdfDocumentFormatException($"No stream or name token found for /CIDToGIDMap in dictionary: {dictionary}.");
            }

            var bytes = stream.Decode(filterProvider, pdfScanner);

            return new CharacterIdentifierToGlyphIndexMap(bytes.Span);
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
                if (DirectObjectFinder.TryGet(obj, pdfScanner, out StringToken? stringToken))
                {
                    return stringToken.Data;
                }

                if (DirectObjectFinder.TryGet(obj, pdfScanner, out HexToken? hexToken))
                {
                    return hexToken.Data;
                }

                throw new PdfDocumentFormatException($"Could not get key for name: {keyName} in {dictionary}.");
            }

            return string.Empty;
        }

        private static bool IsTrueTypeCff(ReadOnlySpan<byte> data)
        {
            if (data.Length < 4)
            {
                return false;
            }
            
            // See https://docs.fileformat.com/font/cff/
            // https://adobe-type-tools.github.io/font-tech-notes/pdfs/5176.CFF.pdf
            byte major = data[0]; // Major version
            byte minor = data[1]; // Minor version
            byte hdrSize = data[2]; // Header size 
            byte offSize = data[3]; // Absolute offset

            return major == 0x01 &&
                   minor == 0x00 &&
                   hdrSize >= 0x04 &&
                   offSize >= 0x01 && offSize <= 0x04;
        }
    }
}
