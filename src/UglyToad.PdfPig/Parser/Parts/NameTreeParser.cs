namespace UglyToad.PdfPig.Parser.Parts
{
    using System;
    using System.Collections.Generic;
    using Exceptions;
    using Tokenization.Scanner;
    using Tokens;

    internal static class NameTreeParser
    {
        public static IReadOnlyDictionary<string, TResult> FlattenNameTreeToDictionary<TResult>(DictionaryToken nameTreeNodeDictionary,
            IPdfTokenScanner pdfScanner,
            bool isLenientParsing,
            Func<IToken, TResult> valuesFactory) where TResult : class
        {
            var result = new Dictionary<string, TResult>();

            FlattenNameTree(nameTreeNodeDictionary, pdfScanner, isLenientParsing, valuesFactory, result);

            return result;
        }

        public static void FlattenNameTree<TResult>(DictionaryToken nameTreeNodeDictionary,
            IPdfTokenScanner pdfScanner,
            bool isLenientParsing,
            Func<IToken, TResult> valuesFactory,
            Dictionary<string, TResult> result) where TResult : class
        {
            if (nameTreeNodeDictionary.TryGet(NameToken.Names, pdfScanner, out ArrayToken nodeNames))
            {
                for (var i = 0; i < nodeNames.Length; i += 2)
                {
                    if (!(nodeNames[i] is IDataToken<string> key))
                    {
                        continue;
                    }
                    
                    var valueToken = nodeNames[i + 1];

                    var value = valuesFactory(valueToken);

                    if (value != null)
                    {
                        result[key.Data] = value;
                    }
                }
            }

            if (nameTreeNodeDictionary.TryGet(NameToken.Kids, pdfScanner, out ArrayToken kids))
            {
                foreach (var kid in kids.Data)
                {
                    if (DirectObjectFinder.TryGet(kid, pdfScanner, out DictionaryToken kidDictionary))
                    {
                        FlattenNameTree(kidDictionary, pdfScanner, isLenientParsing, valuesFactory, result);
                    }
                    else if (!isLenientParsing)
                    {
                        throw new PdfDocumentFormatException($"Invalid kids entry in PDF name tree: {kid} in {kids}.");
                    }
                }
            }
        }
    }
}
