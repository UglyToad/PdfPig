namespace UglyToad.PdfPig.Util
{
    using System.Collections.Generic;
    using System.Linq;
    using Content;
    using Core;
    using Filters;
    using Graphics.Colors;
    using Parser.Parts;
    using Tokenization.Scanner;
    using Tokens;

    internal static class ColorSpaceMapper
    {
        private static bool TryExtendedColorSpaceNameMapping(NameToken name, out ColorSpace result)
        {
            result = ColorSpace.DeviceGray;

            switch (name.Data)
            {
                case "G":
                    result = ColorSpace.DeviceGray;
                    return true;
                case "RGB":
                    result = ColorSpace.DeviceRGB;
                    return true;
                case "CMYK":
                    result = ColorSpace.DeviceCMYK;
                    return true;
                case "I":
                    result = ColorSpace.Indexed;
                    return true;
            }

            return false;
        }

        public static bool TryMap(NameToken name, IResourceStore resourceStore, out ColorSpace colorSpaceResult)
        {
            if (name.TryMapToColorSpace(out colorSpaceResult))
            {
                return true;
            }

            if (TryExtendedColorSpaceNameMapping(name, out colorSpaceResult))
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

            if (TryExtendedColorSpaceNameMapping(colorSpaceNamedToken.Name, out colorSpaceResult))
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
                    return UnsupportedColorSpaceDetails.Instance;
                }

                var colorSpaceDetails = GetColorSpaceDetails(colorSpace, imageDictionary.Without(NameToken.Filter).Without(NameToken.F), scanner, resourceStore, filterProvider, true);
                
                var decodeRaw = imageDictionary.GetObjectOrDefault(NameToken.Decode, NameToken.D) as ArrayToken
                    ?? new ArrayToken(EmptyArray<IToken>.Instance);
                var decode = decodeRaw.Data.OfType<NumericToken>().Select(x => x.Data).ToArray();

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

                        if (first == null || !ColorSpaceMapper.TryMap(first, resourceStore, out var innerColorSpace)
                            || innerColorSpace != ColorSpace.CalGray)
                        {
                            return UnsupportedColorSpaceDetails.Instance;
                        }

                        var second = colorSpaceArray[1];

                        // WhitePoint is required
                        if (!DirectObjectFinder.TryGet(second, scanner, out DictionaryToken dictionaryToken) ||
                            !dictionaryToken.TryGet(NameToken.WhitePoint, out ArrayToken whitePointToken))
                        {
                            return UnsupportedColorSpaceDetails.Instance;
                        }

                        var whitePoint = whitePointToken.Data.OfType<NumericToken>().Select(x => x.Data).ToList();

                        // BlackPoint is optional
                        IReadOnlyList<decimal> blackPoint = null;
                        if (dictionaryToken.TryGet(NameToken.BlackPoint, out ArrayToken blackPointToken))
                        {
                            blackPoint = blackPointToken.Data.OfType<NumericToken>().Select(x => x.Data).ToList();
                        }

                        // Gamma is optional
                        decimal? gamma = null;
                        if (dictionaryToken.TryGet(NameToken.Gamma, out NumericToken gammaToken))
                        {
                            gamma = gammaToken.Data;
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

                        if (first == null || !ColorSpaceMapper.TryMap(first, resourceStore, out var innerColorSpace)
                            || innerColorSpace != ColorSpace.CalRGB)
                        {
                            return UnsupportedColorSpaceDetails.Instance;
                        }

                        var second = colorSpaceArray[1];

                        // WhitePoint is required
                        if (!DirectObjectFinder.TryGet(second, scanner, out DictionaryToken dictionaryToken) ||
                            !dictionaryToken.TryGet(NameToken.WhitePoint, out ArrayToken whitePointToken))
                        {
                            return UnsupportedColorSpaceDetails.Instance;
                        }

                        var whitePoint = whitePointToken.Data.OfType<NumericToken>().Select(x => x.Data).ToList();

                        // BlackPoint is optional
                        IReadOnlyList<decimal> blackPoint = null;
                        if (dictionaryToken.TryGet(NameToken.BlackPoint, out ArrayToken blackPointToken))
                        {
                            blackPoint = blackPointToken.Data.OfType<NumericToken>().Select(x => x.Data).ToList();
                        }

                        // Gamma is optional
                        IReadOnlyList<decimal> gamma = null;
                        if (dictionaryToken.TryGet(NameToken.Gamma, out ArrayToken gammaToken))
                        {
                            gamma = gammaToken.Data.OfType<NumericToken>().Select(x => x.Data).ToList();
                        }

                        // Matrix is optional
                        IReadOnlyList<decimal> matrix = null;
                        if (dictionaryToken.TryGet(NameToken.Matrix, out ArrayToken matrixToken))
                        {
                            matrix = matrixToken.Data.OfType<NumericToken>().Select(x => x.Data).ToList();
                        }

                        return new CalRGBColorSpaceDetails(whitePoint, blackPoint, gamma, matrix);
                    }
                case ColorSpace.Lab:
                    return UnsupportedColorSpaceDetails.Instance;
                case ColorSpace.ICCBased:
                    {
                        if (!TryGetColorSpaceArray(imageDictionary, resourceStore, scanner, out var colorSpaceArray)
                            || colorSpaceArray.Length != 2)
                        {
                            return UnsupportedColorSpaceDetails.Instance;
                        }

                        var first = colorSpaceArray[0] as NameToken;

                        if (first == null || !ColorSpaceMapper.TryMap(first, resourceStore, out var innerColorSpace)
                            || innerColorSpace != ColorSpace.ICCBased)
                        {
                            return UnsupportedColorSpaceDetails.Instance;
                        }

                        var second = colorSpaceArray[1];

                        // N is required
                        if (!DirectObjectFinder.TryGet(second, scanner, out StreamToken streamToken) ||
                            !streamToken.StreamDictionary.TryGet(NameToken.N, out NumericToken numeric))
                        {
                            return UnsupportedColorSpaceDetails.Instance;
                        }

                        // Alternate is optional
                        ColorSpaceDetails alternateColorSpaceDetails = null;
                        if (streamToken.StreamDictionary.TryGet(NameToken.Alternate, out NameToken alternateColorSpaceNameToken) &&
                            ColorSpaceMapper.TryMap(alternateColorSpaceNameToken, resourceStore, out var alternateColorSpace))
                        {
                            alternateColorSpaceDetails =
                                GetColorSpaceDetails(alternateColorSpace, imageDictionary, scanner, resourceStore, filterProvider, true);
                        }

                        // Range is optional
                        IReadOnlyList<decimal> range = null;
                        if (streamToken.StreamDictionary.TryGet(NameToken.Range, out ArrayToken arrayToken))
                        {
                            range = arrayToken.Data.OfType<NumericToken>().Select(x => x.Data).ToList();
                        }

                        // Metadata is optional
                        XmpMetadata metadata = null;
                        if (streamToken.StreamDictionary.TryGet(NameToken.Metadata, out StreamToken metadataStream))
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

                        if (first == null || !ColorSpaceMapper.TryMap(first, resourceStore, out var innerColorSpace)
                            || innerColorSpace != ColorSpace.Indexed)
                        {
                            return UnsupportedColorSpaceDetails.Instance;
                        }

                        var second = colorSpaceArray[1];

                        ColorSpaceDetails baseDetails;

                        if (DirectObjectFinder.TryGet(second, scanner, out NameToken baseColorSpaceNameToken)
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
                        else if (DirectObjectFinder.TryGet(second, scanner, out ArrayToken baseColorSpaceArrayToken)
                        && baseColorSpaceArrayToken.Length > 0 && baseColorSpaceArrayToken[0] is NameToken baseColorSpaceArrayNameToken
                        && ColorSpaceMapper.TryMap(baseColorSpaceArrayNameToken, resourceStore, out var baseColorSpaceArrayColorSpace))
                        {
                            var pseudoImageDictionary = new DictionaryToken(
                                new Dictionary<NameToken, IToken>
                                {
                                    {NameToken.ColorSpace, baseColorSpaceArrayToken}
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

                        if (!DirectObjectFinder.TryGet(third, scanner, out NumericToken hiValNumericToken))
                        {
                            return UnsupportedColorSpaceDetails.Instance;
                        }

                        var hival = hiValNumericToken.Int;

                        var fourth = colorSpaceArray[3];

                        IReadOnlyList<byte> tableBytes;

                        if (DirectObjectFinder.TryGet(fourth, scanner, out HexToken tableHexToken))
                        {
                            tableBytes = tableHexToken.Bytes;
                        }
                        else if (DirectObjectFinder.TryGet(fourth, scanner, out StreamToken tableStreamToken))
                        {
                            tableBytes = tableStreamToken.Decode(filterProvider, scanner);
                        }
                        else if (DirectObjectFinder.TryGet(fourth, scanner, out StringToken stringToken))
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
                    return UnsupportedColorSpaceDetails.Instance;
                case ColorSpace.Separation:
                    {
                        if (!TryGetColorSpaceArray(imageDictionary, resourceStore, scanner, out var colorSpaceArray)
                             || colorSpaceArray.Length != 4)
                        {
                            // Error instead?
                            return UnsupportedColorSpaceDetails.Instance;
                        }

                        if (!DirectObjectFinder.TryGet(colorSpaceArray[0], scanner, out NameToken separationColorSpaceNameToken)
                            || !separationColorSpaceNameToken.Equals(NameToken.Separation))
                        {
                            return UnsupportedColorSpaceDetails.Instance;
                        }

                        if (!DirectObjectFinder.TryGet(colorSpaceArray[1], scanner, out NameToken separationNameToken))
                        {
                            return UnsupportedColorSpaceDetails.Instance;
                        }

                        ColorSpaceDetails alternateColorSpaceDetails;
                        if (DirectObjectFinder.TryGet(colorSpaceArray[2], scanner, out NameToken alternateNameToken)
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
                        else if (DirectObjectFinder.TryGet(colorSpaceArray[2], scanner, out ArrayToken alternateArrayToken)
                        && alternateArrayToken.Length > 0
                        && alternateArrayToken[0] is NameToken alternateColorSpaceNameToken
                        && ColorSpaceMapper.TryMap(alternateColorSpaceNameToken, resourceStore, out var alternateArrayColorSpace))
                        {
                            var pseudoImageDictionary = new DictionaryToken(
                                new Dictionary<NameToken, IToken>
                                {
                                {NameToken.ColorSpace, alternateArrayToken}
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

                        Union<DictionaryToken, StreamToken> functionTokensUnion;
                        var func = colorSpaceArray[3];

                        if (DirectObjectFinder.TryGet(func, scanner, out DictionaryToken functionDictionary))
                        {
                            functionTokensUnion = Union<DictionaryToken, StreamToken>.One(functionDictionary);
                        }
                        else if (DirectObjectFinder.TryGet(func, scanner, out StreamToken functionStream))
                        {
                            functionTokensUnion = Union<DictionaryToken, StreamToken>.Two(functionStream);
                        }
                        else
                        {
                            return UnsupportedColorSpaceDetails.Instance;
                        }

                        return new SeparationColorSpaceDetails(separationNameToken, alternateColorSpaceDetails, functionTokensUnion);
                    }
                case ColorSpace.DeviceN:
                    return UnsupportedColorSpaceDetails.Instance;
                default:
                    return UnsupportedColorSpaceDetails.Instance;
            }
        }

        private static bool TryGetColorSpaceArray(DictionaryToken imageDictionary, IResourceStore resourceStore,
            IPdfTokenScanner scanner,
            out ArrayToken colorSpaceArray)
        {
            var colorSpace = imageDictionary.GetObjectOrDefault(NameToken.ColorSpace, NameToken.Cs);

            if (!DirectObjectFinder.TryGet(colorSpace, scanner, out colorSpaceArray)
                && DirectObjectFinder.TryGet(colorSpace, scanner, out NameToken colorSpaceName) &&
                resourceStore.TryGetNamedColorSpace(colorSpaceName, out var colorSpaceNamedToken))
            {
                colorSpaceArray = colorSpaceNamedToken.Data as ArrayToken;
            }

            return colorSpaceArray != null;
        }
    }
}
