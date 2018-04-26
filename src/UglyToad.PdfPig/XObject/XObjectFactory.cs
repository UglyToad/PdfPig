namespace UglyToad.PdfPig.XObject
{
    using System;
    using Graphics;
    using Tokenization.Scanner;
    using Tokenization.Tokens;

    internal class XObjectFactory
    {
        public void CreateImage(XObjectContentRecord xObject, IPdfTokenScanner pdfScanner, bool isLenientParsing)
        {
            if (xObject == null)
            {
                throw new ArgumentNullException(nameof(xObject));
            }

            if (xObject.Type != XObjectType.Image)
            {
                throw new InvalidOperationException($"Cannot create an image from an XObject with type: {xObject.Type}.");
            }

            var width = xObject.Stream.StreamDictionary.Get<NumericToken>(NameToken.Width, pdfScanner).Int;
            var height = xObject.Stream.StreamDictionary.Get<NumericToken>(NameToken.Height, pdfScanner).Int;

            var isJpxDecode = xObject.Stream.StreamDictionary.TryGet(NameToken.Filter, out var token) 
                && token is NameToken filterName
                && filterName.Equals(NameToken.JpxDecode);

            if (isJpxDecode)
            {
                return;
            }

            var isImageMask = xObject.Stream.StreamDictionary.TryGet(NameToken.ImageMask, out var maskToken)
                              && maskToken is BooleanToken maskBoolean
                              && maskBoolean.Data;

            if (isImageMask)
            {
                return;
            }

            var bitsPerComponents = xObject.Stream.StreamDictionary.Get<NumericToken>(NameToken.BitsPerComponent, pdfScanner).Int;

        }
    }
}
