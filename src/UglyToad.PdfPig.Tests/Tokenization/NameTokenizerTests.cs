namespace UglyToad.PdfPig.Tests.Tokenization
{
    using PdfPig.Tokenization;
    using PdfPig.Tokens;

    public class NameTokenizerTests
    {
        private readonly NameTokenizer tokenizer = new NameTokenizer();
        
        [Fact]
        public void DifferentByteSequencesDoNotCollapseToTheSameName()
        {
            var utf8 = ReadName("/#C3#A9");
            var singleByte = ReadName("/#E9");
            Assert.NotEqual(utf8, singleByte);
            Assert.Equal("\u00c3\u00a9", utf8.Data);
            Assert.Equal("\u00e9", singleByte.Data);
            Assert.Equal(ReadName("/é"), utf8);
        }

        [Fact]
        public void DictionaryKeysPreserveByteIdentity()
        {
            var input = StringBytesTestConverter.Convert("<< /#C3#A9 1 /#E9 2 /#80 3 >>");
            var dictionaryTokenizer = new DictionaryTokenizer(new PdfPig.Core.StackDepthGuard(256));
            Assert.True(dictionaryTokenizer.TryTokenize(input.First, input.Bytes, out var token));
            var dictionary = Assert.IsType<DictionaryToken>(token);
            Assert.Equal(3, dictionary.Data.Count);
            Assert.Equal(1, Assert.IsType<NumericToken>(dictionary.Data["\u00c3\u00a9"]).Int);
            Assert.Equal(2, Assert.IsType<NumericToken>(dictionary.Data["\u00e9"]).Int);
            Assert.Equal(3, Assert.IsType<NumericToken>(dictionary.Data["\u0080"]).Int);
        }

        [Fact]
        public void EveryNonNullByteSurvivesNameWriteAndRead()
        {
            for (var value = 1; value <= 255; value++)
            {
                var name = ReadName("/#" + value.ToString("X2"));
                Assert.Equal(((char)value).ToString(), name.Data);
                using var output = new MemoryStream();
                new PdfPig.Writer.TokenWriter().WriteToken(name, output);
                Assert.Equal(name, ReadName(System.Text.Encoding.ASCII.GetString(output.ToArray())));
            }
        }

        private NameToken ReadName(string source)
        {
            var input = StringBytesTestConverter.Convert(source);
            Assert.True(tokenizer.TryTokenize(input.First, input.Bytes, out var token));
            return Assert.IsType<NameToken>(token);
        }

        [Fact]
        public void ReadsName()
        {
            const string s = "/Type /XRef";

            var input = StringBytesTestConverter.Convert(s);
            
            var result = tokenizer.TryTokenize(input.First, input.Bytes, out var token);

            Assert.True(result);
            
            Assert.Equal("Type", AssertNameToken(token).Data);
        }

        [Fact]
        public void ReadsNameNoEndSpace()
        {
            const string s = "/Type/XRef";

            var input = StringBytesTestConverter.Convert(s);

            var result = tokenizer.TryTokenize(input.First, input.Bytes, out var token);

            Assert.True(result);

            Assert.Equal("Type", AssertNameToken(token).Data);
        }

        [Fact]
        public void ReadsName_NotAtForwardSlash_Throws()
        {
            const string s = " /Type";

            var input = StringBytesTestConverter.Convert(s);

            var result = tokenizer.TryTokenize(input.First, input.Bytes, out var _);

            Assert.False(result);
        }

        [Fact]
        public void ReadsNameAtEndOfStream()
        {
            const string s = "/XRef";

            var input = StringBytesTestConverter.Convert(s);

            var result = tokenizer.TryTokenize(input.First, input.Bytes, out var token);

            Assert.True(result);

            Assert.Equal("XRef", AssertNameToken(token).Data);
        }

        [Fact]
        public void FallsBackToUnescapedForEarlyPdfTypes()
        {
            const string s = "/Priorto1.2#INvalidHexHash";

            var input = StringBytesTestConverter.Convert(s);

            var result = tokenizer.TryTokenize(input.First, input.Bytes, out var token);

            Assert.True(result);

            Assert.Equal("Priorto1.2#INvalidHexHash", AssertNameToken(token).Data);
        }

        [Theory]
        [InlineData("/Name1", "Name1")]
        [InlineData("/ASomewhatLongerName", "ASomewhatLongerName")]
        [InlineData("/A−Name_With;Various***Characters?", "A\u00e2\u0088\u0092Name_With;Various***Characters?")]
        [InlineData("/1.2", "1.2")]
        [InlineData("/$$", "$$")]
        [InlineData("/@pattern", "@pattern")]
        [InlineData("/.notdef", ".notdef")]
        public void ReadsValidPdfNames(string s, string expected)
        {
            var input = StringBytesTestConverter.Convert(s);

            var result = tokenizer.TryTokenize(input.First, input.Bytes, out var token);

            Assert.True(result);

            Assert.Equal(expected, AssertNameToken(token).Data);
        }

        [Theory]
        [InlineData("/Adobe#20Green", "Adobe Green")]
        [InlineData("/PANTONE#205757#20CV", "PANTONE 5757 CV")]
        [InlineData("/paired#28#29parentheses", "paired()parentheses")]
        [InlineData("/The_Key_of_F#23_Minor", "The_Key_of_F#_Minor")]
        [InlineData("/A#42", "AB")]
        public void ReadsHexNames(string s, string expected)
        {
            var input = StringBytesTestConverter.Convert(s);

            var result = tokenizer.TryTokenize(input.First, input.Bytes, out var token);

            Assert.True(result);

            Assert.Equal(expected, AssertNameToken(token).Data);
        }

        [Fact]
        public void IgnoredInvalidHex()
        {
            var input = StringBytesTestConverter.Convert("/Invalid#AZBadHex");

            var result = tokenizer.TryTokenize(input.First, input.Bytes, out var token);

            Assert.True(result);

            Assert.Equal("Invalid#AZBadHex", AssertNameToken(token).Data);
        }

        [Fact]
        public void IgnoreInvalidSingleHex()
        {
            var input = StringBytesTestConverter.Convert("/Invalid#Z");

            var result = tokenizer.TryTokenize(input.First, input.Bytes, out var token);

            Assert.True(result);

            Assert.Equal("Invalid#Z", AssertNameToken(token).Data);
        }

        [Fact]
        public void EndsNameFollowingInvalidHex()
        {
            var input = StringBytesTestConverter.Convert("/Hex#/Name");

            var result = tokenizer.TryTokenize(input.First, input.Bytes, out var token);

            Assert.True(result);

            Assert.Equal("Hex#", AssertNameToken(token).Data);
        }

        private static NameToken AssertNameToken(IToken token)
        {
            Assert.NotNull(token);

            var result = Assert.IsType<NameToken>(token);

            return result;
        }
    }
}
