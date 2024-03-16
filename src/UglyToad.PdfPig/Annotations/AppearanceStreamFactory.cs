namespace UglyToad.PdfPig.Annotations
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using Tokenization.Scanner;
    using Tokens;

    internal static class AppearanceStreamFactory
    {
        public static bool TryCreate(DictionaryToken appearanceDictionary, NameToken name, IPdfTokenScanner tokenScanner, [NotNullWhen(true)] out AppearanceStream? appearanceStream)
        {
            if (appearanceDictionary.TryGet(name, out IndirectReferenceToken appearanceReference))
            {
                var streamToken = tokenScanner.Get(appearanceReference.Data)?.Data as StreamToken;
                appearanceStream = new AppearanceStream(streamToken);
                return true;
            }

            if (appearanceDictionary.TryGet(name, out DictionaryToken stateDictionary))
            {
                var dict = new Dictionary<string, StreamToken>();
                foreach (var state in stateDictionary.Data.Keys)
                {
                    if (stateDictionary.Data.TryGetValue(state, out var stateRef) &&
                        stateRef is IndirectReferenceToken appearanceRef)
                    {
                        var streamToken = tokenScanner.Get(appearanceRef.Data)?.Data as StreamToken;
                        dict[state] = streamToken!;
                    }
                }

                if (dict.Count > 0)
                {
                    appearanceStream = new AppearanceStream(dict);
                    return true;
                }
            }

            appearanceStream = null;
            return false;
        }
    }
}
