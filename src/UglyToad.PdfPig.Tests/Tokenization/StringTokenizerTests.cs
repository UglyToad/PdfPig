namespace UglyToad.PdfPig.Tests.Tokenization
{
    using PdfPig.IO;
    using PdfPig.Tokenization;
    using PdfPig.Tokenization.Tokens;
    using Xunit;

    public class StringTokenizerTests
    {
        private readonly StringTokenizer tokenizer = new StringTokenizer();

        [Fact]
        public void NullInput_ReturnsFalse()
        {
            var result = tokenizer.TryTokenize((byte) 'A', null, out var _);

            Assert.False(result);
        }

        [Theory]
        [InlineData(')')]
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
        public void DoesNotStartWithOpenBracket_ReturnsFalse(byte firstByte)
        {
            var input = new ByteArrayInputBytes(new[] {firstByte});

            var result = tokenizer.TryTokenize(firstByte, input, out var token);

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

        private static StringToken AssertStringToken(IToken token)
        {
            Assert.NotNull(token);
            var stringToken = Assert.IsType<StringToken>(token);
            return stringToken;
        }
    }
}
