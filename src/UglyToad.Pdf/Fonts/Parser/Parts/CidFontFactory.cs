namespace UglyToad.Pdf.Fonts.Parser.Parts
{
    using System;
    using System.Collections.Generic;
    using CidFonts;
    using ContentStream;
    using ContentStream.TypedAccessors;
    using Cos;
    using Exceptions;
    using Filters;
    using Geometry;
    using IO;
    using Pdf.Parser;
    using Pdf.Parser.Parts;
    using TrueType;
    using TrueType.Parser;

    internal class CidFontFactory
    {
        private readonly FontDescriptorFactory descriptorFactory;
        private readonly TrueTypeFontParser trueTypeFontParser;
        private readonly IPdfObjectParser pdfObjectParser;
        private readonly IFilterProvider filterProvider;

        public CidFontFactory(FontDescriptorFactory descriptorFactory, TrueTypeFontParser trueTypeFontParser, 
            IPdfObjectParser pdfObjectParser,
            IFilterProvider filterProvider)
        {
            this.descriptorFactory = descriptorFactory;
            this.trueTypeFontParser = trueTypeFontParser;
            this.pdfObjectParser = pdfObjectParser;
            this.filterProvider = filterProvider;
        }

        public ICidFont Generate(PdfDictionary dictionary, IRandomAccessRead reader, bool isLenientParsing)
        {
            var type = dictionary.GetName(CosName.TYPE);
            if (!CosName.FONT.Equals(type))
            {
                throw new InvalidFontFormatException($"Expected \'Font\' dictionary but found \'{type.Name}\'");
            }

            var widths = ReadWidths(dictionary);
            var verticalWritingMetrics = ReadVerticalDisplacements(dictionary);

            FontDescriptor descriptor = null;
            if (TryGetFontDescriptor(dictionary, reader, out var descriptorDictionary))
            {
                descriptor = descriptorFactory.Generate(descriptorDictionary, isLenientParsing);
            }

            var fontProgram = ReadDescriptorFile(descriptor, reader, isLenientParsing);

            var baseFont = dictionary.GetName(CosName.BASE_FONT);

            var systemInfo = GetSystemInfo(dictionary, reader, isLenientParsing);

            var subType = dictionary.GetName(CosName.SUBTYPE);
            if (CosName.CID_FONT_TYPE0.Equals(subType))
            {
                //return new PDCIDFontType0(dictionary, parent);
            }

            if (CosName.CID_FONT_TYPE2.Equals(subType))
            {
                return new Type2CidFont(type, subType, baseFont, systemInfo, descriptor, fontProgram, verticalWritingMetrics, widths);
            }

            return null;
        }

        private bool TryGetFontDescriptor(PdfDictionary dictionary, IRandomAccessRead reader, out PdfDictionary descriptorDictionary)
        {
            descriptorDictionary = null;

            if (!dictionary.TryGetValue(CosName.FONT_DESC, out var baseValue) || !(baseValue is CosObject obj))
            {
                return false;
            }

            var descriptorObj = pdfObjectParser.Parse(obj.ToIndirectReference(), reader, false);

            if (!(descriptorObj is PdfDictionary descriptor))
            {
                return false;
            }

            descriptorDictionary = descriptor;

            return true;
        }

        private ICidFontProgram ReadDescriptorFile(FontDescriptor descriptor, IRandomAccessRead reader, bool isLenientParsing)
        {
            if (descriptor?.FontFile == null)
            {
                return null;
            }

            var fontFileStream = pdfObjectParser.Parse(descriptor.FontFile.ObjectKey, reader, isLenientParsing) as PdfRawStream;

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

        private static IReadOnlyDictionary<int, decimal> ReadWidths(PdfDictionary dict)
        {
            var widths = new Dictionary<int, decimal>();

            if (!dict.TryGetItemOfType(CosName.W, out COSArray widthArray))
            {
                return widths;
            }

            int size = widthArray.size();
            int counter = 0;
            while (counter < size)
            {
                var firstCode = (ICosNumber)widthArray.getObject(counter++);
                var next = widthArray.getObject(counter++);
                if (next is COSArray array)
                {
                    int startRange = firstCode.AsInt();
                    int arraySize = array.size();
                    for (int i = 0; i < arraySize; i++)
                    {
                        var width = (ICosNumber)array.getObject(i);
                        widths[startRange + i] = width.AsDecimal();
                    }
                }
                else
                {
                    var secondCode = (ICosNumber)next;
                    var rangeWidth = (ICosNumber)widthArray.getObject(counter++);
                    int startRange = firstCode.AsInt();
                    int endRange = secondCode.AsInt();
                    var width = rangeWidth.AsDecimal();
                    for (var i = startRange; i <= endRange; i++)
                    {
                        widths[i] = width;
                    }
                }
            }

            return widths;
        }

        private VerticalWritingMetrics ReadVerticalDisplacements(PdfDictionary dict)
        {
            var verticalDisplacements = new Dictionary<int, decimal>();
            var positionVectors = new Dictionary<int, PdfVector>();

            VerticalVectorComponents dw2;
            if (!dict.TryGetItemOfType(CosName.DW2, out COSArray arrayVerticalComponents))
            {
                dw2 = new VerticalVectorComponents(880, -1000);
            }
            else
            {
                var position = ((ICosNumber)arrayVerticalComponents.get(0)).AsDecimal();
                var displacement = ((ICosNumber)arrayVerticalComponents.get(1)).AsDecimal();

                dw2 = new VerticalVectorComponents(position, displacement);
            }

            // vertical metrics for individual CIDs.
            if (dict.TryGetItemOfType(CosName.W2, out COSArray w2))
            {
                for (var i = 0; i < w2.size(); i++)
                {
                    var c = (ICosNumber)w2.get(i);
                    var next = w2.get(++i);
                    if (next is COSArray array)
                    {
                        for (int j = 0; j < array.size(); j++)
                        {
                            int cid = c.AsInt() + j;
                            var w1y = (ICosNumber)array.get(j);
                            var v1x = (ICosNumber)array.get(++j);
                            var v1y = (ICosNumber)array.get(++j);

                            verticalDisplacements[cid] = w1y.AsDecimal();

                            positionVectors[cid] = new PdfVector(v1x.AsDecimal(), v1y.AsDecimal());
                        }
                    }
                    else
                    {
                        int first = c.AsInt();
                        int last = ((ICosNumber)next).AsInt();
                        var w1y = (ICosNumber)w2.get(++i);
                        var v1x = (ICosNumber)w2.get(++i);
                        var v1y = (ICosNumber)w2.get(++i);

                        for (var cid = first; cid <= last; cid++)
                        {
                            verticalDisplacements[cid] = w1y.AsDecimal();

                            positionVectors[cid] = new PdfVector(v1x.AsDecimal(), v1y.AsDecimal());
                        }
                    }
                }
            }

            return new VerticalWritingMetrics(dw2, verticalDisplacements, positionVectors);
        }

        private CharacterIdentifierSystemInfo GetSystemInfo(PdfDictionary dictionary, IRandomAccessRead reader, bool isLenientParsing)
        {
            if(!dictionary.TryGetValue(CosName.CIDSYSTEMINFO, out var cidEntry))
            {
                throw new InvalidFontFormatException($"No CID System Info was found in the CID Font dictionary: {dictionary}");
            }

            if (cidEntry is PdfDictionary cidDictionary)
            {
                
            }
            else if (cidEntry is CosObject cidObject)
            {
                cidDictionary =
                    DirectObjectFinder.Find<PdfDictionary>(cidObject, pdfObjectParser, reader, isLenientParsing);
            }
            else
            {
                throw new InvalidFontFormatException($"No CID System Info was found in the CID Font dictionary: {dictionary}");
            }

            var registry = (CosString) cidDictionary.GetItemOrDefault(CosName.REGISTRY);
            var ordering = (CosString)cidDictionary.GetItemOrDefault(CosName.ORDERING);
            var supplement = cidDictionary.GetIntOrDefault(CosName.SUPPLEMENT, 0);

            return new CharacterIdentifierSystemInfo(registry.GetAscii(), ordering.GetAscii(), supplement);
        }
    }
}
