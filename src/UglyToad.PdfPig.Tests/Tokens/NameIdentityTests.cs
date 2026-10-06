namespace UglyToad.PdfPig.Tests.Tokens
{
    using System.Text;
    using PdfPig.Core;
    using PdfPig.Tokenization;
    using PdfPig.Tokens;
    using PdfPig.Writer;

    public class NameIdentityTests
    {
        private static NameToken Parse(string source)
        {
            var input = StringBytesTestConverter.Convert(source);
            Assert.True(new NameTokenizer().TryTokenize(input.First, input.Bytes, out var token));
            return Assert.IsType<NameToken>(token);
        }

        [Theory]
        [InlineData("/#E5#AE#8B#E4#BD#93", "\u5b8b\u4f53")]
        [InlineData("/A#E2#88#92Name", "A\u2212Name")]
        [InlineData("/#C3#A9", "\u00e9")]
        [InlineData("/#E9", "\u00e9")]
        [InlineData("/#80", "\u0080")]
        [InlineData("/#EF#BF#BD", "\ufffd")]
        public void DecodesTextSeparatelyFromBytes(string source, string expected)
        {
            Assert.Equal(expected, Parse(source).Data);
        }

        [Fact]
        public void ByteIdentitySurvivesEqualDecodedText()
        {
            var utf8 = Parse("/#C3#A9");
            var latin1 = Parse("/#E9");
            Assert.Equal(utf8.Data, latin1.Data);
            Assert.NotEqual(utf8, latin1);
            Assert.False(utf8 == latin1);
            Assert.Equal(2, new HashSet<NameToken> { utf8, latin1 }.Count);
            Assert.Same(utf8, NameToken.Create("\u00e9"));
            Assert.Same(latin1, NameToken.Create(new byte[] { 0xE9 }.AsSpan()));
            Assert.Same(NameToken.Type, NameToken.Create("Type"));
        }

        [Fact]
        public void NameBytesCannotBeChangedThroughInputOrReturnedArray()
        {
            var bytes = new byte[] { 0xC3, 0xA9 };
            var name = NameToken.Create(bytes.AsSpan());
            bytes[0] = 0x41;
            var copy = name.GetBytes();
            copy[1] = 0x42;
            Assert.Equal(new byte[] { 0xC3, 0xA9 }, name.Bytes.ToArray());
            Assert.Same(name, NameToken.Create("\u00e9"));
        }

        [Fact]
        public void InternsByteIdenticalNamesAcrossThreads()
        {
            var names = new NameToken[100];
            Parallel.For(0, names.Length, i => names[i] = NameToken.Create("Parallel-\u5b8b"));
            Assert.All(names, name => Assert.Same(names[0], name));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Type1NamesPreserveBytesInsideAndOutsideArrays(bool insideArray)
        {
            using var input = new MemoryInputBytes(OtherEncodings.StringAsLatin1Bytes(
                insideArray ? "{/\u00e9}" : "/\u00e9 "));
            Assert.True(input.MoveNext());
            ITokenizer tokenizer = insideArray
                ? new PdfPig.Fonts.Type1.Parser.Type1ArrayTokenizer()
                : new PdfPig.Fonts.Type1.Parser.Type1NameTokenizer();
            Assert.True(tokenizer.TryTokenize(input.CurrentByte, input, out var token));
            var name = insideArray
                ? Assert.IsType<NameToken>(Assert.Single(Assert.IsType<ArrayToken>(token).Data))
                : Assert.IsType<NameToken>(token);
            Assert.Equal(new byte[] { 0xE9 }, name.GetBytes());
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Type3CharProcLookupMatchesPdfBoxUtf8Reconstruction(bool utf8Key)
        {
            var name = utf8Key ? Parse("/#C3#A9") : Parse("/#E9");
            var stream = new StreamToken(DictionaryToken.Empty, new byte[0]);
            var encoding = new PdfPig.Fonts.Encodings.DifferenceBasedEncoding(
                PdfPig.Fonts.Encodings.StandardEncoding.Instance, new[] { (65, "\u00e9") });
            var font = new PdfPig.PdfFonts.Simple.Type3Font(NameToken.Type3,
                new PdfRectangle(0, 0, 500, 700), TransformationMatrix.FromValues(.001, 0, 0, .001, 0, 0),
                encoding, 65, 65, new[] { 500.0 }, null,
                new Dictionary<NameToken, StreamToken> { [name] = stream }, null);
            Assert.Equal(utf8Key, font.TryGetCharProc(65, out var result));
            if (utf8Key) Assert.Same(stream, result);
            else Assert.Null(result);
        }

        [Fact]
        public void DictionarySnapshotsCannotChangeNameIdentity()
        {
            var name = Parse("/#C3#A9");
            var source = new Dictionary<NameToken, IToken> { [name] = new NumericToken(1) };
            var dictionary = new DictionaryToken(source);
            source[name] = new NumericToken(2);
            Assert.Equal(1, Assert.IsType<NumericToken>(dictionary.Entries[name]).Int);
            Assert.Throws<NotSupportedException>(() => ((IDictionary<NameToken, IToken>)dictionary.Entries).Clear());
            Assert.Throws<NotSupportedException>(() => ((IDictionary<string, IToken>)dictionary.Data).Clear());
            var recreated = DictionaryToken.With(dictionary.Data);
            Assert.Equal(dictionary, recreated);
            Assert.Equal(dictionary.GetHashCode(), recreated.GetHashCode());
        }

        [Fact]
        public void LegacyStringDictionaryFactoryRetainsLiveWriterView()
        {
            var source = new Dictionary<string, IToken>();
            var dictionary = DictionaryToken.With(source);
            source["\u00c3\u00a9"] = new NumericToken(1);
            var name = Parse("/#C3#A9");
            Assert.True(dictionary.TryGet<NumericToken>(name, out var value));
            Assert.Equal(1, value.Int);
            Assert.Same(name, Assert.Single(dictionary.Entries).Key);
            source["\u00e9"] = new NumericToken(2);
            Assert.Equal(2, dictionary.Entries.Count);
            using var output = new MemoryStream();
            new TokenWriter().WriteToken(dictionary, output);
            var input = StringBytesTestConverter.Convert(Encoding.ASCII.GetString(output.ToArray()));
            Assert.True(new DictionaryTokenizer(new StackDepthGuard(256)).TryTokenize(input.First, input.Bytes, out var token));
            Assert.Equal(dictionary, Assert.IsType<DictionaryToken>(token));
        }

        [Fact]
        public void EveryNonNullByteSurvivesWritingAndParsing()
        {
            for (var value = 1; value <= 255; value++)
            {
                var name = Parse("/#" + value.ToString("X2"));
                using var output = new MemoryStream();
                new TokenWriter().WriteToken(name, output);
                var reread = Parse(Encoding.ASCII.GetString(output.ToArray()));
                Assert.Equal(new byte[] { (byte)value }, reread.GetBytes());
                Assert.Equal(name, reread);
            }
        }

        [Fact]
        public void DictionaryLookupAndEditsKeepBothNames()
        {
            var utf8 = Parse("/#C3#A9");
            var latin1 = Parse("/#E9");
            var dictionary = new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                [utf8] = new NumericToken(1),
                [latin1] = new NumericToken(2)
            });
            Assert.Equal(2, dictionary.Entries.Count);
            Assert.Same(utf8, dictionary.Entries.Keys.First(x => x == utf8));
            Assert.Equal(2, dictionary.Data.Count);
            Assert.True(dictionary.TryGet<NumericToken>(utf8, out var first));
            Assert.Equal(1, first.Int);
            Assert.True(dictionary.TryGet<NumericToken>(latin1, out var second));
            Assert.Equal(2, second.Int);
            var edited = dictionary.With(latin1, new NumericToken(3)).Without(utf8);
            Assert.False(edited.ContainsKey(utf8));
            Assert.True(edited.TryGet<NumericToken>(latin1, out var remaining));
            Assert.Equal(3, remaining.Int);
            using var output = new MemoryStream();
            new TokenWriter().WriteToken(dictionary, output);
            var input = StringBytesTestConverter.Convert(Encoding.ASCII.GetString(output.ToArray()));
            Assert.True(new DictionaryTokenizer(new StackDepthGuard(256)).TryTokenize(input.First, input.Bytes, out var token));
            Assert.Equal(dictionary, Assert.IsType<DictionaryToken>(token));
        }
    }
}
