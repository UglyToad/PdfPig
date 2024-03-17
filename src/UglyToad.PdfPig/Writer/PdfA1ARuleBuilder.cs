using System.Collections.Generic;
using UglyToad.PdfPig.Tokens;

namespace UglyToad.PdfPig.Writer
{
    internal static class PdfA1ARuleBuilder
    {
        public static void Obey(Dictionary<NameToken, IToken> catalog)
        {
            var structTreeRoot = GenerateStructTree();

            catalog[NameToken.StructTreeRoot] = structTreeRoot;

            var markInfoDictionary = new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                {NameToken.Marked, BooleanToken.True}
            });

            catalog[NameToken.MarkInfo] = markInfoDictionary;
        }

        private static DictionaryToken GenerateStructTree()
        {
            var rootDictionary = new Dictionary<NameToken, IToken>
            {
                {NameToken.Type, NameToken.StructTreeRoot}
            };

            var structTreeRoot = new DictionaryToken(rootDictionary);

            return structTreeRoot;
        }
    }
}
