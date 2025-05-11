namespace UglyToad.PdfPig.XObjects
{
    using System;
    using System.Linq;
    using Content;
    using Core;
    using Filters;
    using Graphics;
    using Graphics.Colors;
    using Graphics.Core;
    using Images;
    using Tokenization.Scanner;
    using Tokens;
    using Util;

    /// <summary>
    /// External Object (XObject) factory.
    /// </summary>
    public static class XObjectFactory
    {
        /// <summary>
        /// Read the XObject image.
        /// </summary>
        public static XObjectImage ReadImage(XObjectContentRecord xObject,
            IPdfTokenScanner pdfScanner,
            ILookupFilterProvider filterProvider,
            IResourceStore resourceStore)
        {
            if (xObject is null)
            {
                throw new ArgumentNullException(nameof(xObject));
            }

            if (xObject.Type != XObjectType.Image)
            {
                throw new InvalidOperationException($"Cannot create an image from an XObject with type: {xObject.Type}.");
            }

            var dictionary = xObject.Stream.StreamDictionary.Resolve(pdfScanner);

            var bounds = xObject.AppliedTransformation.Transform(new PdfRectangle(new PdfPoint(0, 0), new PdfPoint(1, 1)));

            var width = dictionary.GetInt(NameToken.Width);
            var height = dictionary.GetInt(NameToken.Height);

            var isImageMask = dictionary.TryGet(NameToken.ImageMask, out BooleanToken isMaskToken) && isMaskToken.Data;

            XObjectImage? softMaskImage = null;
            if (dictionary.TryGet(NameToken.Smask, pdfScanner, out StreamToken? sMaskToken))
            {
                if (!sMaskToken.StreamDictionary.TryGet(NameToken.Subtype, out NameToken softMaskSubType) || !softMaskSubType.Equals(NameToken.Image))
                {
                    throw new Exception("The SMask dictionary does not contain a 'Subtype' entry, or its value is not 'Image'.");
                }

                if (!sMaskToken.StreamDictionary.TryGet(NameToken.ColorSpace, out NameToken softMaskColorSpace) || !softMaskColorSpace.Equals(NameToken.Devicegray))
                {
                    throw new Exception("The SMask dictionary does not contain a 'ColorSpace' entry, or its value is not 'Devicegray'.");
                }

                if (sMaskToken.StreamDictionary.ContainsKey(NameToken.Mask) || sMaskToken.StreamDictionary.ContainsKey(NameToken.Smask))
                {
                    throw new Exception("The SMask dictionary contains a 'Mask' or 'Smask' entry.");
                }

                XObjectContentRecord softMaskImageRecord = new XObjectContentRecord(XObjectType.Image,
                    sMaskToken,
                    TransformationMatrix.Identity,
                    xObject.DefaultRenderingIntent, // Ignored
                    DeviceGrayColorSpaceDetails.Instance);

                softMaskImage = ReadImage(softMaskImageRecord, pdfScanner, filterProvider, resourceStore);
            }
            else if (dictionary.TryGet(NameToken.Mask, out StreamToken maskStream))
            {
                if (maskStream.StreamDictionary.ContainsKey(NameToken.ColorSpace))
                {
                    throw new Exception("The SMask dictionary contains a 'ColorSpace'.");
                }

                // Stencil masking
                XObjectContentRecord maskImageRecord = new XObjectContentRecord(XObjectType.Image,
                    maskStream,
                    TransformationMatrix.Identity,
                    xObject.DefaultRenderingIntent,
                    null);

                softMaskImage = ReadImage(maskImageRecord, pdfScanner, filterProvider, resourceStore);
                System.Diagnostics.Debug.Assert(softMaskImage.ColorSpaceDetails?.IsStencil == true);
            }

            var isJpxDecode = dictionary.TryGet(NameToken.Filter, out NameToken filterName) && filterName.Equals(NameToken.JpxDecode);

            int bitsPerComponent;
            if (isImageMask)
            {
                bitsPerComponent = 1;
            }
            else
            {
                if (isJpxDecode)
                {
                    // Optional for JPX
                    if (dictionary.TryGet(NameToken.BitsPerComponent, out NumericToken? bitsPerComponentToken))
                    {
                        bitsPerComponent = bitsPerComponentToken.Int;
                        System.Diagnostics.Debug.Assert(bitsPerComponent == Jpeg2000Helper.GetBitsPerComponent(xObject.Stream.Data.Span));
                    }
                    else
                    {
                        bitsPerComponent = Jpeg2000Helper.GetBitsPerComponent(xObject.Stream.Data.Span);
                        System.Diagnostics.Debug.Assert(new int[] { 1, 2, 4, 8, 16 }.Contains(bitsPerComponent));
                    }
                }
                else
                {
                    if (!dictionary.TryGet(NameToken.BitsPerComponent, out NumericToken? bitsPerComponentToken))
                    {
                        throw new PdfDocumentFormatException($"No bits per component defined for image: {dictionary}.");
                    }

                    bitsPerComponent = bitsPerComponentToken.Int;
                }
            }

            var intent = xObject.DefaultRenderingIntent;
            if (dictionary.TryGet(NameToken.Intent, out NameToken renderingIntentToken))
            {
                intent = renderingIntentToken.Data.ToRenderingIntent();
            }

            var interpolate = dictionary.TryGet(NameToken.Interpolate, out BooleanToken? interpolateToken)
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

            var decodedBytes = supportsFilters ? new Lazy<ReadOnlyMemory<byte>>(() => xObject.Stream.Decode(filterProvider, pdfScanner))
                : null;

            var decode = Array.Empty<double>();
            if (dictionary.TryGet(NameToken.Decode, out ArrayToken decodeArrayToken))
            {
                decode = decodeArrayToken.Data.OfType<NumericToken>()
                    .Select(x => x.Double)
                    .ToArray();
            }

            ColorSpaceDetails? details = null;
            if (!isImageMask)
            {
                if (dictionary.TryGet(NameToken.ColorSpace, out NameToken? colorSpaceNameToken))
                {
                    details = resourceStore.GetColorSpaceDetails(colorSpaceNameToken, dictionary);
                }
                else if (dictionary.TryGet(NameToken.ColorSpace, out ArrayToken? colorSpaceArrayToken)
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
                details,
                softMaskImage);
        }
    }
}
