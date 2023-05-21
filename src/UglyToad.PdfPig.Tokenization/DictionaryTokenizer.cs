namespace UglyToad.PdfPig.Tokenization
{
    using System.Collections.Generic;
    using Core;
    using Scanner;
    using Tokens;

    internal class DictionaryTokenizer : ITokenizer
    {
        private readonly IReadOnlyList<NameToken> requiredKeys;

        public bool ReadsNextByte { get; } = false;

        /// <summary>
        /// Create a new <see cref="DictionaryTokenizer"/>.
        /// </summary>
        /// <param name="requiredKeys">
        /// Can be provided to recover from errors with missing dictionary end symbols if the
        /// set of keys expected in the dictionary are known.
        /// </param>
        public DictionaryTokenizer(IReadOnlyList<NameToken> requiredKeys = null)
        {
            this.requiredKeys = requiredKeys;
        }

        public bool TryTokenize(byte currentByte, IInputBytes inputBytes, out IToken token)
        {
            var start = inputBytes.CurrentOffset;

            try
            {
                return TryTokenizeInternal(currentByte, inputBytes, false, out token);
            }
            catch (PdfDocumentFormatException)
            {
                // Cannot attempt inferred end.
                if (requiredKeys == null)
                {
                    throw;
                }
            }

            inputBytes.Seek(start);

            return TryTokenizeInternal(currentByte, inputBytes, true, out token);
        }

        private bool TryTokenizeInternal(byte currentByte, IInputBytes inputBytes, bool useRequiredKeys, out IToken token)
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
                if (coreScanner.CurrentToken is CommentToken)
                {
                    continue;
                }

                tokens.Add(coreScanner.CurrentToken);

                // Has enough key/values for each required key
                if (useRequiredKeys && tokens.Count >= requiredKeys.Count * 2)
                {
                    var proposedDictionary = ConvertToDictionary(tokens);

                    var isAcceptable = true;
                    foreach (var key in requiredKeys)
                    {
                        if (!proposedDictionary.TryGetValue(key, out var tok) || tok == null)
                        {
                            isAcceptable = false;
                            break;
                        }
                    }

                    // If each required key has a value and we're here because parsing broke previously then return
                    // this dictionary.
                    if (isAcceptable)
                    {
                        token = new DictionaryToken(proposedDictionary);
                        return true;
                    }
                }
            }

            var dictionary = ConvertToDictionary(tokens);

            token = new DictionaryToken(dictionary);

            return true;

        }

        private static Dictionary<NameToken, IToken> ConvertToDictionary(List<IToken> tokens)
        {
            var result = new Dictionary<NameToken, IToken>();

            NameToken key = null;
            for (var i = 0; i < tokens.Count; i++)
            {
                var token = tokens[i];

                if (key == null)
                {
                    if (token is NameToken name)
                    {
                        key = name;
                        continue;
                    }

                    throw new PdfDocumentFormatException($"Expected name as dictionary key, instead got: " + token);
                }

                // Combine indirect references, e.g. 12 0 R
                if (token is NumericToken num && PeekNext(tokens, i) is NumericToken gen)
                {
                    var r = PeekNext(tokens, i + 1);

                    if (r == OperatorToken.R)
                    {
                        result[key] = new IndirectReferenceToken(new IndirectReference(num.Long, gen.Int));
                        i = i + 2;
                    }
                }
                else
                {
                    result[key] = token;
                }

                // skip def.
                if (PeekNext(tokens, i) == OperatorToken.Def)
                {
                    i++;
                }

                key = null;
            }

            return result;
        }

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
