namespace UglyToad.PdfPig.Tests.Tokenization
{
    using System.IO;
    using System.Text;
    using PdfPig.Core;
    using PdfPig.Tokenization.Scanner;
    using PdfPig.Tokens;
    using PdfPig.Writer;

    /// <summary>
    /// PDF 2.0 added UTF-8, marked by a byte order mark, as a text string encoding alongside
    /// PDFDocEncoding and UTF-16BE, see ISO 32000-2, 7.9.2.2. A mark only carries that meaning in a
    /// text string, so none of the three is looked for in the character codes of a content stream.
    /// </summary>
    public class Utf8TextStringTests
    {
        // Non-ASCII in two and three byte forms, and an ASCII run, so a mis-decode cannot pass unnoticed.
        private const string Text = "Caf\u00e9 \u2014 na\u00efve";

        [Fact]
        public void LiteralStringWithUtf8ByteOrderMarkIsDecoded()
        {
            var token = (StringToken)ScanOne(Literal(Utf8WithBom(Text)), usePdfDocEncoding: true);

            Assert.Equal(Text, token.Data);
            Assert.Equal(StringToken.Encoding.Utf8, token.EncodedWith);
        }

        [Fact]
        public void LiteralStringKeepsItsOriginalBytes()
        {
            // The raw bytes have to survive, they are what the encryption and file identifier entries are read from.
            var raw = Utf8WithBom(Text);

            var token = (StringToken)ScanOne(Literal(raw), usePdfDocEncoding: true);

            Assert.Equal(raw, token.GetBytes());
        }

        [Fact]
        public void HexStringWithUtf8ByteOrderMarkIsDecoded()
        {
            var token = (HexToken)ScanOne(Hex(Utf8WithBom(Text)), usePdfDocEncoding: true);

            Assert.Equal(Text, token.Data);
        }

        [Fact]
        public void ContentStreamStringIsNotTreatedAsUtf8()
        {
            // The operand of a text showing operator is a sequence of character codes, not a text string,
            // so the bytes have to reach the font untouched however they happen to start.
            var raw = Utf8WithBom(Text);

            var token = (StringToken)ScanOne(Literal(raw), usePdfDocEncoding: false);

            Assert.NotEqual(StringToken.Encoding.Utf8, token.EncodedWith);
            Assert.Equal(raw.Length, token.Data.Length);
            Assert.Equal(raw, token.GetBytes());
        }

        [Fact]
        public void ContentStreamStringIsNotTreatedAsUtf16()
        {
            // Same reasoning as UTF-8. A simple font maps 0xFE and 0xFF to glyphs like any other code,
            // and a string opening with them is not a text string carrying a mark.
            var raw = new byte[] { 0xFE, 0xFF }.Concat(Encoding.BigEndianUnicode.GetBytes(Text)).ToArray();

            var token = (StringToken)ScanOne(Literal(raw), usePdfDocEncoding: false);

            Assert.Equal(StringToken.Encoding.Iso88591, token.EncodedWith);
            Assert.Equal(raw.Length, token.Data.Length);
            Assert.Equal(raw, token.GetBytes());
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void StringWithoutAByteOrderMarkIsUnchanged(bool usePdfDocEncoding)
        {
            var token = (StringToken)ScanOne(Literal(Encoding.ASCII.GetBytes("Cafe")), usePdfDocEncoding);

            Assert.Equal("Cafe", token.Data);
            Assert.NotEqual(StringToken.Encoding.Utf8, token.EncodedWith);
        }

        [Fact]
        public void Utf16ByteOrderMarkStillWins()
        {
            var raw = new byte[] { 0xFE, 0xFF }.Concat(Encoding.BigEndianUnicode.GetBytes(Text)).ToArray();

            var literal = (StringToken)ScanOne(Literal(raw), usePdfDocEncoding: true);
            var hex = (HexToken)ScanOne(Hex(raw), usePdfDocEncoding: true);

            Assert.Equal(Text, literal.Data);
            Assert.Equal(StringToken.Encoding.Utf16BE, literal.EncodedWith);
            Assert.Equal(Text, hex.Data);
        }

        [Fact]
        public void GetBytesAddsTheByteOrderMarkForAConstructedToken()
        {
            var token = new StringToken(Text, StringToken.Encoding.Utf8);

            Assert.Equal(Utf8WithBom(Text), token.GetBytes());
        }

        [Fact]
        public void WrittenUtf8StringReadsBackUnchanged()
        {
            // UTF-8 leaves ASCII as it stands, so parentheses and backslashes in the text reach the
            // output as themselves and have to be escaped, unlike the same text in UTF-16.
            const string tricky = "a (b) c \\ d \\( e \u00e9";

            using var ms = new MemoryStream();
            TokenWriter.Instance.WriteToken(new StringToken(tricky, StringToken.Encoding.Utf8), ms);

            var token = (StringToken)ScanOne(ms.ToArray(), usePdfDocEncoding: true);

            Assert.Equal(tricky, token.Data);
        }

        private static byte[] Utf8WithBom(string text)
        {
            var body = Encoding.UTF8.GetBytes(text);

            var result = new byte[body.Length + 3];
            result[0] = 0xEF;
            result[1] = 0xBB;
            result[2] = 0xBF;
            body.CopyTo(result, 3);

            return result;
        }

        private static byte[] Literal(byte[] payload)
        {
            var result = new byte[payload.Length + 2];
            result[0] = (byte)'(';
            payload.CopyTo(result, 1);
            result[result.Length - 1] = (byte)')';

            return result;
        }

        private static byte[] Hex(byte[] payload)
        {
            var builder = new StringBuilder("<");
            foreach (var b in payload)
            {
                builder.Append(b.ToString("X2"));
            }

            builder.Append('>');

            return OtherEncodings.StringAsLatin1Bytes(builder.ToString());
        }

        private static IToken ScanOne(byte[] input, bool usePdfDocEncoding)
        {
            var bytes = new MemoryInputBytes(input);
            var scanner = new CoreTokenScanner(bytes, usePdfDocEncoding, new StackDepthGuard(256), ScannerScope.None);

            Assert.True(scanner.MoveNext());

            return scanner.CurrentToken;
        }
    }
}
