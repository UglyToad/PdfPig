namespace UglyToad.PdfPig.Tokenization.Scanner
{
    using System;
    using System.Collections.Generic;
    using ContentStream;
    using Cos;
    using Exceptions;
    using IO;
    using Tokens;

    internal class PdfTokenScanner : ISeekableTokenScanner
    {
        private readonly IInputBytes inputBytes;
        private readonly CrossReferenceTable crossReferenceTable;
        private readonly CoreTokenScanner coreTokenScanner;

        private readonly long[] previousTokenPositions = new long[2];
        private readonly IToken[] previousTokens = new IToken[2];

        private readonly Dictionary<IndirectReference, long> objectOffsets = new Dictionary<IndirectReference, long>();

        public IToken CurrentToken { get; private set; }

        public long CurrentPosition => coreTokenScanner.CurrentPosition;

        public PdfTokenScanner(IInputBytes inputBytes, CrossReferenceTable crossReferenceTable)
        {
            this.inputBytes = inputBytes;
            this.crossReferenceTable = crossReferenceTable;
            coreTokenScanner = new CoreTokenScanner(inputBytes);
        }

        public bool MoveNext()
        {
            int tokensRead = 0;
            while (coreTokenScanner.MoveNext() && coreTokenScanner.CurrentToken != OperatorToken.StartObject)
            {
                if (coreTokenScanner.CurrentToken is CommentToken)
                {
                    continue;
                }

                tokensRead++;

                previousTokens[0] = previousTokens[1];
                previousTokenPositions[0] = previousTokenPositions[1];

                previousTokens[1] = coreTokenScanner.CurrentToken;
                previousTokenPositions[1] = coreTokenScanner.CurrentTokenStart;
            }

            if (tokensRead < 2)
            {
                return false;
            }

            var startPosition = previousTokenPositions[0];
            var objectNumber = previousTokens[0] as NumericToken;
            var generation = previousTokens[1] as NumericToken;

            if (objectNumber == null || generation == null)
            {
                throw new PdfDocumentFormatException("The obj operator (start object) was not preceded by a 2 numbers." +
                                                     $"Instead got: {previousTokens[0]} {previousTokens[1]} obj");
            }

            var data = new List<IToken>();

            while (coreTokenScanner.MoveNext() && coreTokenScanner.CurrentToken != OperatorToken.EndObject)
            {
                if (coreTokenScanner.CurrentToken is CommentToken)
                {
                    continue;
                }

                if (coreTokenScanner.CurrentToken == OperatorToken.StartStream)
                {
                    // Read stream.
                }

                data.Add(coreTokenScanner.CurrentToken);

                previousTokens[0] = previousTokens[1];
                previousTokenPositions[0] = previousTokenPositions[1];

                previousTokens[1] = coreTokenScanner.CurrentToken;
                previousTokenPositions[1] = coreTokenScanner.CurrentPosition;
            }

            if (coreTokenScanner.CurrentToken != OperatorToken.EndObject)
            {
                return false;
            }

            CurrentToken = new ObjectToken(startPosition, new IndirectReference(objectNumber.Long, generation.Int), data[data.Count - 1]);

            return true;
        }

        public bool TryReadToken<T>(out T token) where T : class, IToken
        {
            return coreTokenScanner.TryReadToken(out token);
        }

        public void Seek(long position)
        {
            coreTokenScanner.Seek(position);
        }

        public void RegisterCustomTokenizer(byte firstByte, ITokenizer tokenizer)
        {
            coreTokenScanner.RegisterCustomTokenizer(firstByte, tokenizer);
        }

        public void DeregisterCustomTokenizer(ITokenizer tokenizer)
        {
            coreTokenScanner.DeregisterCustomTokenizer(tokenizer);
        }
    }
}
