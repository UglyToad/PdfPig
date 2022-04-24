namespace UglyToad.PdfPig.Tests
{
    using System.Text;
    using PdfPig.Core;
    using PdfPig.Tokenization.Scanner;

    internal static class StringBytesTestConverter
    {
        public static Result Convert(string s, bool readFirst = true)
        {
            var input = new ByteArrayInputBytes(Encoding.UTF8.GetBytes(s));

            byte initialByte = 0;
            if (readFirst)
            {
                input.MoveNext();
                initialByte = input.CurrentByte;
            }
            
            return new Result
            {
                First = initialByte,
                Bytes = input
            };
        }

        public class Result
        {
            public byte First { get; set; }

            public IInputBytes Bytes { get; set; }
        }

        internal static (CoreTokenScanner scanner, IInputBytes bytes) Scanner(string s)
        {
            var inputBytes = new ByteArrayInputBytes(OtherEncodings.StringAsLatin1Bytes(s));
            var result = new CoreTokenScanner(inputBytes);

            return (result, inputBytes);
        }
    }
}
