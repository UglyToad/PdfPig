namespace UglyToad.PdfPig.Tests.Tokenization
{
    using System.IO;
    using PdfPig.Core;
    using PdfPig.Tokenization;
    using PdfPig.Tokenization.Scanner;
    using PdfPig.Tokens;

    /// <summary>
    /// The plain and numeric tokenizers read an in-memory input from its span and any other input
    /// byte by byte. Both ways must produce the same token and leave the input in the same place.
    /// </summary>
    public class TokenizerInputKindTests
    {
        public static IEnumerable<object[]> Inputs => new[]
        {
            "Tj ", "Tj", "TJ\n", "BT\r\n", "q", "Q ", "d0 ", "d1 1", "re[", "true ", "true", "false)", "null<",
            "endstream", "eexec", "stream\r\n", "Do/F1", "Td%c", "unknownOperator ", "true1", "n", "n]", "W*", "T* ",
            "1", "1 ", "0", "-1 ", "+2 ", "3.5 ", ".5 ", "-.5 ", "1.2.3 ", "12e", "12e5 ", "1E2 ", "1.53E3 ", "1.457E2 ",
            "e5", "1e5e ", "1- ", "4.", "0003 ", "12345678901234567890 ", "7]", "7/Name", "-", "+", ".", "1e-2 ", "255 ", "1024 ", "-128 ", "-129 "
        }.Select(s => new object[] { s });

        [Theory]
        [MemberData(nameof(Inputs))]
        public void PlainTokenizerAgreesBetweenMemoryAndStream(string input)
        {
            AssertAgree(new PlainTokenizer(), input);
        }

        [Theory]
        [MemberData(nameof(Inputs))]
        public void PlainTokenizerSplittingOnDigitAgreesBetweenMemoryAndStream(string input)
        {
            AssertAgree(new PlainTokenizer(splitOnDigit: true), input);
        }

        [Theory]
        [MemberData(nameof(Inputs))]
        public void NumericTokenizerAgreesBetweenMemoryAndStream(string input)
        {
            AssertAgree(new NumericTokenizer(), input);
        }

        [Fact]
        public void ScannerYieldsTheSameTokensFromMemoryAndStream()
        {
            const string content = "BT /F1 12 Tf 72 700.5 Td [(Hello)-20(World)] TJ ET\n"
                                   + "q 1 0 0 1 0 0 cm /Im1 Do Q 0 0 1 rg 1.2.3 5 w true null false % comment\r\n"
                                   + "<</A 1 /B [2 3.5 <414243>] /C (x)>> 12e 3 d0 W* T* ' \" 1E2 .5 -.5 -1 Tj";

            var bytes = OtherEncodings.StringAsLatin1Bytes(content);

            var fromMemory = ReadAll(new MemoryInputBytes(bytes));
            var fromStream = ReadAll(new StreamInputBytes(new MemoryStream(bytes)));

            Assert.Equal(fromStream.Count, fromMemory.Count);

            for (var i = 0; i < fromStream.Count; i++)
            {
                Assert.Equal(fromStream[i].GetType(), fromMemory[i].GetType());
                Assert.Equal(fromStream[i], fromMemory[i]);
            }
        }

        private static List<IToken> ReadAll(IInputBytes input)
        {
            var scanner = new CoreTokenScanner(input, false, new StackDepthGuard(256));
            var result = new List<IToken>();
            while (scanner.MoveNext())
            {
                result.Add(scanner.CurrentToken);
            }

            return result;
        }

        private static void AssertAgree(ITokenizer tokenizer, string input)
        {
            var bytes = OtherEncodings.StringAsLatin1Bytes(input);

            var memory = new MemoryInputBytes(bytes);
            var stream = new StreamInputBytes(new MemoryStream(bytes));

            memory.MoveNext();
            stream.MoveNext();

            var memoryResult = tokenizer.TryTokenize(memory.CurrentByte, memory, out var memoryToken);
            var streamResult = tokenizer.TryTokenize(stream.CurrentByte, stream, out var streamToken);

            Assert.Equal(streamResult, memoryResult);
            Assert.Equal(streamToken, memoryToken);
            Assert.Equal(stream.CurrentOffset, memory.CurrentOffset);
            // Only while there is input left: at the end a stream input reports a zero byte and
            // a memory input the last byte, which is how the two have always differed.
            if (!memory.IsAtEnd())
            {
                Assert.Equal(stream.CurrentByte, memory.CurrentByte);
            }
        }
    }
}
