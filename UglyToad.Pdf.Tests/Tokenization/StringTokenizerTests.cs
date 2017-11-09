namespace UglyToad.Pdf.Tests.Tokenization
{
    using System.Linq;
    using IO;
    using Pdf.Tokenization;
    using Pdf.Tokenization.Tokens;
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

            var input = new ByteArrayInputBytes(s.Select(x => (byte) x).ToArray());

            input.MoveNext();
            var initialByte = input.CurrentByte;
            
            var result = tokenizer.TryTokenize(initialByte, input, out var token);

            Assert.True(result);
            Assert.NotNull(token);

            var stringToken = Assert.IsType<StringToken>(token);

            Assert.Equal(@"this string )contains escaped ( parentheses", stringToken.Data);
        }

        [Theory]
        [InlineData("(This is a string)", "This is a string")]
        [InlineData("(Strings may contain newlines\r\nand such.)", "Strings may contain newlines\r\nand such.")]
        [InlineData("(Strings may contain balanced parentheses () and special characters (*!*&}^% and so on).)",
            "Strings may contain balanced parentheses () and special characters (*!*&}^% and so on).")]
        [InlineData("()", "")]
        public void CanReadValidStrings(string s, string expected)
        {
            var input = new ByteArrayInputBytes(s.Select(x => (byte)x).ToArray());

            input.MoveNext();
            var initialByte = input.CurrentByte;

            var result = tokenizer.TryTokenize(initialByte, input, out var token);

            Assert.True(result);
            Assert.NotNull(token);
            var stringToken = Assert.IsType<StringToken>(token);

            Assert.Equal(expected, stringToken.Data);
        }
        
        [Fact]
        public void CanHandleNestedParentheses()
        {
            const string s = "(this string (contains nested (two levels)) parentheses)";

            var input = new ByteArrayInputBytes(s.Select(x => (byte)x).ToArray());

            input.MoveNext();
            var initialByte = input.CurrentByte;

            var result = tokenizer.TryTokenize(initialByte, input, out var token);

            Assert.True(result);
            Assert.NotNull(token);

            var stringToken = Assert.IsType<StringToken>(token);

            Assert.Equal("this string (contains nested (two levels)) parentheses", stringToken.Data);
        }
        
        [Fact]
        public void CanHandleAngleBrackets()
        {
            const string s = "(this string <contains>)";

            var input = new ByteArrayInputBytes(s.Select(x => (byte)x).ToArray());

            input.MoveNext();
            var initialByte = input.CurrentByte;

            var result = tokenizer.TryTokenize(initialByte, input, out var token);

            Assert.True(result);
            Assert.NotNull(token);

            var stringToken = Assert.IsType<StringToken>(token);

            Assert.Equal("this string <contains>", stringToken.Data);
        }

        [Fact]
        public void SkipsEscapedEndLines()
        {
            const string s = @"(These \
two strings \
are the same.)";

            const string expected = "These two strings are the same.";

            var input = new ByteArrayInputBytes(s.Select(x => (byte)x).ToArray());

            input.MoveNext();
            var initialByte = input.CurrentByte;

            var result = tokenizer.TryTokenize(initialByte, input, out var token);

            Assert.True(result);
            Assert.NotNull(token);

            var stringToken = Assert.IsType<StringToken>(token);

            Assert.Equal(expected, stringToken.Data);
        }

        [Fact]
        public void TreatsEndLinesAsNewline()
        {
            const string s = "(So does this one.\n)";

            const string expected = "So does this one.\n";

            var input = new ByteArrayInputBytes(s.Select(x => (byte)x).ToArray());

            input.MoveNext();
            var initialByte = input.CurrentByte;

            var result = tokenizer.TryTokenize(initialByte, input, out var token);

            Assert.True(result);
            Assert.NotNull(token);

            var stringToken = Assert.IsType<StringToken>(token);

            Assert.Equal(expected, stringToken.Data);
        }
    }
}
