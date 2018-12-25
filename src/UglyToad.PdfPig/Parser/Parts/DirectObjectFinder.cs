namespace UglyToad.PdfPig.Parser.Parts
{
    using Exceptions;
    using Tokenization.Scanner;
    using Tokens;

    internal static class DirectObjectFinder
    {
        public static bool TryGet<T>(IToken token, IPdfTokenScanner scanner, out T tokenResult) where T : IToken
        {
            tokenResult = default(T);
            if (token is T t)
            {
                tokenResult = t;
                return true;
            }

            if (!(token is IndirectReferenceToken reference))
            {
                return false;
            }

            var temp = scanner.Get(reference.Data);

            if (temp.Data is T tTemp)
            {
                tokenResult = tTemp;
                return true;
            }

            if (temp.Data is IndirectReferenceToken nestedReferenceToken)
            {
                return TryGet(nestedReferenceToken, scanner, out tokenResult);
            }

            return false;
        }

        public static T Get<T>(IndirectReference reference, IPdfTokenScanner scanner) where T : IToken
        {
            var temp = scanner.Get(reference);

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

            throw new PdfDocumentFormatException($"Could not find the object number {reference} with type {typeof(T).Name}.");
        }

        public static T Get<T>(IToken token, IPdfTokenScanner scanner) where T : IToken
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
