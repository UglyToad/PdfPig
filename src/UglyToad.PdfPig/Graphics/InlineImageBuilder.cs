namespace UglyToad.PdfPig.Graphics
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Colors;
    using Content;
    using Core;
    using Filters;
    using PdfPig.Core;
    using Tokenization.Scanner;
    using Tokens;
    using Util;

    internal class InlineImageBuilder
    {
        public IReadOnlyDictionary<NameToken, IToken> Properties { get; set; }

        public IReadOnlyList<byte> Bytes { get; set; }

        public InlineImage CreateInlineImage(TransformationMatrix transformationMatrix, IFilterProvider filterProvider,
            IPdfTokenScanner tokenScanner,
            RenderingIntent defaultRenderingIntent,
            IResourceStore resourceStore)
        {
            if (Properties == null || Bytes == null)
            {
                throw new InvalidOperationException($"Inline image builder not completely defined before calling {nameof(CreateInlineImage)}.");
            }


            
            var bounds = transformationMatrix.Transform(new PdfRectangle(new PdfPoint(1, 1),
                new PdfPoint(0, 0)));

            var width = GetByKeys<NumericToken>(NameToken.Width, NameToken.W, true).Int;

            var height = GetByKeys<NumericToken>(NameToken.Height, NameToken.H, true).Int;

            var maskToken = GetByKeys<BooleanToken>(NameToken.ImageMask, NameToken.Im, false);

            var isMask = maskToken?.Data == true;

            var bitsPerComponent = GetByKeys<NumericToken>(NameToken.BitsPerComponent, NameToken.Bpc, !isMask)?.Int ?? 1;

            var colorSpace = default(ColorSpace?);

            if (!isMask)
            {
                var colorSpaceName = GetByKeys<NameToken>(NameToken.ColorSpace, NameToken.Cs, false);

                if (colorSpaceName == null)
                {
                    var colorSpaceArray = GetByKeys<ArrayToken>(NameToken.ColorSpace, NameToken.Cs, true);

                    if (colorSpaceArray.Length == 0)
                    {
                        throw new PdfDocumentFormatException("Empty ColorSpace array defined for inline image.");
                    }

                    if (!(colorSpaceArray.Data[0] is NameToken firstColorSpaceName))
                    {
                        throw new PdfDocumentFormatException($"Invalid ColorSpace array defined for inline image: {colorSpaceArray}.");
                    }

                    if (!ColorSpaceMapper.TryMap(firstColorSpaceName, resourceStore, out var colorSpaceMapped))
                    {
                        throw new PdfDocumentFormatException($"Invalid ColorSpace defined for inline image: {firstColorSpaceName}.");
                    }

                    colorSpace = colorSpaceMapped;
                }
                else
                {
                    if (!ColorSpaceMapper.TryMap(colorSpaceName, resourceStore, out var colorSpaceMapped))
                    {
                        throw new PdfDocumentFormatException($"Invalid ColorSpace defined for inline image: {colorSpaceName}.");
                    }

                    colorSpace = colorSpaceMapped;
                }
            }

            var imgDic = new DictionaryToken(Properties ?? new Dictionary<NameToken, IToken>());

            var details = ColorSpaceDetailsParser.GetColorSpaceDetails(
                colorSpace,
                imgDic,
                tokenScanner,
                resourceStore,
                filterProvider);

            var renderingIntent = GetByKeys<NameToken>(NameToken.Intent, null, false)?.Data?.ToRenderingIntent() ?? defaultRenderingIntent;

            var filterNames = new List<NameToken>();

            var filterName = GetByKeys<NameToken>(NameToken.Filter, NameToken.F, false);

            if (filterName == null)
            {
                var filterArray = GetByKeys<ArrayToken>(NameToken.Filter, NameToken.F, false);

                if (filterArray != null)
                {
                    filterNames.AddRange(filterArray.Data.OfType<NameToken>());
                }
            }
            else
            {
                filterNames.Add(filterName);
            }

            var filters = filterProvider.GetNamedFilters(filterNames);

            var decodeRaw = GetByKeys<ArrayToken>(NameToken.Decode, NameToken.D, false) ?? new ArrayToken(EmptyArray<IToken>.Instance);

            var decode = decodeRaw.Data.OfType<NumericToken>().Select(x => x.Data).ToArray();

            var filterDictionaryEntries = new Dictionary<NameToken, IToken>();
            var decodeParamsDict = GetByKeys<DictionaryToken>(NameToken.DecodeParms, NameToken.Dp, false);

            if (decodeParamsDict == null)
            {
                var decodeParamsArray = GetByKeys<ArrayToken>(NameToken.DecodeParms, NameToken.Dp, false);

                if (decodeParamsArray != null)
                {
                    filterDictionaryEntries[NameToken.DecodeParms] = decodeParamsArray;
                }
            }
            else
            {
                filterDictionaryEntries[NameToken.DecodeParms] = decodeParamsDict;
            }

            var streamDictionary = new DictionaryToken(filterDictionaryEntries);

            var interpolate = GetByKeys<BooleanToken>(NameToken.Interpolate, NameToken.I, false)?.Data ?? false;

            return new InlineImage(bounds, width, height, bitsPerComponent, isMask, renderingIntent, interpolate, colorSpace, decode, Bytes,
                filters,
                streamDictionary,
                details);
        }

        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
        private T GetByKeys<T>(NameToken name1, NameToken name2, bool required) where T : IToken
        {
            if (Properties.TryGetValue(name1, out var val) && val is T result)
            {
                return result;
            }

            if (name2 != null)
            {
                if (Properties.TryGetValue(name2, out val) && val is T result2)
                {
                    return result2;
                }
            }

            if (required)
            {
                throw new PdfDocumentFormatException($"Inline image dictionary missing required entry {name1}/{name2}.");
            }

            return default(T);
        }
    }
}