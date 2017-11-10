namespace UglyToad.Pdf.IO
{
    using System;
    using System.Collections.Generic;
    using Parser.Parts;
    using Text.Operators;
    using Tokenization;
    using Tokenization.Scanner;
    using Tokenization.Tokens;

    public class CoreTokenScanner : ITokenScanner
    {
        private readonly CosDictionaryParser dictionaryParser;
        private readonly CosArrayParser arrayParser;
        private readonly IInputBytes inputBytes;
        private readonly List<byte> currentBuffer = new List<byte>();

        private static readonly HexTokenizer HexTokenizer = new HexTokenizer();
        private static readonly StringTokenizer StringTokenizer = new StringTokenizer();
        private static readonly NumericTokenizer NumericTokenizer = new NumericTokenizer();
        private static readonly NameTokenizer NameTokenizer = new NameTokenizer();

        private static readonly IReadOnlyDictionary<byte, ITokenizer> Tokenizers = new Dictionary<byte, ITokenizer>
        {
            {(byte) '(', new StringTokenizer()}
        };

        public IToken CurrentToken { get; private set; }

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
            while (inputBytes.MoveNext())
            {
                var currentByte = inputBytes.CurrentByte;

                if (BaseTextComponentApproach.IsEmpty(currentByte))
                {
                    isSkippingSymbol = false;
                    continue;
                }

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
                        }
                        else
                        {
                            tokenizer = HexTokenizer;
                        }
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
                        tokenizer = null;
                        break;
                }

                if (tokenizer == null || !tokenizer.TryTokenize(currentByte, inputBytes, out var token))
                {
                    isSkippingSymbol = true;
                    continue;
                }

                CurrentToken = token;

                return true;
            }

            return false;
        }
    }
}