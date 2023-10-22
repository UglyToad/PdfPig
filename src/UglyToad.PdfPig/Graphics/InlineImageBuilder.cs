namespace UglyToad.PdfPig.Graphics
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Content;
    using Core;
    using Filters;
    using PdfPig.Core;
    using Tokenization.Scanner;
    using Tokens;

    /// <summary>
    /// Inline Image Builder.
    /// </summary>
    public sealed class InlineImageBuilder
    {
        /// <summary>
        /// Inline image properties.
        /// </summary>
        public IReadOnlyDictionary<NameToken, IToken> Properties { get; set; }

        /// <summary>
        /// Inline image bytes.
        /// </summary>
        public IReadOnlyList<byte> Bytes { get; set; }

        internal InlineImage CreateInlineImage(TransformationMatrix transformationMatrix, ILookupFilterProvider filterProvider,
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

            NameToken colorSpaceName = null;

            if (!isMask)
            {
                colorSpaceName = GetByKeys<NameToken>(NameToken.ColorSpace, NameToken.Cs, false);

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

                    colorSpaceName = firstColorSpaceName;
                }
            }

            var imgDic = new DictionaryToken(Properties ?? new Dictionary<NameToken, IToken>());

            var details = resourceStore.GetColorSpaceDetails(colorSpaceName, imgDic);

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

            var interpolate = GetByKeys<BooleanToken>(NameToken.Interpolate, NameToken.I, false)?.Data ?? false;

            return new InlineImage(bounds, width, height, bitsPerComponent, isMask, renderingIntent, interpolate, decode, Bytes,
                filters,
                imgDic,
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