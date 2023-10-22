namespace UglyToad.PdfPig.XObjects
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Content;
    using Core;
    using Filters;
    using Graphics;
    using Graphics.Colors;
    using Graphics.Core;
    using Tokenization.Scanner;
    using Tokens;
    using Util;

    internal static class XObjectFactory
    {
        public static XObjectImage ReadImage(XObjectContentRecord xObject, IPdfTokenScanner pdfScanner,
            ILookupFilterProvider filterProvider,
            IResourceStore resourceStore)
        {
            if (xObject == null)
            {
                throw new ArgumentNullException(nameof(xObject));
            }

            if (xObject.Type != XObjectType.Image)
            {
                throw new InvalidOperationException($"Cannot create an image from an XObject with type: {xObject.Type}.");
            }

            var dictionary = xObject.Stream.StreamDictionary.Resolve(pdfScanner);

            var bounds = xObject.AppliedTransformation.Transform(new PdfRectangle(new PdfPoint(0, 0), new PdfPoint(1, 1)));

            var width = dictionary.Get<NumericToken>(NameToken.Width, pdfScanner).Int;
            var height = dictionary.Get<NumericToken>(NameToken.Height, pdfScanner).Int;

            var isImageMask = dictionary.TryGet(NameToken.ImageMask, out BooleanToken isMaskToken)
                && isMaskToken.Data;

            var isJpxDecode = dictionary.TryGet(NameToken.Filter, out var token)
                && token is NameToken filterName
                && filterName.Equals(NameToken.JpxDecode);

            int bitsPerComponent = 0;
            if (!isImageMask && !isJpxDecode)
            {
                if (!dictionary.TryGet(NameToken.BitsPerComponent, out NumericToken bitsPerComponentToken))
                {
                    throw new PdfDocumentFormatException($"No bits per component defined for image: {dictionary}.");
                }

                bitsPerComponent = bitsPerComponentToken.Int;
            }
            else if (isImageMask)
            {
                bitsPerComponent = 1;
            }

            var intent = xObject.DefaultRenderingIntent;
            if (dictionary.TryGet(NameToken.Intent, out NameToken renderingIntentToken))
            {
                intent = renderingIntentToken.Data.ToRenderingIntent();
            }

            var interpolate = dictionary.TryGet(NameToken.Interpolate, out BooleanToken interpolateToken)
                              && interpolateToken.Data;


            var supportsFilters = true;

            var filters = filterProvider.GetFilters(dictionary, pdfScanner);
            foreach (var filter in filters)
            {
                if (!filter.IsSupported)
                {
                    supportsFilters = false;
                    break;
                }
            }

            var streamToken = new StreamToken(dictionary, xObject.Stream.Data);

            var decodedBytes = supportsFilters ? new Lazy<IReadOnlyList<byte>>(() => streamToken.Decode(filterProvider, pdfScanner))
                : null;

            var decode = EmptyArray<decimal>.Instance;

            if (dictionary.TryGet(NameToken.Decode, pdfScanner, out ArrayToken decodeArrayToken))
            {
                decode = decodeArrayToken.Data.OfType<NumericToken>()
                    .Select(x => x.Data)
                    .ToArray();
            }

            ColorSpaceDetails details = null;
            if (!isImageMask)
            {
                if (dictionary.TryGet(NameToken.ColorSpace, pdfScanner, out NameToken colorSpaceNameToken))
                {
                    details = resourceStore.GetColorSpaceDetails(colorSpaceNameToken, dictionary);
                }
                else if (dictionary.TryGet(NameToken.ColorSpace, pdfScanner, out ArrayToken colorSpaceArrayToken)
                    && colorSpaceArrayToken.Length > 0 && colorSpaceArrayToken.Data[0] is NameToken firstColorSpaceName)
                {
                    details = resourceStore.GetColorSpaceDetails(firstColorSpaceName, dictionary);
                }
                else if (!isJpxDecode)
                {
                    details = xObject.DefaultColorSpace;
                }
            }
            else
            {
                details = resourceStore.GetColorSpaceDetails(null, dictionary);
            }

            return new XObjectImage(
                bounds,
                width,
                height,
                bitsPerComponent,
                isJpxDecode,
                isImageMask,
                intent,
                interpolate,
                decode,
                dictionary,
                xObject.Stream.Data,
                decodedBytes,
                details);
        }
    }
}
