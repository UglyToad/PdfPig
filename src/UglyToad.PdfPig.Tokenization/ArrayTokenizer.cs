namespace UglyToad.PdfPig.Tokenization
{
    using System.Collections.Generic;
    using Core;
    using Scanner;
    using Tokens;

    internal sealed class ArrayTokenizer : ITokenizer
    {
        private readonly bool usePdfDocEncoding;

        public bool ReadsNextByte => false;

        public ArrayTokenizer(bool usePdfDocEncoding)
        {
            this.usePdfDocEncoding = usePdfDocEncoding;
        }

        public bool TryTokenize(byte currentByte, IInputBytes inputBytes, out IToken token)
        {
            token = null;

            if (currentByte != '[')
            {
                return false;
            }

            var scanner = new CoreTokenScanner(inputBytes, usePdfDocEncoding, ScannerScope.Array);

            var contents = new List<IToken>();

            IToken previousToken = null;
            while (!CurrentByteEndsCurrentArray(inputBytes, previousToken) && scanner.MoveNext())
            {
                previousToken = scanner.CurrentToken;

                if (scanner.CurrentToken is CommentToken)
                {
                    continue;
                }
                
                contents.Add(scanner.CurrentToken);
            }

            token = new ArrayToken(contents);

            return true;
        }

        private static bool CurrentByteEndsCurrentArray(IInputBytes inputBytes, IToken previousToken)
        {
            if (inputBytes.CurrentByte == ']' && !(previousToken is ArrayToken))
            {
                return true;
            }

            return false;
        }
    }
}
