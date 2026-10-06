namespace UglyToad.PdfPig.Tests.Tokenization
{
    using PdfPig.Core;
    using PdfPig.Tokenization;
    using PdfPig.Tokens;

    public class StringTokenizerTests
    {
        private readonly StringTokenizer tokenizer = new StringTokenizer();

        [Theory]
        [InlineData("(a\rb)", "a\rb")]
        [InlineData("(a\nb)", "a\nb")]
        [InlineData("(a\r\nb)", "a\r\nb")]
        [InlineData("(a\r\r\nb)", "a\r\r\nb")]
        [InlineData("(a\r\n\nb)", "a\r\n\nb")]
        [InlineData("(a\\\rb)", "ab")]
        [InlineData("(a\\\nb)", "ab")]
        [InlineData("(a\\\r\nb)", "ab")]
        [InlineData("(a\\\r\n\nb)", "a\nb")]
        [InlineData("(a\\\n\nb)", "a\nb")]
        [InlineData("(a\\\r\rb)", "a\rb")]
        [InlineData("(a\\r\\nb)", "a\r\nb")]
        public void PreservesPhysicalLineEndingsAndSkipsOnlyOneEscapedLineEnding(string source, string expected)
        {
            var input = StringBytesTestConverter.Convert(source);
            Assert.True(tokenizer.TryTokenize(input.First, input.Bytes, out var token));
            Assert.Equal(System.Text.Encoding.ASCII.GetBytes(expected), AssertStringToken(token).GetBytes());
        }

        [Theory]
        [InlineData(32)]
        [InlineData(48)]
        public void PreservesBinaryStringBytesIncludingCarriageReturns(int length)
        {
            // Model binary encryption values with raw CR, CRLF and high bytes.
            var expected = Enumerable.Repeat((byte)0xA5, length).ToArray();
            expected[0] = 0x00;
            expected[1] = 0x0D;
            expected[2] = 0x81;
            expected[length - 3] = 0x0D;
            expected[length - 2] = 0x0A;
            expected[length - 1] = 0x0D;
            var source = expected.Concat(new byte[] { 0x29 }).ToArray();
            using var input = new MemoryInputBytes(source);

            Assert.True(tokenizer.TryTokenize(0x28, input, out var token));

            Assert.Equal(expected, AssertStringToken(token).GetBytes());
        }

        [Fact]
        public void NullInput_ReturnsFalse()
        {
            var result = tokenizer.TryTokenize((byte) 'A', null, out var _);

            Assert.False(result);
        }

        [Theory]
        [InlineData('<')]
        [InlineData('\\')]
        [InlineData('A')]
        [InlineData('[')]
        [InlineData('{')]
        [InlineData('|')]
        [InlineData('>')]
        [InlineData(' ')]
        [InlineData('y')]
        [InlineData('^')]
        public void DoesNotStartWithOpenBracket_ReturnsFalse(char firstByte)
        {
            var input = new MemoryInputBytes(new[] {(byte)firstByte});

            var result = tokenizer.TryTokenize((byte)firstByte, input, out var token);

            Assert.False(result);
            Assert.Null(token);
        }

        [Fact]
        public void CanHandleEscapedParentheses()
        {
            const string s = "(this string \\)contains escaped \\( parentheses)";

            var input = StringBytesTestConverter.Convert(s);
            
            var result = tokenizer.TryTokenize(input.First, input.Bytes, out var token);

            Assert.True(result);

            Assert.Equal(@"this string )contains escaped ( parentheses", AssertStringToken(token).Data);
        }

        [Theory]
        [InlineData("(This is a string)", "This is a string")]
        [InlineData("(Strings may contain newlines\r\nand such.)", "Strings may contain newlines\r\nand such.")]
        [InlineData("(Strings may contain balanced parentheses () and special characters (*!*&}^% and so on).)",
            "Strings may contain balanced parentheses () and special characters (*!*&}^% and so on).")]
        [InlineData("()", "")]
        public void CanReadValidStrings(string s, string expected)
        {
            var input = StringBytesTestConverter.Convert(s);

            var result = tokenizer.TryTokenize(input.First, input.Bytes, out var token);

            Assert.True(result);

            Assert.Equal(expected, AssertStringToken(token).Data);
        }
        
        [Fact]
        public void CanHandleNestedParentheses()
        {
            const string s = "(this string (contains nested (two levels)) parentheses)";

            var input = StringBytesTestConverter.Convert(s);

            var result = tokenizer.TryTokenize(input.First, input.Bytes, out var token);

            Assert.True(result);

            Assert.Equal("this string (contains nested (two levels)) parentheses", AssertStringToken(token).Data);
        }
        
        [Fact]
        public void CanHandleAngleBrackets()
        {
            const string s = "(this string <contains>)";

            var input = StringBytesTestConverter.Convert(s);

            var result = tokenizer.TryTokenize(input.First, input.Bytes, out var token);

            Assert.True(result);

            Assert.Equal("this string <contains>", AssertStringToken(token).Data);
        }

        [Fact]
        public void SkipsEscapedEndLines()
        {
            const string s = @"(These \
two strings \
are the same.)";

            const string expected = "These two strings are the same.";
            
            var input = StringBytesTestConverter.Convert(s);

            var result = tokenizer.TryTokenize(input.First, input.Bytes, out var token);

            Assert.True(result);

            Assert.Equal(expected, AssertStringToken(token).Data);
        }

        [Fact]
        public void TreatsEndLinesAsNewline()
        {
            const string s = "(So does this one.\n)";

            const string expected = "So does this one.\n";

            var input = StringBytesTestConverter.Convert(s);

            var result = tokenizer.TryTokenize(input.First, input.Bytes, out var token);

            Assert.True(result);

            Assert.Equal(expected, AssertStringToken(token).Data);
        }

        [Fact]
        public void ConvertsFullOctal()
        {
            const string s = @"(This string contains \245two octal characters\307.)";

            var input = StringBytesTestConverter.Convert(s);

            var result = tokenizer.TryTokenize(input.First, input.Bytes, out var token);

            Assert.True(result);

            Assert.Equal("This string contains ¥two octal charactersÇ.", AssertStringToken(token).Data);
        }

        [Fact]
        public void ConvertsFullOctalFollowedByNormalNumber()
        {
            const string s = @"(This string contains \2451 octal character.)";

            var input = StringBytesTestConverter.Convert(s);

            var result = tokenizer.TryTokenize(input.First, input.Bytes, out var token);

            Assert.True(result);

            Assert.Equal("This string contains ¥1 octal character.", AssertStringToken(token).Data);
        }

        [Fact]
        public void ConvertsPartialOctal()
        {
            const string s = @"(This string has a plus: \53 as octal)";

            var input = StringBytesTestConverter.Convert(s);

            var result = tokenizer.TryTokenize(input.First, input.Bytes, out var token);

            Assert.True(result);

            Assert.Equal("This string has a plus: + as octal", AssertStringToken(token).Data);
        }

        [Fact]
        public void ConvertsTwoPartialOctalsInARow()
        {
            const string s = @"(This string has two \53\326ctals)";

            var input = StringBytesTestConverter.Convert(s);

            var result = tokenizer.TryTokenize(input.First, input.Bytes, out var token);

            Assert.True(result);
            
            Assert.Equal("This string has two +Öctals", AssertStringToken(token).Data);
        }

        /// <summary>
        /// Three octal digits reach \777, past what a byte holds. The specification (7.3.4.2) says
        /// high-order overflow is ignored, so only the low eight bits of the escape survive.
        /// </summary>
        [Theory]
        [InlineData(@"(\400)", 0x00)]
        [InlineData(@"(\401)", 0x01)]
        [InlineData(@"(\777)", 0xFF)]
        [InlineData(@"(\377)", 0xFF)]
        public void OctalEscapeAboveAByteIgnoresHighOrderOverflow(string s, int expected)
        {
            var input = StringBytesTestConverter.Convert(s);

            var result = tokenizer.TryTokenize(input.First, input.Bytes, out var token);

            Assert.True(result);

            Assert.Equal(new[] { (byte)expected }, AssertStringToken(token).GetBytes());
        }

        [Fact]
        public void HandlesEscapedBackslash()
        {
            const string s = @"(listen\\learn)";

            var input = StringBytesTestConverter.Convert(s);

            var result = tokenizer.TryTokenize(input.First, input.Bytes, out var token);

            Assert.True(result);

            Assert.Equal(@"listen\learn", AssertStringToken(token).Data);
        }

        [Theory]
        [InlineData(@"(new line \n)", "new line \n")]
        [InlineData(@"(carriage return \r)", "carriage return \r")]
        [InlineData(@"(tab \t)", "tab \t")]
        [InlineData(@"(bell \b)", "bell \b")]
        [InlineData(@"(uhmmm \f)", "uhmmm \f")]
        public void WritesEscapedCharactersToOutput(string input, string expected)
        {
            var bytes = StringBytesTestConverter.Convert(input);

            var result = tokenizer.TryTokenize(bytes.First, bytes.Bytes, out var token);

            Assert.True(result);

            Assert.Equal(expected, AssertStringToken(token).Data);
        }

        [Fact]
        public void EscapedNonEscapeCharacterWritesPlainCharacter()
        {
            const string s = @"(this does not need escaping \e)";

            var input = StringBytesTestConverter.Convert(s);

            var result = tokenizer.TryTokenize(input.First, input.Bytes, out var token);

            Assert.True(result);

            Assert.Equal(@"this does not need escaping e", AssertStringToken(token).Data);
        }

        [Fact]
        public void ReachesEndOfInputAssumesEndOfString()
        {
            const string s = @"(this does not end with bracket";

            var input = StringBytesTestConverter.Convert(s);

            var result = tokenizer.TryTokenize(input.First, input.Bytes, out var token);

            Assert.True(result);

            Assert.Equal(@"this does not end with bracket", AssertStringToken(token).Data);
        }

        [Fact]
        public void HandlesEscapedEscapeCharacters()
        {
            const string s = @"(   \(sleep 1; printf ""QUIT\\r\\n""\) | )";

            var input = StringBytesTestConverter.Convert(s);

            var result = tokenizer.TryTokenize(input.First, input.Bytes, out var token);

            Assert.True(result);

            Assert.Equal(@"   (sleep 1; printf ""QUIT\r\n"") | ", AssertStringToken(token).Data);
        }

        [Fact]
        public void HandlesUtf16Strings()
        {
            var input = new MemoryInputBytes(new byte[]
            {
                0xFE, 0xFF, 0x00, 0x4D, 0x00, 0x69, 0x00,
                0x63, 0x29
            });

            var result = tokenizer.TryTokenize(0x28, input, out var token);

            Assert.True(result);

            Assert.Equal(@"Mic", AssertStringToken(token).Data);
        }

        [Fact]
        public void HandlesUtf16BigEndianStrings()
        {
            var input = new MemoryInputBytes(new byte[]
            {
                0xFF, 0xFE, 0x4D, 0x00, 0x69, 0x00, 0x63,
                0x00, 0x29
            });

            var result = tokenizer.TryTokenize(0x28, input, out var token);

            Assert.True(result);

            Assert.Equal(@"Mic", AssertStringToken(token).Data);
        }

        private static StringToken AssertStringToken(IToken token)
        {
            Assert.NotNull(token);
            var stringToken = Assert.IsType<StringToken>(token);
            return stringToken;
        }
    }
}
