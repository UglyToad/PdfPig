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
                var decodeRaw = imageDictionary.GetObjectOrDefault(NameToken.Decode, NameToken.D) as ArrayToken
                    ?? new ArrayToken(EmptyArray<IToken>.Instance);
                var decode = decodeRaw.Data.OfType<NumericToken>().Select(x => x.Data).ToArray();
                return IndexedColorSpaceDetails.Stencil(decode);
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
                    return UnsupportedColorSpaceDetails.Instance;
                case ColorSpace.CalRGB:
                    return UnsupportedColorSpaceDetails.Instance;
                case ColorSpace.Lab:
                    return UnsupportedColorSpaceDetails.Instance;
                case ColorSpace.ICCBased:
                    return UnsupportedColorSpaceDetails.Instance;
                case ColorSpace.Indexed:
                    {
                        if (cannotRecurse)
                        {
                            return UnsupportedColorSpaceDetails.Instance;
                        }

                        if ((!imageDictionary.TryGet(NameToken.ColorSpace, scanner, out ArrayToken colorSpaceArray) &&
                             !imageDictionary.TryGet(NameToken.Cs, scanner, out colorSpaceArray))
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
                        if ((!imageDictionary.TryGet(NameToken.ColorSpace, scanner, out ArrayToken colorSpaceArray) &&
                             !imageDictionary.TryGet(NameToken.Cs, scanner, out colorSpaceArray))
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
    }
}
