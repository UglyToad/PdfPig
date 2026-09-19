using System.IO;
using System.Text;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Tokenization;
using UglyToad.PdfPig.Tokens;
using UglyToad.PdfPig.Writer;

namespace UglyToad.PdfPig.Tests.Writer
{
    /// <summary>
    /// A string token holds the bytes of its string, so writing one and reading it back gives the
    /// same bytes whatever encoding its text was decoded with.
    /// </summary>
    public class TokenWriterStringTests
    {
        private static StringToken WriteAndReadBack(StringToken token)
        {
            var stream = new MemoryStream();

            new TokenWriter().WriteToken(token, stream);

            var written = stream.ToArray();

            // The writer emits the enclosing brackets, and the tokenizer is handed the opening one.
            Assert.Equal((byte)'(', written[0]);

            var input = new MemoryInputBytes(written.AsMemory(1));

            Assert.True(new StringTokenizer().TryTokenize((byte)'(', input, out var read));

            return Assert.IsType<StringToken>(read);
        }

        /// <summary>
        /// UTF-16 bytes were written raw, so a string whose bytes happened to include a bracket or a
        /// backslash closed or escaped the literal early and the output was not a valid string.
        /// </summary>
        [Fact]
        public void Utf16StringWhoseBytesContainABracketSurvives()
        {
            // "A(" in UTF-16BE, whose second character is the byte 0x28.
            var bytes = new byte[] { 0xFE, 0xFF, 0x00, 0x41, 0x00, 0x28 };

            var token = WriteAndReadBack(new StringToken(bytes));

            Assert.Equal(bytes, token.GetBytes());
            Assert.Equal("A(", token.Data);
        }

        [Fact]
        public void Utf16StringWhoseBytesContainABackslashSurvives()
        {
            // "A\" in UTF-16BE, whose second character is the byte 0x5C.
            var bytes = new byte[] { 0xFE, 0xFF, 0x00, 0x41, 0x00, 0x5C };

            var token = WriteAndReadBack(new StringToken(bytes));

            Assert.Equal(bytes, token.GetBytes());
            Assert.Equal("A\\", token.Data);
        }

        /// <summary>
        /// PdfDocEncoding gives some single bytes a character above U+00FF, which used to send the
        /// whole string out as UTF-16BE rather than as the bytes it was read from.
        /// </summary>
        [Fact]
        public void PdfDocEncodedStringKeepsItsBytes()
        {
            var bytes = new byte[] { 0x48, 0x69, 0x18 }; // "Hi" then a breve, U+02D8

            var token = WriteAndReadBack(new StringToken(bytes));

            Assert.Equal(bytes, token.GetBytes());
        }

        [Fact]
        public void SingleByteStringKeepsItsBytes()
        {
            var bytes = new byte[] { 0x28, 0x5C, 0x29, 0x0A, 0x80, 0xFF };

            var token = WriteAndReadBack(new StringToken(bytes));

            Assert.Equal(bytes, token.GetBytes());
        }

        /// <summary>
        /// Text that single bytes cannot hold is still written as UTF-16BE rather than being
        /// flattened, which is what the document information dictionary relies on.
        /// </summary>
        [Fact]
        public void TextCreatedFromAStringBeyondLatin1IsWrittenAsUtf16()
        {
            var token = WriteAndReadBack(new StringToken("日本"));

            Assert.Equal("日本", token.Data);
        }
    }
}
