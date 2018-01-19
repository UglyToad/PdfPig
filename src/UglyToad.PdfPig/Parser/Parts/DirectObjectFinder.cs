namespace UglyToad.PdfPig.Parser.Parts
{
    using System;
    using Cos;
    using Exceptions;
    using IO;
    using Tokenization.Scanner;
    using Tokenization.Tokens;

    internal static class DirectObjectFinder
    {
        public static T Find<T>(CosObject baseObject, IPdfObjectParser parser, IRandomAccessRead reader,
            bool isLenientParsing) where T : CosBase
        {
            var result = parser.Parse(baseObject.ToIndirectReference(), reader, isLenientParsing);

            if (result is T resultT)
            {
                return resultT;
            }

            if (result is CosObject obj)
            {
                return Find<T>(obj, parser, reader, isLenientParsing);
            }

            if (result is COSArray arr && arr.Count == 1 && arr.get(0) is CosObject arrayObject)
            {
                return Find<T>(arrayObject, parser, reader, isLenientParsing);
            }

            throw new InvalidOperationException($"Could not find the object {baseObject.ToIndirectReference()} with type {typeof(T).Name}.");
        }

        public static T Get<T>(IToken token, IPdfObjectScanner scanner) where T : IToken
        {
            if (token is T result)
            {
                return result;
            }

            if (token is IndirectReferenceToken reference)
            {
                var temp = scanner.Get(reference.Data);

                if (temp.Data is T locatedResult)
                {
                    return locatedResult;
                }

                if (temp.Data is IndirectReferenceToken nestedReference)
                {
                    return Get<T>(nestedReference, scanner);
                }

                if (temp.Data is ArrayToken array && array.Data.Count == 1)
                {
                    var arrayElement = array.Data[0];

                    if (arrayElement is IndirectReferenceToken arrayReference)
                    {
                        return Get<T>(arrayReference, scanner);
                    }

                    if (arrayElement is T arrayToken)
                    {
                        return arrayToken;
                    }
                }
            }

            throw new PdfDocumentFormatException($"Could not find the object {token} with type {typeof(T).Name}.");
        }
    }
}
