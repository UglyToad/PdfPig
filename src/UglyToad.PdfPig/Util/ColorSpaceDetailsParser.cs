namespace UglyToad.PdfPig.Util
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using Content;
    using Filters;
    using Graphics.Colors;
    using Parser.Parts;
    using Tokenization.Scanner;
    using Tokens;
    using UglyToad.PdfPig.Functions;

    internal static class ColorSpaceMapper
    {
        public static bool TryMap(NameToken name, IResourceStore resourceStore, out ColorSpace colorSpaceResult)
        {
            if (name.TryMapToColorSpace(out colorSpaceResult))
            {
                return true;
            }

            if (!resourceStore.TryGetNamedColorSpace(name, out var colorSpaceNamedToken))
            {
                return false;
            }

            if (colorSpaceNamedToken.Name.TryMapToColorSpace(out colorSpaceResult))
            {
                return true;
            }

            return false;
        }
    }

    internal static class ColorSpaceDetailsParser
    {
        public static ColorSpaceDetails GetColorSpaceDetails(ColorSpace? colorSpace,
            DictionaryToken imageDictionary,
            IPdfTokenScanner scanner,
            IResourceStore resourceStore,
            ILookupFilterProvider filterProvider,
            bool cannotRecurse = false)
        {
            if (imageDictionary.GetObjectOrDefault(NameToken.ImageMask, NameToken.Im) != null ||
                filterProvider.GetFilters(imageDictionary, scanner).OfType<CcittFaxDecodeFilter>().Any())
            {
                if (cannotRecurse)
                {
                    // Not sure if always Gray, it's just a single color.
                    return DeviceGrayColorSpaceDetails.Instance;
                }

                var colorSpaceDetails = GetColorSpaceDetails(colorSpace, imageDictionary.Without(NameToken.Filter).Without(NameToken.F), scanner, resourceStore, filterProvider, true);

                var decodeRaw = imageDictionary.GetObjectOrDefault(NameToken.Decode, NameToken.D) as ArrayToken ?? new ArrayToken([]);
                var decode = decodeRaw.Data.OfType<NumericToken>().Select(static x => x.Double).ToArray();

                return IndexedColorSpaceDetails.Stencil(colorSpaceDetails, decode);
            }

            if (!colorSpace.HasValue)
            {
                return UnsupportedColorSpaceDetails.Instance;
            }

            switch (colorSpace.Value)
            {
                case ColorSpace.DeviceGray:
                    return DeviceGrayColorSpaceDetails.Instance;
                case ColorSpace.DeviceRGB:
                    return DeviceRgbColorSpaceDetails.Instance;
                case ColorSpace.DeviceCMYK:
                    return DeviceCmykColorSpaceDetails.Instance;
                case ColorSpace.CalGray:
                    {
                        if (!TryGetColorSpaceArray(imageDictionary, resourceStore, scanner, out var colorSpaceArray)
                                || colorSpaceArray.Length != 2)
                        {
                            return UnsupportedColorSpaceDetails.Instance;
                        }

                        var first = colorSpaceArray[0] as NameToken;

                        if (first is null || !ColorSpaceMapper.TryMap(first, resourceStore, out var innerColorSpace)
                            || innerColorSpace != ColorSpace.CalGray)
                        {
                            return UnsupportedColorSpaceDetails.Instance;
                        }

                        var second = colorSpaceArray[1];

                        // WhitePoint is required
                        if (!DirectObjectFinder.TryGet(second, scanner, out DictionaryToken? dictionaryToken) ||
                            !dictionaryToken.TryGet(NameToken.WhitePoint, scanner, out ArrayToken? whitePointToken))
                        {
                            return UnsupportedColorSpaceDetails.Instance;
                        }

                        var whitePoint = whitePointToken.Data.OfType<NumericToken>().Select(x => x.Double).ToArray();

                        // BlackPoint is optional
                        double[]? blackPoint = null;
                        if (dictionaryToken.TryGet(NameToken.BlackPoint, scanner, out ArrayToken? blackPointToken))
                        {
                            blackPoint = blackPointToken.Data.OfType<NumericToken>().Select(x => x.Double).ToArray();
                        }

                        // Gamma is optional
                        double? gamma = null;
                        if (dictionaryToken.TryGet(NameToken.Gamma, scanner, out NumericToken? gammaToken))
                        {
                            gamma = gammaToken.Double;
                        }

                        return new CalGrayColorSpaceDetails(whitePoint, blackPoint, gamma);
                    }
                case ColorSpace.CalRGB:
                    {
                        if (!TryGetColorSpaceArray(imageDictionary, resourceStore, scanner, out var colorSpaceArray)
                            || colorSpaceArray.Length != 2)
                        {
                            return UnsupportedColorSpaceDetails.Instance;
                        }

                        var first = colorSpaceArray[0] as NameToken;

                        if (first is null || !ColorSpaceMapper.TryMap(first, resourceStore, out var innerColorSpace)
                            || innerColorSpace != ColorSpace.CalRGB)
                        {
                            return UnsupportedColorSpaceDetails.Instance;
                        }

                        var second = colorSpaceArray[1];

                        // WhitePoint is required
                        if (!DirectObjectFinder.TryGet(second, scanner, out DictionaryToken? dictionaryToken) ||
                            !dictionaryToken.TryGet(NameToken.WhitePoint, scanner, out ArrayToken? whitePointToken))
                        {
                            return UnsupportedColorSpaceDetails.Instance;
                        }

                        var whitePoint = whitePointToken.Data.OfType<NumericToken>().Select(x => x.Double).ToArray();

                        // BlackPoint is optional
                        double[]? blackPoint = null;
                        if (dictionaryToken.TryGet(NameToken.BlackPoint, scanner, out ArrayToken? blackPointToken))
                        {
                            blackPoint = blackPointToken.Data.OfType<NumericToken>().Select(static x => x.Double).ToArray();
                        }

                        // Gamma is optional
                        double[]? gamma = null;
                        if (dictionaryToken.TryGet(NameToken.Gamma, scanner, out ArrayToken? gammaToken))
                        {
                            gamma = gammaToken.Data.OfType<NumericToken>().Select(static x => x.Double).ToArray();
                        }

                        // Matrix is optional
                        double[]? matrix = null;
                        if (dictionaryToken.TryGet(NameToken.Matrix, scanner, out ArrayToken? matrixToken))
                        {
                            matrix = matrixToken.Data.OfType<NumericToken>().Select(static x => x.Double).ToArray();
                        }

                        return new CalRGBColorSpaceDetails(whitePoint, blackPoint, gamma, matrix);
                    }
                case ColorSpace.Lab:
                    {
                        if (!TryGetColorSpaceArray(imageDictionary, resourceStore, scanner, out var colorSpaceArray)
                             || colorSpaceArray.Length != 2)
                        {
                            return UnsupportedColorSpaceDetails.Instance;
                        }

                        var first = colorSpaceArray[0] as NameToken;

                        if (first is null || !ColorSpaceMapper.TryMap(first, resourceStore, out var innerColorSpace)
                            || innerColorSpace != ColorSpace.Lab)
                        {
                            return UnsupportedColorSpaceDetails.Instance;
                        }

                        var second = colorSpaceArray[1];

                        // WhitePoint is required
                        if (!DirectObjectFinder.TryGet(second, scanner, out DictionaryToken? dictionaryToken) ||
                            !dictionaryToken.TryGet(NameToken.WhitePoint, scanner, out ArrayToken? whitePointToken))
                        {
                            return UnsupportedColorSpaceDetails.Instance;
                        }

                        var whitePoint = whitePointToken.Data.OfType<NumericToken>().Select(x => x.Double).ToArray();

                        // BlackPoint is optional
                        double[]? blackPoint = null;
                        if (dictionaryToken.TryGet(NameToken.BlackPoint, scanner, out ArrayToken? blackPointToken))
                        {
                            blackPoint = blackPointToken.Data.OfType<NumericToken>().Select(x => x.Double).ToArray();
                        }

                        // Matrix is optional
                        double[]? matrix = null;
                        if (dictionaryToken.TryGet(NameToken.Matrix, scanner, out ArrayToken? matrixToken))
                        {
                            matrix = matrixToken.Data.OfType<NumericToken>().Select(x => x.Double).ToArray();
                        }

                        return new LabColorSpaceDetails(whitePoint, blackPoint, matrix);
                    }
                case ColorSpace.ICCBased:
                    {
                        if (!TryGetColorSpaceArray(imageDictionary, resourceStore, scanner, out var colorSpaceArray)
                            || colorSpaceArray.Length != 2)
                        {
                            return UnsupportedColorSpaceDetails.Instance;
                        }

                        var first = colorSpaceArray[0] as NameToken;

                        if (first is null || !ColorSpaceMapper.TryMap(first, resourceStore, out var innerColorSpace)
                            || innerColorSpace != ColorSpace.ICCBased)
                        {
                            return UnsupportedColorSpaceDetails.Instance;
                        }

                        var second = colorSpaceArray[1];

                        // N is required
                        if (!DirectObjectFinder.TryGet(second, scanner, out StreamToken? streamToken) ||
                            !streamToken.StreamDictionary.TryGet(NameToken.N, scanner, out NumericToken? numeric))
                        {
                            return UnsupportedColorSpaceDetails.Instance;
                        }

                        // Alternate is optional
                        ColorSpaceDetails? alternateColorSpaceDetails = null;
                        if (streamToken.StreamDictionary.TryGet(NameToken.Alternate, out NameToken alternateColorSpaceNameToken) &&
                            ColorSpaceMapper.TryMap(alternateColorSpaceNameToken, resourceStore, out var alternateColorSpace))
                        {
                            alternateColorSpaceDetails =
                                GetColorSpaceDetails(alternateColorSpace, imageDictionary, scanner, resourceStore, filterProvider, true);
                        }

                        // Range is optional
                        double[]? range = null;
                        if (streamToken.StreamDictionary.TryGet(NameToken.Range, scanner, out ArrayToken? arrayToken))
                        {
                            range = arrayToken.Data.OfType<NumericToken>().Select(x => x.Double).ToArray();
                        }

                        // Metadata is optional
                        XmpMetadata? metadata = null;
                        if (streamToken.StreamDictionary.TryGet(NameToken.Metadata, scanner, out StreamToken? metadataStream))
                        {
                            metadata = new XmpMetadata(metadataStream, filterProvider, scanner);
                        }

                        return new ICCBasedColorSpaceDetails(numeric.Int, alternateColorSpaceDetails, range, metadata);
                    }
                case ColorSpace.Indexed:
                    {
                        if (cannotRecurse)
                        {
                            return UnsupportedColorSpaceDetails.Instance;
                        }

                        if (!TryGetColorSpaceArray(imageDictionary, resourceStore, scanner, out var colorSpaceArray)
                            || colorSpaceArray.Length != 4)
                        {
                            // Error instead?
                            return UnsupportedColorSpaceDetails.Instance;
                        }

                        var first = colorSpaceArray[0] as NameToken;

                        if (first is null || !ColorSpaceMapper.TryMap(first, resourceStore, out var innerColorSpace)
                            || innerColorSpace != ColorSpace.Indexed)
                        {
                            return UnsupportedColorSpaceDetails.Instance;
                        }

                        var second = colorSpaceArray[1];

                        ColorSpaceDetails baseDetails;

                        if (DirectObjectFinder.TryGet(second, scanner, out NameToken? baseColorSpaceNameToken)
                            && ColorSpaceMapper.TryMap(baseColorSpaceNameToken, resourceStore, out var baseColorSpaceName))
                        {
                            baseDetails = GetColorSpaceDetails(
                                baseColorSpaceName,
                                imageDictionary,
                                scanner,
                                resourceStore,
                                filterProvider,
                                true);
                        }
                        else if (DirectObjectFinder.TryGet(second, scanner, out ArrayToken? baseColorSpaceArrayToken)
                            && baseColorSpaceArrayToken.Length > 0 && baseColorSpaceArrayToken[0] is NameToken baseColorSpaceArrayNameToken
                            && ColorSpaceMapper.TryMap(baseColorSpaceArrayNameToken, resourceStore, out var baseColorSpaceArrayColorSpace))
                        {
                            var pseudoImageDictionary = new DictionaryToken(
                                new Dictionary<NameToken, IToken>
                                {
                                    { NameToken.ColorSpace, baseColorSpaceArrayToken }
                                });

                            baseDetails = GetColorSpaceDetails(
                                baseColorSpaceArrayColorSpace,
                                pseudoImageDictionary,
                                scanner,
                                resourceStore,
                                filterProvider,
                                true);
                        }
                        else
                        {
                            return UnsupportedColorSpaceDetails.Instance;
                        }

                        if (baseDetails is UnsupportedColorSpaceDetails)
                        {
                            return UnsupportedColorSpaceDetails.Instance;
                        }

                        var third = colorSpaceArray[2];

                        if (!DirectObjectFinder.TryGet(third, scanner, out NumericToken? hiValNumericToken))
                        {
                            return UnsupportedColorSpaceDetails.Instance;
                        }

                        var hival = hiValNumericToken.Int;

                        var fourth = colorSpaceArray[3];

                        byte[] tableBytes;

                        if (DirectObjectFinder.TryGet(fourth, scanner, out HexToken? tableHexToken))
                        {
                            tableBytes = [.. tableHexToken.Bytes];
                        }
                        else if (DirectObjectFinder.TryGet(fourth, scanner, out StreamToken? tableStreamToken))
                        {
                            tableBytes = tableStreamToken.Decode(filterProvider, scanner).Span.ToArray();
                        }
                        else if (DirectObjectFinder.TryGet(fourth, scanner, out StringToken? stringToken))
                        {
                            tableBytes = stringToken.GetBytes();
                        }
                        else
                        {
                            return UnsupportedColorSpaceDetails.Instance;
                        }

                        return new IndexedColorSpaceDetails(baseDetails, (byte)hival, tableBytes);
                    }
                case ColorSpace.Pattern:
                    {
                        if (cannotRecurse)
                        {
                            return UnsupportedColorSpaceDetails.Instance;
                        }

                        ColorSpaceDetails underlyingColourSpace = UnsupportedColorSpaceDetails.Instance;
                        if (imageDictionary.Data.Count > 0)
                        {
                            // Pattern color spaces do not always have dictionary token
                            if (!TryGetColorSpaceArray(imageDictionary, resourceStore, scanner, out var colorSpaceArray)
                                || (colorSpaceArray.Length != 1 && colorSpaceArray.Length != 2))
                            {
                                return UnsupportedColorSpaceDetails.Instance;
                            }

                            if (!DirectObjectFinder.TryGet(colorSpaceArray[0], scanner, out NameToken? patternColorSpaceNameToken)
                                || !patternColorSpaceNameToken.Equals(NameToken.Pattern))
                            {
                                return UnsupportedColorSpaceDetails.Instance;
                            }

                            // Uncoloured Tiling Patterns
                            if (colorSpaceArray.Length > 1 && DirectObjectFinder.TryGet(colorSpaceArray[1], scanner, out NameToken? underlyingCsNameToken)
                                && ColorSpaceMapper.TryMap(underlyingCsNameToken, resourceStore, out var underlyingColorSpaceName))
                            {
                                underlyingColourSpace = GetColorSpaceDetails(
                                    underlyingColorSpaceName,
                                    imageDictionary,
                                    scanner,
                                    resourceStore,
                                    filterProvider,
                                    true);
                            }
                        }
                        return new PatternColorSpaceDetails(resourceStore.GetPatterns(), underlyingColourSpace);
                    }
                case ColorSpace.Separation:
                    {
                        if (!TryGetColorSpaceArray(imageDictionary, resourceStore, scanner, out var colorSpaceArray)
                             || colorSpaceArray.Length != 4)
                        {
                            // Error instead?
                            return UnsupportedColorSpaceDetails.Instance;
                        }

                        if (!DirectObjectFinder.TryGet(colorSpaceArray[0], scanner, out NameToken? separationColorSpaceNameToken)
                            || !separationColorSpaceNameToken.Equals(NameToken.Separation))
                        {
                            return UnsupportedColorSpaceDetails.Instance;
                        }

                        if (!DirectObjectFinder.TryGet(colorSpaceArray[1], scanner, out NameToken? separationNameToken))
                        {
                            return UnsupportedColorSpaceDetails.Instance;
                        }

                        ColorSpaceDetails alternateColorSpaceDetails;
                        if (DirectObjectFinder.TryGet(colorSpaceArray[2], scanner, out NameToken? alternateNameToken)
                            && ColorSpaceMapper.TryMap(alternateNameToken, resourceStore, out var baseColorSpaceName))
                        {
                            alternateColorSpaceDetails = GetColorSpaceDetails(
                                baseColorSpaceName,
                                imageDictionary,
                                scanner,
                                resourceStore,
                                filterProvider,
                                true);
                        }
                        else if (DirectObjectFinder.TryGet(colorSpaceArray[2], scanner, out ArrayToken? alternateArrayToken)
                            && alternateArrayToken.Length > 0
                            && alternateArrayToken[0] is NameToken alternateColorSpaceNameToken
                            && ColorSpaceMapper.TryMap(alternateColorSpaceNameToken, resourceStore, out var alternateArrayColorSpace))
                        {
                            var pseudoImageDictionary = new DictionaryToken(
                                new Dictionary<NameToken, IToken>
                                {
                                    { NameToken.ColorSpace, alternateArrayToken }
                                });

                            alternateColorSpaceDetails = GetColorSpaceDetails(
                                alternateArrayColorSpace,
                                pseudoImageDictionary,
                                scanner,
                                resourceStore,
                                filterProvider,
                                true);
                        }
                        else
                        {
                            return UnsupportedColorSpaceDetails.Instance;
                        }

                        PdfFunction function;
                        var func = colorSpaceArray[3];

                        if (DirectObjectFinder.TryGet(func, scanner, out DictionaryToken? functionDictionary))
                        {
                            function = PdfFunctionParser.Create(functionDictionary, scanner, filterProvider);
                        }
                        else if (DirectObjectFinder.TryGet(func, scanner, out StreamToken? functionStream))
                        {
                            function = PdfFunctionParser.Create(functionStream, scanner, filterProvider);
                        }
                        else
                        {
                            return UnsupportedColorSpaceDetails.Instance;
                        }

                        return new SeparationColorSpaceDetails(separationNameToken, alternateColorSpaceDetails, function);
                    }
                case ColorSpace.DeviceN:
                    {
                        if (!TryGetColorSpaceArray(imageDictionary, resourceStore, scanner, out var colorSpaceArray)
                             || (colorSpaceArray.Length != 4 && colorSpaceArray.Length != 5))
                        {
                            // Error instead?
                            return UnsupportedColorSpaceDetails.Instance;
                        }

                        if (!DirectObjectFinder.TryGet(colorSpaceArray[0], scanner, out NameToken? deviceNColorSpaceNameToken)
                            || !deviceNColorSpaceNameToken.Equals(NameToken.Devicen))
                        {
                            return UnsupportedColorSpaceDetails.Instance;
                        }

                        if (!DirectObjectFinder.TryGet(colorSpaceArray[1], scanner, out ArrayToken? deviceNNamesToken))
                        {
                            return UnsupportedColorSpaceDetails.Instance;
                        }

                        ColorSpaceDetails alternateColorSpaceDetails;
                        if (DirectObjectFinder.TryGet(colorSpaceArray[2], scanner, out NameToken? alternateNameToken)
                            && ColorSpaceMapper.TryMap(alternateNameToken, resourceStore, out var baseColorSpaceName))
                        {
                            alternateColorSpaceDetails = GetColorSpaceDetails(
                                baseColorSpaceName,
                                imageDictionary,
                                scanner,
                                resourceStore,
                                filterProvider,
                                true);
                        }
                        else if (DirectObjectFinder.TryGet(colorSpaceArray[2], scanner, out ArrayToken? alternateArrayToken)
                            && alternateArrayToken.Length > 0
                            && alternateArrayToken[0] is NameToken alternateColorSpaceNameToken
                            && ColorSpaceMapper.TryMap(alternateColorSpaceNameToken, resourceStore, out var alternateArrayColorSpace))
                        {
                            var pseudoImageDictionary = new DictionaryToken(
                                new Dictionary<NameToken, IToken>
                                {
                                    { NameToken.ColorSpace, alternateArrayToken }
                                });

                            alternateColorSpaceDetails = GetColorSpaceDetails(
                                alternateArrayColorSpace,
                                pseudoImageDictionary,
                                scanner,
                                resourceStore,
                                filterProvider,
                                true);
                        }
                        else
                        {
                            return UnsupportedColorSpaceDetails.Instance;
                        }

                        var func = colorSpaceArray[3];
                        PdfFunction tintFunc = PdfFunctionParser.Create(func, scanner, filterProvider);

                        if (colorSpaceArray.Length > 4 && DirectObjectFinder.TryGet(colorSpaceArray[4], scanner, out DictionaryToken? deviceNAttributesToken))
                        {
                            // Optionnal

                            // Subtype - NameToken - Optional - Default value: DeviceN.
                            NameToken subtype = NameToken.Devicen;
                            if (deviceNAttributesToken.ContainsKey(NameToken.Subtype))
                            {
                                subtype = deviceNAttributesToken.Get<NameToken>(NameToken.Subtype, scanner);
                            }

                            // Colorants - dictionary - Required if Subtype is NChannel and the colour space includes spot colorants; otherwise optional
                            DictionaryToken? colorants = null;
                            if (deviceNAttributesToken.ContainsKey(NameToken.Colorants))
                            {
                                colorants = deviceNAttributesToken.Get<DictionaryToken>(NameToken.Colorants, scanner);
                            }

                            // Process - dictionary - Required if Subtype is NChannel and the colour space includes components of a process colour space, otherwise optional; PDF 1.6
                            DictionaryToken? process = null;
                            if (deviceNAttributesToken.ContainsKey(NameToken.Process))
                            {
                                process = deviceNAttributesToken.Get<DictionaryToken>(NameToken.Process, scanner);
                            }

                            // MixingHints - dictionary - Optional
                            DictionaryToken? mixingHints = null;
                            if (deviceNAttributesToken.ContainsKey(NameToken.MixingHints))
                            {
                                mixingHints = deviceNAttributesToken.Get<DictionaryToken>(NameToken.MixingHints, scanner);
                            }

                            var attributes = new DeviceNColorSpaceDetails.DeviceNColorSpaceAttributes(subtype, colorants, process, mixingHints);

                            return new DeviceNColorSpaceDetails(deviceNNamesToken.Data.OfType<NameToken>().ToArray(), alternateColorSpaceDetails, tintFunc, attributes);
                        }

                        return new DeviceNColorSpaceDetails(deviceNNamesToken.Data.OfType<NameToken>().ToArray(), alternateColorSpaceDetails, tintFunc);
                    }
                default:
                    return UnsupportedColorSpaceDetails.Instance;
            }
        }

        private static bool TryGetColorSpaceArray(DictionaryToken imageDictionary, IResourceStore resourceStore,
            IPdfTokenScanner scanner,
            [NotNullWhen(true)] out ArrayToken? colorSpaceArray)
        {
            var colorSpace = imageDictionary.GetObjectOrDefault(NameToken.ColorSpace, NameToken.Cs);

            if (!DirectObjectFinder.TryGet(colorSpace, scanner, out colorSpaceArray)
                && DirectObjectFinder.TryGet(colorSpace, scanner, out NameToken? colorSpaceName) &&
                resourceStore.TryGetNamedColorSpace(colorSpaceName, out var colorSpaceNamedToken))
            {
                colorSpaceArray = colorSpaceNamedToken.Data as ArrayToken;
            }

            return colorSpaceArray != null;
        }
    }
}
