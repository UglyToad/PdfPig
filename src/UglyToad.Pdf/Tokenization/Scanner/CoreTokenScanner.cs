namespace UglyToad.Pdf.Tokenization.Scanner
{
    using System;
    using System.Collections.Generic;
    using IO;
    using Parser.Parts;
    using Text.Operators;
    using Tokenization;
    using Tokens;

    public class CoreTokenScanner : ITokenScanner
    {
        private readonly CosDictionaryParser dictionaryParser;
        private readonly CosArrayParser arrayParser;
        private readonly IInputBytes inputBytes;
        private readonly List<byte> currentBuffer = new List<byte>();

        private static readonly HexTokenizer HexTokenizer = new HexTokenizer();
        private static readonly StringTokenizer StringTokenizer = new StringTokenizer();
        private static readonly Tokenization.NumericTokenizer NumericTokenizer = new Tokenization.NumericTokenizer();
        private static readonly NameTokenizer NameTokenizer = new NameTokenizer();
        private static readonly PlainTokenizer PlainTokenizer = new PlainTokenizer();
        
        public IToken CurrentToken { get; private set; }

        private bool hasBytePreRead;

        internal CoreTokenScanner(IInputBytes inputBytes, CosDictionaryParser dictionaryParser,
            CosArrayParser arrayParser)
        {
            this.dictionaryParser = dictionaryParser;
            this.arrayParser = arrayParser;
            this.inputBytes = inputBytes ?? throw new ArgumentNullException(nameof(inputBytes));
        }

        public bool MoveNext()
        {
            currentBuffer.Clear();

            bool isSkippingSymbol = false;
            while ((hasBytePreRead && !inputBytes.IsAtEnd()) || inputBytes.MoveNext())
            {
                hasBytePreRead = false;
                var currentByte = inputBytes.CurrentByte;

                if (BaseTextComponentApproach.IsEmpty(currentByte)
                    || ReadHelper.IsWhitespace(currentByte))
                {
                    isSkippingSymbol = false;
                    continue;
                }

                // If we failed to read the symbol for whatever reason we pass over it.
                if (isSkippingSymbol)
                {
                    continue;
                }

                ITokenizer tokenizer = null;
                switch ((char) currentByte)
                {
                    case '(':
                        tokenizer = StringTokenizer;
                        break;
                    case '<':
                        var following = inputBytes.Peek();
                        if (following == '<')
                        {
                            isSkippingSymbol = true;
                            // TODO: Dictionary tokenizer
                        }
                        else
                        {
                            tokenizer = HexTokenizer;
                        }
                        break;
                    case '[':
                        // TODO: Array tokenizer
                        break;
                    case '/':
                        tokenizer = NameTokenizer;
                        break;
                    case '0':
                    case '1':
                    case '2':
                    case '3':
                    case '4':
                    case '5':
                    case '6':
                    case '7':
                    case '8':
                    case '9':
                    case '-':
                    case '+':
                    case '.':
                        tokenizer = NumericTokenizer;
                        break;
                    default:
                        tokenizer = PlainTokenizer;
                        break;
                }

                if (tokenizer == null || !tokenizer.TryTokenize(currentByte, inputBytes, out var token))
                {
                    isSkippingSymbol = true;
                    hasBytePreRead = false;
                    continue;
                }

                CurrentToken = token;

                /* 
                 * Some tokenizers need to read the symbol of the next token to know if they have ended
                 * so we don't want to move on to the next byte, we would lose a byte, e.g.: /NameOne/NameTwo or /Name(string)                
                 */
                hasBytePreRead = tokenizer.ReadsNextByte;

                return true;
            }

            return false;
        }
    }
}