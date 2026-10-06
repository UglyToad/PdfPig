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
                var dict = new Dictionary<NameToken, StreamToken>();
                foreach (var entry in stateDictionary.Entries)
                {
                    if (entry.Value is IndirectReferenceToken appearanceRef)
                    {
                        var streamToken = tokenScanner.Get(appearanceRef.Data)?.Data as StreamToken;
                        dict[entry.Key] = streamToken!;
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
