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

    internal static class XObjectFactory
    {
        public static XObjectImage ReadImage(XObjectContentRecord xObject, IPdfTokenScanner pdfScanner,
            IFilterProvider filterProvider,
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

            var dictionary = xObject.Stream.StreamDictionary;

            var bounds = xObject.AppliedTransformation.Transform(new PdfRectangle(new PdfPoint(0, 0), new PdfPoint(1, 1)));

            var width = dictionary.Get<NumericToken>(NameToken.Width, pdfScanner).Int;
            var height = dictionary.Get<NumericToken>(NameToken.Height, pdfScanner).Int;

            var isImageMask = dictionary.TryGet(NameToken.ImageMask, pdfScanner, out BooleanToken isMaskToken)
                         && isMaskToken.Data;

            var isJpxDecode = dictionary.TryGet(NameToken.Filter, out var token)
                && token is NameToken filterName
                && filterName.Equals(NameToken.JpxDecode);

            int bitsPerComponent = 0;
            if (!isImageMask && !isJpxDecode)
            {
                if (!dictionary.TryGet(NameToken.BitsPerComponent, pdfScanner, out NumericToken bitsPerComponentToken))
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

            var interpolate = dictionary.TryGet(NameToken.Interpolate, pdfScanner, out BooleanToken interpolateToken)
                              && interpolateToken.Data;

            var decodedBytes = new Lazy<IReadOnlyList<byte>>(() => xObject.Stream.Decode(filterProvider));

            var decode = EmptyArray<decimal>.Instance;

            if (dictionary.TryGet(NameToken.Decode, pdfScanner, out ArrayToken decodeArrayToken))
            {
                decode = decodeArrayToken.Data.OfType<NumericToken>()
                    .Select(x => x.Data)
                    .ToArray();
            }

            var colorSpace = default(ColorSpace?);

            if (!isImageMask)
            {
                if (dictionary.TryGet(NameToken.ColorSpace, pdfScanner, out NameToken colorSpaceNameToken)
                    && TryMapColorSpace(colorSpaceNameToken, resourceStore, out var colorSpaceResult))
                {
                    colorSpace = colorSpaceResult;
                }
                else if (dictionary.TryGet(NameToken.ColorSpace, pdfScanner, out ArrayToken colorSpaceArrayToken)
                && colorSpaceArrayToken.Length > 0)
                {
                    var first = colorSpaceArrayToken.Data[0];

                    if ((first is NameToken firstColorSpaceName) && TryMapColorSpace(firstColorSpaceName, resourceStore, out colorSpaceResult))
                    {
                        colorSpace = colorSpaceResult;
                    }
                }
                else if (!isJpxDecode)
                {
                    colorSpace = xObject.DefaultColorSpace;
                }
            }

            return new XObjectImage(bounds, width, height, bitsPerComponent, colorSpace, isJpxDecode, isImageMask, intent, interpolate, decode,
                dictionary, xObject.Stream.Data, decodedBytes);
        }

        private static bool TryMapColorSpace(NameToken name, IResourceStore resourceStore, out ColorSpace colorSpaceResult)
        {
            if (name.TryMapToColorSpace(out colorSpaceResult))
            {
                return true;
            }

            if (!resourceStore.TryGetNamedColorSpace(name, out var colorSpaceNamedToken))
            {
                return false;
            }

            return colorSpaceNamedToken.Name.TryMapToColorSpace(out colorSpaceResult);
        }
    }
}
