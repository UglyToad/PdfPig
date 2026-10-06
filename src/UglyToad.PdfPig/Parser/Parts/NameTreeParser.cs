#nullable disable

namespace UglyToad.PdfPig.Parser.Parts
{
    using System;
    using System.Collections.Generic;
    using Core;
    using Tokenization.Scanner;
    using Tokens;

    internal static class NameTreeParser
    {
        public static IReadOnlyDictionary<string, TResult> FlattenNameTreeToDictionary<TResult>(
            DictionaryToken nameTreeNodeDictionary,
            IPdfTokenScanner pdfScanner,
            Func<IToken, TResult> valuesFactory) where TResult : class
        {
            var result = new Dictionary<string, TResult>();

            FlattenNameTree(nameTreeNodeDictionary, pdfScanner, valuesFactory, result);

            return result;
        }

        /// <summary>
        /// Walks a name tree iteratively. Each indirectly referenced node is visited at most once, so a
        /// cyclic /Kids entry (a node listing itself or an ancestor) terminates instead of recursing until
        /// the process dies with a non-catchable stack overflow.
        /// </summary>
        public static void FlattenNameTree<TResult>(
            DictionaryToken nameTreeNodeDictionary,
            IPdfTokenScanner pdfScanner,
            Func<IToken, TResult> valuesFactory,
            Dictionary<string, TResult> result) where TResult : class
        {
            var visited = new HashSet<IndirectReference>();
            var pending = new Stack<DictionaryToken>();
            pending.Push(nameTreeNodeDictionary);

            while (pending.Count > 0)
            {
                var node = pending.Pop();

                if (node.TryGet(NameToken.Names, pdfScanner, out ArrayToken nodeNames))
                {
                    for (var i = 0; i + 1 < nodeNames.Length; i += 2)
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

                if (!node.TryGet(NameToken.Kids, pdfScanner, out ArrayToken kids))
                {
                    continue;
                }

                // Push in reverse so the kids are processed in document order, as the recursive version did.
                for (var i = kids.Length - 1; i >= 0; i--)
                {
                    var kid = kids[i];

                    if (kid is IndirectReferenceToken reference && !visited.Add(reference.Data))
                    {
                        continue;
                    }

                    if (DirectObjectFinder.TryGet(kid, pdfScanner, out DictionaryToken kidDictionary))
                    {
                        pending.Push(kidDictionary);
                    }
                }
            }
        }
    }
}
