namespace UglyToad.PdfPig.Tokenization.Scanner
{
    using System;
    using System.Collections.Generic;
    using Exceptions;
    using IO;
    using Parser.Parts;
    using Tokens;

    internal class CoreTokenScanner : ISeekableTokenScanner
    {
        private static readonly HexTokenizer HexTokenizer = new HexTokenizer();
        private static readonly StringTokenizer StringTokenizer = new StringTokenizer();
        private static readonly NumericTokenizer NumericTokenizer = new NumericTokenizer();
        private static readonly NameTokenizer NameTokenizer = new NameTokenizer();
        private static readonly PlainTokenizer PlainTokenizer = new PlainTokenizer();
        private static readonly ArrayTokenizer ArrayTokenizer = new ArrayTokenizer();
        private static readonly DictionaryTokenizer DictionaryTokenizer = new DictionaryTokenizer();
        private static readonly CommentTokenizer CommentTokenizer = new CommentTokenizer();

        private readonly ScannerScope scope;
        private readonly IInputBytes inputBytes;
        private readonly List<(byte firstByte, ITokenizer tokenizer)> customTokenizers = new List<(byte, ITokenizer)>();
        
        internal long CurrentTokenStart { get; private set; }

        public IToken CurrentToken { get; private set; }

        public bool TryReadToken<T>(out T token) where T : class, IToken
        {
            token = default(T);

            if (!MoveNext())
            {
                return false;
            }

            if (CurrentToken is T canCast)
            {
                token = canCast;
                return true;
            }

            return false;
        }

        public void Seek(long position)
        {
            inputBytes.Seek(position);
        }

        public long CurrentPosition => inputBytes.CurrentOffset;
        
        private bool hasBytePreRead;
        private bool isInInlineImage;

        internal CoreTokenScanner(IInputBytes inputBytes, ScannerScope scope = ScannerScope.None)
        {
            this.scope = scope;
            this.inputBytes = inputBytes ?? throw new ArgumentNullException(nameof(inputBytes));
        }

        public bool MoveNext()
        {
            var endAngleBracesRead = 0;

            bool isSkippingSymbol = false;
            while ((hasBytePreRead && !inputBytes.IsAtEnd()) || inputBytes.MoveNext())
            {
                hasBytePreRead = false;
                var currentByte = inputBytes.CurrentByte;
                var c = (char) currentByte;

                ITokenizer tokenizer = null;
                foreach (var customTokenizer in customTokenizers)
                {
                    if (currentByte == customTokenizer.firstByte)
                    {
                        tokenizer = customTokenizer.tokenizer;
                        break;
                    }
                }

                if (tokenizer == null)
                {
                    if (IsEmpty(currentByte) || ReadHelper.IsWhitespace(currentByte))
                    {
                        isSkippingSymbol = false;
                        continue;
                    }

                    // If we failed to read the symbol for whatever reason we pass over it.
                    if (isSkippingSymbol && c != '>')
                    {
                        continue;
                    }

                    switch (c)
                    {
                        case '(':
                            tokenizer = StringTokenizer;
                            break;
                        case '<':
                            var following = inputBytes.Peek();
                            if (following == '<')
                            {
                                isSkippingSymbol = true;
                                tokenizer = DictionaryTokenizer;
                            }
                            else
                            {
                                tokenizer = HexTokenizer;
                            }
                            break;
                        case '>' when scope == ScannerScope.Dictionary:
                            endAngleBracesRead++;
                            if (endAngleBracesRead == 2)
                            {
                                return false;
                            }
                            break;
                        case '[':
                            tokenizer = ArrayTokenizer;
                            break;
                        case ']' when scope == ScannerScope.Array:
                            return false;
                        case '/':
                            tokenizer = NameTokenizer;
                            break;
                        case '%':
                            tokenizer = CommentTokenizer;
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
                }

                CurrentTokenStart = inputBytes.CurrentOffset - 1;

                if (tokenizer == null || !tokenizer.TryTokenize(currentByte, inputBytes, out var token))
                {
                    isSkippingSymbol = true;
                    hasBytePreRead = false;
                    continue;
                }

                if (token is OperatorToken op)
                {
                    if (op.Data == "BI")
                    {
                        isInInlineImage = true;
                    }
                    else if (isInInlineImage && op.Data == "ID")
                    {
                        // Special case handling for inline images.
                        var imageData = ReadInlineImageData();
                        isInInlineImage = false;
                        CurrentToken = new InlineImageDataToken(imageData);
                        hasBytePreRead = false;
                        return true;
                    }
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

        public void RegisterCustomTokenizer(byte firstByte, ITokenizer tokenizer)
        {
            if (tokenizer == null)
            {
                throw new ArgumentNullException(nameof(tokenizer));
            }

            customTokenizers.Add((firstByte, tokenizer));
        }

        public void DeregisterCustomTokenizer(ITokenizer tokenizer)
        {
            customTokenizers.RemoveAll(x => ReferenceEquals(x.tokenizer, tokenizer));
        }

        private IReadOnlyList<byte> ReadInlineImageData()
        {
            // The ID operator should be followed by a single white-space character, and the next character is interpreted
            // as the first byte of image data. 
            if (inputBytes.CurrentByte != ' ')
            {
                throw new PdfDocumentFormatException($"No whitespace character following the image data (ID) operator. Position: {inputBytes.CurrentOffset}.");
            }

            var startsAt = inputBytes.CurrentOffset - 2;

            var imageData = new List<byte>();
            byte prevByte = 0;
            while (inputBytes.MoveNext())
            {
                if (inputBytes.CurrentByte == 'I' && prevByte == 'E')
                {
                    imageData.RemoveAt(imageData.Count - 1);
                    return imageData;
                }

                imageData.Add(inputBytes.CurrentByte);

                prevByte = inputBytes.CurrentByte;
            }

            throw new PdfDocumentFormatException($"No end of inline image data (EI) was found for image data at position {startsAt}.");
        }

        private static bool IsEmpty(byte b)
        {
            return b == ' ' || b == '\r' || b == '\n' || b == 0;
        }
    }
}