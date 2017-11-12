namespace UglyToad.Pdf.Tokenization
{
    using System.Collections.Generic;
    using IO;
    using Scanner;
    using Tokens;

    public class ArrayTokenizer : ITokenizer
    {
        public bool ReadsNextByte { get; } = false;

        public bool TryTokenize(byte currentByte, IInputBytes inputBytes, out IToken token)
        {
            token = null;

            if (currentByte != '[')
            {
                return false;
            }

            var scanner = new CoreTokenScanner(inputBytes, ScannerScope.Array);

            var contents = new List<IToken>();

            IToken previousToken = null;
            while (!CurrentByteEndsCurrentArray(inputBytes, previousToken) && scanner.MoveNext())
            {
                previousToken = scanner.CurrentToken;
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
