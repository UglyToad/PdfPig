namespace UglyToad.Pdf.Tokenization
{
    using System.Collections.Generic;
    using IO;
    using Parser.Parts;
    using Scanner;
    using Tokens;
    using Util.JetBrains.Annotations;

    public class DictionaryTokenizer : ITokenizer
    {
        public bool ReadsNextByte { get; } = false;

        public bool TryTokenize(byte currentByte, IInputBytes inputBytes, out IToken token)
        {
            token = null;

            if (currentByte != '<')
            {
                return false;
            }

            bool foundNextOpenBrace = false;

            while (inputBytes.MoveNext())
            {
                if (inputBytes.CurrentByte == '<')
                {
                    foundNextOpenBrace = true;
                    break;
                }

                if (!ReadHelper.IsWhitespace(inputBytes.CurrentByte))
                {
                    break;
                }
            }

            if (!foundNextOpenBrace)
            {
                return false;
            }

            var coreScanner = new CoreTokenScanner(inputBytes, ScannerScope.Dictionary);

            var tokens = new List<IToken>();

            while (coreScanner.MoveNext())
            {
                tokens.Add(coreScanner.CurrentToken);
            }

            var dictionary = ConvertToDictionary(tokens);

            token = new DictionaryToken(dictionary);

            return true;
        }

        private static Dictionary<IToken, IToken> ConvertToDictionary(IReadOnlyList<IToken> tokens)
        {
            var result = new Dictionary<IToken, IToken>();

            IToken key = null;
            for (var i = 0; i < tokens.Count; i++)
            {
                var token = tokens[i];

                if (key == null)
                {
                    key = token;
                    continue;
                }

                // Combine indirect references, e.g. 12 0 R
                if (token is NumericToken num && PeekNext(tokens, i) is NumericToken gen)
                {
                    var r = PeekNext(tokens, i + 1);

                    if (r == OperatorToken.R)
                    {
                        result[key] = new IndirectReferenceToken(new IndirectReference(num.Long, gen.Long));
                        i = i + 2;
                    }
                }
                else
                {
                    result[key] = token;
                }

                key = null;
            }

            return result;
        }

        [CanBeNull]
        private static IToken PeekNext(IReadOnlyList<IToken> tokens, int currentIndex)
        {
            if (tokens.Count - 1 < currentIndex + 1)
            {
                return null;
            }

            return tokens[currentIndex + 1];
        }
    }
}
