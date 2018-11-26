namespace UglyToad.PdfPig.XObjects
{
    using System;
    using Graphics;
    using Tokenization.Scanner;
    using Tokens;

    internal class XObjectFactory
    {
        public XObjectImage CreateImage(XObjectContentRecord xObject, IPdfTokenScanner pdfScanner, bool isLenientParsing)
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
            
            var isImageMask = xObject.Stream.StreamDictionary.TryGet(NameToken.ImageMask, out var maskToken)
                              && maskToken is BooleanToken maskBoolean
                              && maskBoolean.Data;

            return new XObjectImage(width, height, isJpxDecode, isImageMask, xObject.Stream.StreamDictionary,
                xObject.Stream.Data);
        }
    }
}
