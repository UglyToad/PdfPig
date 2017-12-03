namespace UglyToad.Pdf.Fonts.Parser.Parts
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using CidFonts;
    using ContentStream;
    using Cos;
    using Filters;
    using Geometry;
    using Pdf.Parser;
    using Util;

    internal class CidFontFactory
    {
        private readonly FontDescriptorFactory descriptorFactory;

        public CidFontFactory(FontDescriptorFactory descriptorFactory)
        {
            this.descriptorFactory = descriptorFactory;
        }

        public ICidFont Generate(PdfDictionary dictionary, ParsingArguments arguments, bool isLenientParsing)
        {
            var type = dictionary.GetName(CosName.TYPE);
            if (!CosName.FONT.Equals(type))
            {
                throw new InvalidOperationException($"Expected \'Font\' dictionary but found \'{type.Name}\'");
            }

            var widths = ReadWidths(dictionary);
            var verticalWritingMetrics = ReadVerticalDisplacements(dictionary);

            FontDescriptor descriptor = null;
            if (TryGetFontDescriptor(dictionary, arguments, out var descriptorDictionary))
            {
                descriptor = descriptorFactory.Generate(descriptorDictionary, arguments.IsLenientParsing);
            }

            ReadDescriptorFile(descriptor, arguments);

            var subType = dictionary.GetName(CosName.SUBTYPE);
            if (CosName.CID_FONT_TYPE0.Equals(subType))
            {
                //return new PDCIDFontType0(dictionary, parent);
            }

            if (CosName.CID_FONT_TYPE2.Equals(subType))
            {
                //return new PDCIDFontType2(dictionary, parent);
            }

            return null;
        }

        private static bool TryGetFontDescriptor(PdfDictionary dictionary, ParsingArguments arguments,
            out PdfDictionary descriptorDictionary)
        {
            descriptorDictionary = null;

            if (!dictionary.TryGetValue(CosName.FONT_DESC, out var baseValue) || !(baseValue is CosObject obj))
            {
                return false;
            }

            var descriptorObj = arguments.Get<DynamicParser>().Parse(arguments, obj, false);

            if (!(descriptorObj is PdfDictionary descriptor))
            {
                return false;
            }

            descriptorDictionary = descriptor;

            return true;
        }

        private static void ReadDescriptorFile(FontDescriptor descriptor, ParsingArguments arguments)
        {
            if (descriptor?.FontFile == null)
            {
                return;
            }

            var fontFileStream = arguments.Get<DynamicParser>().Parse(arguments, descriptor.FontFile.ObjectKey, false) as RawCosStream;

            if (fontFileStream == null)
            {
                return;
            }

            var fontFile = fontFileStream.Decode(arguments.Get<IFilterProvider>());
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
    }
}
