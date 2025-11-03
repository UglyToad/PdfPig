namespace UglyToad.PdfPig.Tokenization.Scanner
{
    using System;
    using System.Collections.Generic;
    using Core;
    using Tokens;

    /// <summary>
    /// The default <see cref="ITokenScanner"/> for reading PostScript/PDF style data.
    /// </summary>
    public class CoreTokenScanner : ISeekableTokenScanner
    {
        private static readonly CommentTokenizer CommentTokenizer = new CommentTokenizer();
        private static readonly HexTokenizer HexTokenizer = new HexTokenizer();
        private static readonly NameTokenizer NameTokenizer = new NameTokenizer();
        private static readonly PlainTokenizer PlainTokenizer = new PlainTokenizer();
        private static readonly NumericTokenizer NumericTokenizer = new NumericTokenizer();

        private readonly StringTokenizer stringTokenizer;
        private readonly ArrayTokenizer arrayTokenizer;
        private readonly DictionaryTokenizer dictionaryTokenizer;

        private readonly ScannerScope scope;
        private readonly IReadOnlyDictionary<NameToken, IReadOnlyList<NameToken>> namedDictionaryRequiredKeys;
        private readonly IInputBytes inputBytes;
        private readonly bool usePdfDocEncoding;
        private readonly List<(byte firstByte, ITokenizer tokenizer)> customTokenizers = new List<(byte, ITokenizer)>();
        private readonly bool useLenientParsing;

        /// <summary>
        /// The offset in the input data at which the <see cref="CurrentToken"/> starts.
        /// </summary>
        public long CurrentTokenStart { get; private set; }

        /// <inheritdoc />
        public IToken CurrentToken { get; private set; }

        /// <inheritdoc />
        public long CurrentPosition => inputBytes.CurrentOffset;

        /// <inheritdoc />
        public long Length => inputBytes.Length;

        private bool hasBytePreRead;
        private bool isInInlineImage;
        /// <summary>
        /// '%' only identifies comments outside of PDF streams and strings, inside these we should ignore it.
        /// </summary>
        /// <remarks>
        /// PDFBox skips all of a line following a comment character inside streams, see:
        /// https://github.com/apache/pdfbox/blob/0e1c42dace1c3a2631d5309f662de5628b80fda6/pdfbox/src/main/java/org/apache/pdfbox/pdfparser/BaseParser.java#L1319
        /// </remarks>
        private readonly bool isStream;

        /// <summary>
        /// Create a new <see cref="CoreTokenScanner"/> from the input.
        /// </summary>
        public CoreTokenScanner(
            IInputBytes inputBytes,
            bool usePdfDocEncoding,
            ScannerScope scope = ScannerScope.None,
            IReadOnlyDictionary<NameToken, IReadOnlyList<NameToken>> namedDictionaryRequiredKeys = null,
            bool useLenientParsing = false,
            bool isStream = false)
        {
            this.inputBytes = inputBytes ?? throw new ArgumentNullException(nameof(inputBytes));
            this.usePdfDocEncoding = usePdfDocEncoding;
            this.stringTokenizer = new StringTokenizer(usePdfDocEncoding);
            this.arrayTokenizer = new ArrayTokenizer(usePdfDocEncoding);
            this.dictionaryTokenizer = new DictionaryTokenizer(usePdfDocEncoding, useLenientParsing: useLenientParsing);
            this.scope = scope;
            this.namedDictionaryRequiredKeys = namedDictionaryRequiredKeys;
            this.useLenientParsing = useLenientParsing;
            this.isStream = isStream;
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
        public void Seek(long position)
        {
            inputBytes.Seek(position);
        }

        /// <inheritdoc />
        public bool MoveNext()
        {
            var endAngleBracesRead = 0;

            bool isSkippingLine = false;
            bool isSkippingSymbol = false;
            while ((hasBytePreRead && !inputBytes.IsAtEnd()) || inputBytes.MoveNext())
            {
                hasBytePreRead = false;
                var currentByte = inputBytes.CurrentByte;
                var c = (char) currentByte;

                if (isSkippingLine)
                {
                    if (ReadHelper.IsEndOfLine(c))
                    {
                        isSkippingLine = false;
                        continue;
                    }

                    continue;
                }

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
                    if (ReadHelper.IsWhitespace(currentByte) || char.IsControl(c))
                    {
                        isSkippingSymbol = false;
                        continue;
                    }

                    if (currentByte == (byte)'%' && isStream)
                    {
                        isSkippingLine = true;
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
                            tokenizer = stringTokenizer;
                            break;
                        case '<':
                            var following = inputBytes.Peek();
                            if (following == '<')
                            {
                                isSkippingSymbol = true;
                                tokenizer = dictionaryTokenizer;

                                if (namedDictionaryRequiredKeys != null
                                    && CurrentToken is NameToken name
                                    && namedDictionaryRequiredKeys.TryGetValue(name, out var requiredKeys))
                                {
                                    tokenizer = new DictionaryTokenizer(usePdfDocEncoding, requiredKeys, useLenientParsing);
                                }
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
                            tokenizer = arrayTokenizer;
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
                        CurrentToken = new InlineImageDataToken(new Memory<byte>([..imageData]));
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

        /// <inheritdoc />
        public void RegisterCustomTokenizer(byte firstByte, ITokenizer tokenizer)
        {
            if (tokenizer == null)
            {
                throw new ArgumentNullException(nameof(tokenizer));
            }

            customTokenizers.Add((firstByte, tokenizer));
        }

        /// <inheritdoc />
        public void DeregisterCustomTokenizer(ITokenizer tokenizer)
        {
            customTokenizers.RemoveAll(x => ReferenceEquals(x.tokenizer, tokenizer));
        }

        /// <summary>
        /// Handles the situation where "EI" was encountered in the inline image data but was
        /// not the end of the image.
        /// </summary>
        /// <param name="lastEndImageOffset">The offset of the "E" of the "EI" marker which was incorrectly read.</param>
        /// <returns>The set of bytes from the incorrect "EI" to the correct "EI" including the incorrect "EI".</returns>
        public IReadOnlyList<byte> RecoverFromIncorrectEndImage(long lastEndImageOffset)
        {
            var data = new List<byte>();

            inputBytes.Seek(lastEndImageOffset);
            
            if (!inputBytes.MoveNext() || inputBytes.CurrentByte != 'E')
            {
                var message = $"Failed to recover the image data stream for an inline image at offset {lastEndImageOffset}. " +
                              $"Expected to read byte 'E' instead got {inputBytes.CurrentByte}.";

                throw new PdfDocumentFormatException(message);
            }

            data.Add(inputBytes.CurrentByte);

            if (!inputBytes.MoveNext() || inputBytes.CurrentByte != 'I')
            {
                var message = $"Failed to recover the image data stream for an inline image at offset {lastEndImageOffset}. " +
                              $"Expected to read second byte 'I' following 'E' instead got {inputBytes.CurrentByte}.";

                throw new PdfDocumentFormatException(message);
            }

            data.Add(inputBytes.CurrentByte);

            data.AddRange(ReadUntilEndImage(lastEndImageOffset));

            // Skip beyond the 'I' in the "EI" token we just read so the scanner is in a valid position.
            inputBytes.MoveNext();

            return data;
        }

        private List<byte> ReadInlineImageData()
        {
            // The ID operator should be followed by a single white-space character, and the next character is interpreted
            // as the first byte of image data. 
            if (inputBytes.Peek() is { } c
                && !ReadHelper.IsWhitespace(c))
            {
                throw new PdfDocumentFormatException($"No whitespace character following the image data (ID) operator. Position: {inputBytes.CurrentOffset}.");
            }

            var startsAt = inputBytes.CurrentOffset - 1;

            return ReadUntilEndImage(startsAt);
        }

        private List<byte> ReadUntilEndImage(long startsAt)
        {
            const byte lastPlainText = 127;
            const byte space = 32;

            var imageData = new List<byte>();
            byte prevByte = 0;
            while (inputBytes.MoveNext())
            {
                if (inputBytes.CurrentByte == 'I' && prevByte == 'E')
                {
                    // Check for EI appearing in binary data.
                    var buffer = new byte[6];

                    var currentOffset = inputBytes.CurrentOffset;

                    var read = inputBytes.Read(buffer);

                    var isEnd = true;

                    if (read == buffer.Length)
                    {
                        var containsWhitespace = false;
                        for (var i = 0; i < buffer.Length; i++)
                        {
                            var b = buffer[i];

                            if (ReadHelper.IsWhitespace(b))
                            {
                                containsWhitespace = true;
                                continue;
                            }

                            if (b > lastPlainText)
                            {
                                isEnd = false;
                                break;
                            }

                            if (b < space && b != '\r' && b != '\n' && b != '\t')
                            {
                                isEnd = false;
                                break;
                            }
                        }

                        if (!containsWhitespace)
                        {
                            isEnd = false;
                        }
                    }

                    inputBytes.Seek(currentOffset);

                    if (isEnd)
                    {
                        imageData.RemoveAt(imageData.Count - 1);
                        return imageData;
                    }
                }

                imageData.Add(inputBytes.CurrentByte);

                prevByte = inputBytes.CurrentByte;
            }

            if (useLenientParsing)
            {
                // Other parsers just treat end-of-file as a valid end-image. Though the image file will be messed up
                // and invalid, and we may miss genuine page content, all tests parsers seem to work this way for file 0007511
                // in the test corpus.
                return imageData;
            }

            throw new PdfDocumentFormatException($"No end of inline image data (EI) was found for image data at position {startsAt}.");
        }
    }
}