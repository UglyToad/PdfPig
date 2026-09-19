namespace UglyToad.PdfPig.Tokenization
{
    using System.Collections.Generic;
    using Core;
    using Scanner;
    using Tokens;

    internal class DictionaryTokenizer : ITokenizer
    {
        private readonly bool usePdfDocEncoding;
        private readonly IReadOnlyList<NameToken> requiredKeys;
        private readonly bool useLenientParsing;
        private readonly StackDepthGuard stackDepthGuard;

        public bool ReadsNextByte { get; } = false;

        /// <summary>
        /// Create a new <see cref="DictionaryTokenizer"/>.
        /// </summary>
        /// <param name="usePdfDocEncoding">
        /// Whether to read strings using the PdfDocEncoding.
        /// </param>
        /// <param name="stackDepthGuard"></param>
        /// <param name="requiredKeys">
        /// Can be provided to recover from errors with missing dictionary end symbols if the
        /// set of keys expected in the dictionary are known.
        /// </param>
        /// <param name="useLenientParsing">Whether to use lenient parsing.</param>
        public DictionaryTokenizer(bool usePdfDocEncoding, StackDepthGuard stackDepthGuard, IReadOnlyList<NameToken> requiredKeys = null, bool useLenientParsing = false)
        {
            this.usePdfDocEncoding = usePdfDocEncoding;
            this.stackDepthGuard = stackDepthGuard;
            this.requiredKeys = requiredKeys;
            this.useLenientParsing = useLenientParsing;
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

            var coreScanner = new CoreTokenScanner(inputBytes,  usePdfDocEncoding, stackDepthGuard, ScannerScope.Dictionary, useLenientParsing: useLenientParsing);

            // An entry is a key and a value, so two tokens. Three quarters of the dictionaries in a
            // file hold eight entries or fewer and the median holds five, so 16 fits them without a
            // growth, where the list would otherwise grow 4, 8, 16 for every dictionary in the file.
            var tokens = new List<IToken>(16);

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
                    var proposedDictionary = ConvertToDictionary(tokens, useLenientParsing);

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

            var dictionary = ConvertToDictionary(tokens, useLenientParsing);

            token = new DictionaryToken(dictionary);

            return true;
        }

        private static Dictionary<NameToken, IToken> ConvertToDictionary(List<IToken> tokens, bool useLenientParsing)
        {
            // An entry needs at least two tokens, and three when its value is an indirect reference,
            // so half the token count is never too small and the dictionary is built without a resize.
            var result = new Dictionary<NameToken, IToken>(tokens.Count / 2);

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

                    if (useLenientParsing)
                    {
                        // TODO - Log warning
                        System.Diagnostics.Debug.WriteLine($"Expected name as dictionary key, instead got: " + token);
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

        private static IToken PeekNext(List<IToken> tokens, int currentIndex)
        {
            if (tokens.Count - 1 < currentIndex + 1)
            {
                return null;
            }

            return tokens[currentIndex + 1];
        }
    }
}
