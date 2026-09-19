using System.IO;
using System.Linq;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Graphics;
using UglyToad.PdfPig.Graphics.Operations;
using UglyToad.PdfPig.Graphics.Operations.TextShowing;
using UglyToad.PdfPig.Logging;
using UglyToad.PdfPig.Parser;

namespace UglyToad.PdfPig.Tests.Graphics
{
    /// <summary>
    /// Writing an operation emits the operands followed by its operator symbol, so a content stream
    /// rebuilt from parsed operations parses back to the same operations.
    /// </summary>
    public class TextShowingOperationWriteTests
    {
        private static readonly PageContentParser Parser =
            new PageContentParser(ReflectionGraphicsStateOperationFactory.Instance, new StackDepthGuard(256));

        private static T WriteAndParseBack<T>(IGraphicsStateOperation operation)
        {
            var stream = new MemoryStream();

            operation.Write(stream);

            var parsed = Parser.Parse(1, new MemoryInputBytes(stream.ToArray()), new NoOpLog());

            return Assert.IsType<T>(Assert.Single(parsed));
        }

        [Fact]
        public void MoveToNextLineShowTextRoundTrips()
        {
            var parsed = WriteAndParseBack<MoveToNextLineShowText>(new MoveToNextLineShowText("Hi"));

            Assert.Equal("Hi", parsed.Text);
        }

        /// <summary>
        /// The <c>"</c> operator's operands are meaningless without the symbol that follows them:
        /// omitting it dropped the spacing and the text from the rebuilt stream.
        /// </summary>
        [Fact]
        public void MoveToNextLineShowTextWithSpacingRoundTrips()
        {
            var parsed = WriteAndParseBack<MoveToNextLineShowTextWithSpacing>(
                new MoveToNextLineShowTextWithSpacing(1, 2, "Hi"));

            Assert.Equal(1, parsed.WordSpacing);
            Assert.Equal(2, parsed.CharacterSpacing);
            Assert.Equal("Hi", parsed.Text);
        }

        /// <summary>
        /// An end-of-line marker inside a literal string that no backslash precedes shall be read as
        /// a single line feed, so writing a carriage return as it stands hands a conforming reader a
        /// different character code, and a carriage return line feed pair one code instead of two.
        /// The marker has to be escaped on the way out. Our own tokenizer takes a bare carriage
        /// return at face value, so only the written bytes show this.
        /// </summary>
        [Fact]
        public void ShowTextEscapesEndOfLineCharacterCodes()
        {
            var codes = new byte[] { (byte)'a', 0x0D, (byte)'b', 0x0A, (byte)'c', 0x0D, 0x0A, (byte)'d' };

            var stream = new MemoryStream();

            new ShowText(OtherEncodings.BytesAsLatin1String(codes)).Write(stream);

            Assert.StartsWith(@"(a\rb\nc\r\nd) Tj", OtherEncodings.BytesAsLatin1String(stream.ToArray()));
        }

        /// <summary>
        /// The escaped markers still come back as the character codes they stand for.
        /// </summary>
        [Fact]
        public void ShowTextWithEndOfLineCharacterCodesRoundTrips()
        {
            var codes = new byte[] { (byte)'a', 0x0D, (byte)'b', 0x0A, (byte)'c', 0x0D, 0x0A, (byte)'d' };

            var parsed = WriteAndParseBack<ShowText>(new ShowText(OtherEncodings.BytesAsLatin1String(codes)));

            Assert.Equal(codes, OtherEncodings.StringAsLatin1Bytes(parsed.Text));
        }
    }
}
