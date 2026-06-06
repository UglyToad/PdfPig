namespace UglyToad.PdfPig.Tests.Tokenization
{
    using System.Text;
    using PdfPig.Core;
    using PdfPig.Tokenization;
    using PdfPig.Tokens;

    public class NameTokenizerTests
    {
        private readonly NameTokenizer tokenizer = new NameTokenizer();
        
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
        [InlineData("/A−Name_With;Various***Characters?", "A−Name_With;Various***Characters?")]
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

        [Fact]
        public void ReadsGbkEncodedCjkName()
        {
            // "/ABCDEE+黑体" where 黑体 is written as raw GBK (codepage 936) bytes
            // BA DA = 黑, CC E5 = 体. Not valid UTF-8, so it must be detected as GBK.
            var raw = new byte[] { (byte)'/', (byte)'A', (byte)'B', (byte)'C', (byte)'D', (byte)'E', (byte)'E', (byte)'+', 0xBA, 0xDA, 0xCC, 0xE5 };
            var input = new MemoryInputBytes(raw);
            input.MoveNext();

            var result = tokenizer.TryTokenize(input.CurrentByte, input, out var token);

            Assert.True(result);
            Assert.Equal("ABCDEE+黑体", AssertNameToken(token).Data);
        }
        
        [Fact]
        public void ReadsGbkEncodedCjkNameFromRawBytes()
        {
            // A plain name "/黑体" written as raw GBK (codepage 936) bytes: BA DA = 黑, CC E5 = 体.
            // This is not valid UTF-8, so it must be detected and decoded as GBK. The decoding is not
            // specific to font names - any name token benefits.
            var raw = new byte[] { (byte)'/', 0xBA, 0xDA, 0xCC, 0xE5 };

            Assert.Equal("黑体", TokenizeRaw(raw));
        }

        [Fact]
        public void ReadsGbkEncodedCjkNameWithTrailingAscii()
        {
            // "/ABCDEE+微软雅黑,Bold" with the CJK part as raw GBK bytes.
            var raw = new byte[]
            {
                (byte)'/', (byte)'A', (byte)'B', (byte)'C', (byte)'D', (byte)'E', (byte)'E', (byte)'+',
                0xCE, 0xA2, 0xC8, 0xED, 0xD1, 0xC5, 0xBA, 0xDA,
                (byte)',', (byte)'B', (byte)'o', (byte)'l', (byte)'d'
            };
            var input = new MemoryInputBytes(raw);
            input.MoveNext();

            var result = tokenizer.TryTokenize(input.CurrentByte, input, out var token);

            Assert.True(result);
            Assert.Equal("ABCDEE+微软雅黑,Bold", AssertNameToken(token).Data);
        }

        [Fact]
        public void ReadsGbkEncodedCjkNameInterspersedWithAscii()
        {
            // ASCII characters before and after the GBK CJK bytes in an arbitrary (non-font) name:
            // "/X黑体Y".
            var raw = BuildRawName("X", "黑体", "Y");

            Assert.Equal("X黑体Y", TokenizeRaw(raw));
        }

        [Fact]
        public void IsolatedHighByteFallsBackToWindows1252()
        {
            // "/Café" where é is a single raw 0xE9 (Latin-1/Windows-1252) byte, not a valid GBK
            // double-byte sequence. Must NOT be mis-decoded as GBK.
            var raw = new byte[] { (byte)'/', (byte)'C', (byte)'a', (byte)'f', 0xE9 };
            var input = new MemoryInputBytes(raw);
            input.MoveNext();

            var result = tokenizer.TryTokenize(input.CurrentByte, input, out var token);

            Assert.True(result);
            Assert.Equal("Café", AssertNameToken(token).Data);
        }

        [Theory]
        [InlineData("黑体")]            // SimHei
        [InlineData("宋体")]            // SimSun
        [InlineData("仿宋")]            // FangSong
        [InlineData("微软雅黑")]        // Microsoft YaHei
        public void ReadsGbkEncodedCjkNames(string cjk)
        {
            // Build "/<cjk>" with the CJK characters encoded as raw GBK (codepage 936) bytes, the way
            // the affected producers write them. These bytes are never valid UTF-8 for real CJK text.
            var raw = BuildRawName(null, cjk, null);

            Assert.Equal(cjk, TokenizeRaw(raw));
        }

        [Theory]
        [InlineData("黑体")]            // SimHei
        [InlineData("宋体")]            // SimSun
        [InlineData("仿宋")]            // FangSong
        [InlineData("微软雅黑")]        // Microsoft YaHei
        public void ReadsFontGbkEncodedCjkNames(string cjk)
        {
            // Build "/ABCDEE+<cjk>" with the CJK part encoded as raw GBK (codepage 936) bytes, the way
            // the affected producers write them. These bytes are never valid UTF-8 for real CJK text.
            var raw = BuildRawName("ABCDEE+", cjk, null);

            Assert.Equal("ABCDEE+" + cjk, TokenizeRaw(raw));
        }

        [Fact]
        public void ReadsGbkEncodedCjkNameWithSubsetPrefixAndStyleSuffix()
        {
            // GBK CJK characters between an ASCII subset prefix and an ASCII ",Bold" style suffix.
            var raw = BuildRawName("ABCDEE+", "微软雅黑", ",Bold");

            Assert.Equal("ABCDEE+微软雅黑,Bold", TokenizeRaw(raw));
        }

        [Fact]
        public void ReadsGbkEncodedCjkNameWithEscapedBytes()
        {
            // The GBK bytes for 黑体 (BA DA CC E5) written using #XX hex escapes, which is the
            // spec-compliant way to embed non-ASCII bytes in a name. The result must still be GBK.
            var input = StringBytesTestConverter.Convert("/#BA#DA#CC#E5");

            var result = tokenizer.TryTokenize(input.First, input.Bytes, out var token);

            Assert.True(result);
            Assert.Equal("黑体", AssertNameToken(token).Data);
        }

        [Fact]
        public void ReadsFontGbkEncodedCjkNameWithEscapedBytes()
        {
            // Same GBK bytes for 黑体 (BA DA CC E5) but written using #XX hex escapes, which is the
            // spec-compliant way to embed non-ASCII bytes in a name. The result must still be GBK.
            var input = StringBytesTestConverter.Convert("/ABCDEE+#BA#DA#CC#E5");

            var result = tokenizer.TryTokenize(input.First, input.Bytes, out var token);

            Assert.True(result);
            Assert.Equal("ABCDEE+黑体", AssertNameToken(token).Data);
        }

        [Fact]
        public void PrefersUtf8OverGbkForValidUtf8Cjk()
        {
            // 黑体 written as valid UTF-8 (E9 BB 91 E4 BD 93). UTF-8 is checked first (PDF 2.0, 7.3.5),
            // so a genuinely UTF-8 encoded CJK name must not be re-interpreted as GBK.
            var raw = new byte[] { (byte)'/', 0xE9, 0xBB, 0x91, 0xE4, 0xBD, 0x93 };

            Assert.Equal("黑体", TokenizeRaw(raw));
        }

        [Fact]
        public void GbkLeadByteAtLowerBoundaryIsDetected()
        {
            // 0x81 is the lowest GBK lead byte, 0x40 the lowest trail byte.
            var raw = new byte[] { (byte)'/', 0x81, 0x40 };

            Assert.Equal(DecodeGbk(new byte[] { 0x81, 0x40 }), TokenizeRaw(raw));
        }

        [Fact]
        public void GbkLeadByteAtUpperBoundaryIsDetected()
        {
            // 0xFE is the highest GBK lead byte, 0xFE the highest trail byte.
            var raw = new byte[] { (byte)'/', 0xFE, 0xFE };

            Assert.Equal(DecodeGbk(new byte[] { 0xFE, 0xFE }), TokenizeRaw(raw));
        }

        [Theory]
        [InlineData(0x80)] // 0x80 is not a valid GBK lead byte
        [InlineData(0xFF)] // 0xFF is not a valid GBK lead byte
        public void InvalidGbkLeadByteFallsBackToWindows1252(int lead)
        {
            var raw = new byte[] { (byte)'/', (byte)lead, (byte)'A' };

            Assert.Equal(Decode1252(new byte[] { (byte)lead, (byte)'A' }), TokenizeRaw(raw));
        }

        [Fact]
        public void GbkTrailByte0x7FIsRejectedAndFallsBackToWindows1252()
        {
            // 0x7F is excluded from the GBK trail range, so 0x81 0x7F is not a valid sequence.
            var raw = new byte[] { (byte)'/', 0x81, 0x7F };

            Assert.Equal(Decode1252(new byte[] { 0x81, 0x7F }), TokenizeRaw(raw));
        }

        [Fact]
        public void DanglingGbkLeadByteAtEndFallsBackToWindows1252()
        {
            // A lead byte with no following trail byte (truncated) is not a valid GBK sequence.
            var raw = new byte[] { (byte)'/', (byte)'A', 0x81 };

            Assert.Equal(Decode1252(new byte[] { (byte)'A', 0x81 }), TokenizeRaw(raw));
        }

        [Fact]
        public void HighByteFollowedBySubTrailRangeAsciiFallsBackToWindows1252()
        {
            // "/caf<E9>2" — é (E9) is followed by the digit '2' (0x32), which is below the GBK trail
            // range (0x40-0xFE). The pair is therefore invalid GBK and the name stays Windows-1252.
            var name = new byte[] { (byte)'c', (byte)'a', (byte)'f', 0xE9, (byte)'2' };
            var raw = new byte[] { (byte)'/' }.Concat(name).ToArray();

            Assert.Equal(Decode1252(name), TokenizeRaw(raw));
        }

        [Fact]
        public void Gb18030FourByteSequenceFallsBackToWindows1252()
        {
            // GB18030 four-byte sequences use a 0x30-0x39 second byte, which is below the GBK trail
            // range, so they are not treated as GBK (codepage 936 cannot decode them anyway).
            var raw = new byte[] { (byte)'/', 0x81, 0x30, 0x81, 0x30 };

            Assert.Equal(Decode1252(new byte[] { 0x81, 0x30, 0x81, 0x30 }), TokenizeRaw(raw));
        }

        [Fact]
        public void SingleWindows1252HighByteIsPreserved()
        {
            // 0x80 is the Euro sign in Windows-1252; a lone occurrence must keep that meaning.
            var raw = new byte[] { (byte)'/', 0x80 };

            Assert.Equal("€", TokenizeRaw(raw));
        }

        // Codepage encodings require the provider that NameTokenizer's static constructor registers, so
        // these are resolved at call time (after a NameTokenizer instance exists) rather than in static
        // field initializers which could run first.
        private static string DecodeGbk(byte[] bytes) => Encoding.GetEncoding(936).GetString(bytes);

        private static string Decode1252(byte[] bytes) => Encoding.GetEncoding("windows-1252").GetString(bytes);

        private static byte[] BuildRawName(string asciiPrefix, string cjk, string asciiSuffix)
        {
            var bytes = new System.Collections.Generic.List<byte> { (byte)'/' };
            if (!string.IsNullOrEmpty(asciiPrefix))
            {
                bytes.AddRange(Encoding.ASCII.GetBytes(asciiPrefix));
            }

            bytes.AddRange(Encoding.GetEncoding(936).GetBytes(cjk));
            if (!string.IsNullOrEmpty(asciiSuffix))
            {
                bytes.AddRange(Encoding.ASCII.GetBytes(asciiSuffix));
            }

            return bytes.ToArray();
        }

        private string TokenizeRaw(byte[] raw)
        {
            var input = new MemoryInputBytes(raw);
            input.MoveNext();

            var result = tokenizer.TryTokenize(input.CurrentByte, input, out var token);

            Assert.True(result);

            return AssertNameToken(token).Data;
        }

        private static NameToken AssertNameToken(IToken token)
        {
            Assert.NotNull(token);

            var result = Assert.IsType<NameToken>(token);

            return result;
        }
    }
}
