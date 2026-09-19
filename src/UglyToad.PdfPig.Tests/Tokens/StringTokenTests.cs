using System.Linq;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Tokenization;
using UglyToad.PdfPig.Tokens;

namespace UglyToad.PdfPig.Tests.Tokens
{
    /// <summary>
    /// A string token holds the bytes of its string. Those bytes are what a text showing operand
    /// needs, and <see cref="StringToken.Data"/> reads them as a text string (7.9.2.2) for the
    /// entries that are text.
    /// </summary>
    public class StringTokenTests
    {
        private static StringToken Tokenize(params byte[] literalContents)
        {
            // The tokenizer is handed the bytes between the brackets, having already read the '('.
            var input = new MemoryInputBytes(literalContents.Concat(new byte[] { (byte)')' }).ToArray());

            Assert.True(new StringTokenizer().TryTokenize((byte)'(', input, out var token));

            return Assert.IsType<StringToken>(token);
        }

        [Fact]
        public void KeepsTheBytesItWasReadFrom()
        {
            // Character codes for the current font, which must survive untouched.
            var bytes = new byte[] { 0x00, 0x41, 0xFE, 0xFF, 0x80 };

            var token = Tokenize(bytes);

            Assert.Equal(bytes, token.Bytes.ToArray());
            Assert.Equal(bytes, token.GetBytes());
        }

        /// <summary>
        /// An unpaired surrogate does not survive being decoded and encoded again, so this is the
        /// case that shows the bytes are kept rather than reconstructed from the text.
        /// </summary>
        [Fact]
        public void KeepsBytesThatDecodingCannotReverse()
        {
            var bytes = new byte[] { 0xFE, 0xFF, 0x00, 0x41, 0xD8, 0x00 };

            Assert.Equal(bytes, Tokenize(bytes).GetBytes());
        }

        [Fact]
        public void ReadsUtf16BigEndianTextWhenAByteOrderMarkSaysSo()
        {
            // The spec-correct spelling of a single space.
            Assert.Equal(" ", Tokenize(0xFE, 0xFF, 0x00, 0x20).Data);

            // "Hi" then U+4E2D, which is beyond what single bytes hold.
            Assert.Equal("Hi中", Tokenize(0xFE, 0xFF, 0x00, 0x48, 0x00, 0x69, 0x4E, 0x2D).Data);
        }

        [Fact]
        public void ReadsUtf8TextWhenAByteOrderMarkSaysSo()
        {
            Assert.Equal("Hi", Tokenize(0xEF, 0xBB, 0xBF, 0x48, 0x69).Data);
        }

        /// <summary>
        /// Not a text string encoding the specification defines, but it is accepted on the way in.
        /// </summary>
        [Fact]
        public void ReadsUtf16LittleEndianTextWhenAByteOrderMarkSaysSo()
        {
            Assert.Equal("Hi", Tokenize(0xFF, 0xFE, 0x48, 0x00, 0x69, 0x00).Data);
        }

        [Fact]
        public void ReadsPdfDocEncodedTextWithoutAByteOrderMark()
        {
            Assert.Equal("Hi", Tokenize(0x48, 0x69).Data);

            // 0x18 is a breve in PdfDocEncoding, which is beyond Latin 1.
            Assert.Equal("Hi˘", Tokenize(0x48, 0x69, 0x18).Data);
        }

        [Fact]
        public void ReadsTheSameTextAsAHexTokenOfTheSameBytes()
        {
            var token = Tokenize(0xFE, 0xFF, 0x00, 0x48);

            Assert.Equal(new HexToken("FEFF0048".AsSpan()).Data, token.Data);
        }

        [Fact]
        public void TextThatPdfDocEncodingHoldsIsStoredAsPdfDocEncoding()
        {
            var token = new StringToken("Hi");

            Assert.Equal(new byte[] { 0x48, 0x69 }, token.GetBytes());
            Assert.Equal("Hi", token.Data);
        }

        /// <summary>
        /// Text beyond PdfDocEncoding is stored as UTF-16 rather than being flattened, which is what
        /// the document information dictionary relies on.
        /// </summary>
        [Fact]
        public void TextBeyondPdfDocEncodingIsStoredAsUtf16()
        {
            var token = new StringToken("日本");

            Assert.Equal(new byte[] { 0xFE, 0xFF, 0x65, 0xE5, 0x67, 0x2C }, token.GetBytes());
            Assert.Equal("日本", token.Data);
        }

        /// <summary>
        /// PdfDocEncoding has bytes for U+00FE and U+00FF, so text opening with them would be stored
        /// as the bytes FE FF and read back as a UTF-16 byte order mark. Such text is stored as
        /// UTF-16 instead so that it survives the round trip.
        /// </summary>
        [Theory]
        [InlineData("þÿA")]
        [InlineData("ÿþA")]
        [InlineData("ï»¿A")]
        public void TextThatWouldEncodeToAByteOrderMarkRoundTrips(string text)
        {
            var token = new StringToken(text);

            Assert.Equal(text, new StringToken(token.GetBytes()).Data);
        }

        [Fact]
        public void TokensWithTheSameBytesAreEqual()
        {
            var bytes = new byte[] { 0xFE, 0xFF, 0x00, 0x48 };

            Assert.Equal(new StringToken(bytes), new StringToken(bytes.ToArray()));
            Assert.Equal(new StringToken(bytes).GetHashCode(), new StringToken(bytes.ToArray()).GetHashCode());

            Assert.NotEqual(new StringToken(bytes), new StringToken(new byte[] { 0x48 }));
        }
    }
}
