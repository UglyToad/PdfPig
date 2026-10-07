namespace UglyToad.PdfPig.Tests.Filters
{
    using System.IO;
    using UglyToad.PdfPig.Filters;
    using UglyToad.PdfPig.Fonts;
    using UglyToad.PdfPig.Tokens;
    using UglyToad.PdfPig.Filters.CcittFax;
    using UglyToad.PdfPig.Tests.Images;

    public class CcittFaxDecoderStreamTests
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ByteAlignmentRetainsPrefetchedNextRow(bool lenient)
        {
            // First row: white one, black one, white six (13 bits plus byte padding).
            // Looking ahead for the final short code can prefetch the second white row.
            using var decoder = new CcittFaxDecoderStream(new MemoryStream(new byte[] { 0x1D, 0x70, 0x98 }), 8, CcittFaxCompressionType.ModifiedHuffman, true, lenient);
            Assert.Equal(0x40, decoder.ReadByte());
            Assert.Equal(0x00, decoder.ReadByte());
        }

        [Fact]
        public void DecoderRejectsSeekingWithoutChangingDecodedState()
        {
            using var decoder = new CcittFaxDecoderStream(new MemoryStream(new byte[] { 0x98, 0xA0 }), 16, CcittFaxCompressionType.ModifiedHuffman, false);
            Assert.False(decoder.CanSeek);
            Assert.Equal(0, decoder.ReadByte());
            Assert.Throws<NotSupportedException>(() => decoder.Seek(0, SeekOrigin.Begin));
            Assert.Throws<NotSupportedException>(() => { _ = decoder.Position; });
            Assert.Throws<NotSupportedException>(() => decoder.Position = 0);
            Assert.Throws<NotSupportedException>(() => { _ = decoder.Length; });
            Assert.Equal(0xFF, decoder.ReadByte());
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void OversizedRunRespectsParsingMode(bool lenient)
        {
            using var decoder = new CcittFaxDecoderStream(new MemoryStream(new byte[] { 0xA8 }), 8, CcittFaxCompressionType.ModifiedHuffman, false, lenient);
            if (lenient) Assert.Equal(0, decoder.ReadByte());
            else Assert.Throws<CorruptCompressedDataException>(() => decoder.ReadByte());
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void UnknownTwoDimensionalCodeRespectsParsingMode(bool lenient)
        {
            using var decoder = new CcittFaxDecoderStream(new MemoryStream(new byte[] { 0x00, 0x80 }), 8, CcittFaxCompressionType.Group4_2D, false, lenient);
            if (lenient) Assert.Equal(0, decoder.ReadByte());
            else Assert.Throws<CorruptCompressedDataException>(() => decoder.ReadByte());
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void NegativeVerticalPositionRespectsParsingMode(bool lenient)
        {
            using var decoder = new CcittFaxDecoderStream(new MemoryStream(new byte[] { 0x05 }), 1, CcittFaxCompressionType.Group4_2D, false, lenient);
            if (lenient) Assert.Equal(0x80, decoder.ReadByte());
            else Assert.Throws<CorruptCompressedDataException>(() => decoder.ReadByte());
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Group4EndOfBlockRetainsZeroPadding(bool lenient)
        {
            // Two consecutive EOL codes form a legal Group 4 EOFB marker.
            using var decoder = new CcittFaxDecoderStream(new MemoryStream(new byte[] { 0x00, 0x10, 0x01 }), 8, CcittFaxCompressionType.Group4_2D, false, lenient);
            Assert.Equal(0, decoder.ReadByte());
            Assert.Equal(0, decoder.ReadByte());
        }

        [Fact]
        public void RunAccumulationOverflowIsRejectedEvenInLenientMode()
        {
            // Each 12-bit 000000011111 makeup code adds 2560 white pixels. Two fit
            // into three bytes; enough repetitions overflow Int32 without a huge bitmap.
            var pairs = (int.MaxValue / 2560 + 2) / 2;
            var input = new byte[pairs * 3];
            for (var i = 0; i < input.Length; i += 3)
            {
                input[i] = 0x01;
                input[i + 1] = 0xF0;
                input[i + 2] = 0x1F;
            }
            using var decoder = new CcittFaxDecoderStream(new MemoryStream(input), 8, CcittFaxCompressionType.ModifiedHuffman, false, true);
            var exception = Assert.Throws<CorruptCompressedDataException>(() => decoder.ReadByte());
            Assert.IsType<OverflowException>(exception.InnerException);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ActualEndOfInputStillPadsWithZeros(bool lenient)
        {
            using var decoder = new CcittFaxDecoderStream(new MemoryStream(new byte[] { 0x00 }), 8, CcittFaxCompressionType.ModifiedHuffman, false, lenient);
            var output = new byte[] { 0xAA, 0xAA };
            Assert.Equal(2, decoder.Read(output, 0, output.Length));
            Assert.Equal(new byte[] { 0, 0 }, output);
            Assert.Equal(0, decoder.ReadByte());
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void InvalidHuffmanCodeRespectsParsingMode(bool lenient)
        {
            using var decoder = new CcittFaxDecoderStream(new MemoryStream(new byte[] { 0x00, 0x80 }), 8, CcittFaxCompressionType.ModifiedHuffman, false, lenient);
            if (lenient)
            {
                Assert.Equal(0, decoder.ReadByte());
                Assert.Equal(0, decoder.ReadByte());
            }
            else
            {
                var exception = Assert.Throws<CorruptCompressedDataException>(() => decoder.ReadByte());
                Assert.Equal("Unknown code in Huffman RLE stream", exception.Message);
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void FilterPassesParsingModeToDecoder(bool lenient)
        {
            var parameters = new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                [NameToken.Columns] = new NumericToken(8),
                [NameToken.Rows] = new NumericToken(1),
                [NameToken.EndOfLine] = BooleanToken.False,
                [NameToken.BlackIs1] = BooleanToken.True
            });
            var dictionary = new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                [NameToken.Filter] = NameToken.CcittfaxDecode,
                [NameToken.DecodeParms] = parameters
            });
            var filter = new CcittFaxDecodeFilter(lenient);
            var input = new byte[] { 0x00, 0x80 };
            if (lenient)
            {
                Assert.Equal(new byte[] { 0 }, filter.Decode(input, dictionary, TestFilterProvider.Instance, 0).ToArray());
            }
            else
            {
                Assert.Throws<CorruptCompressedDataException>(() => filter.Decode(input, dictionary, TestFilterProvider.Instance, 0));
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ExcessZeroLengthRunsProduceACompressedDataException(bool lenient)
        {
            // Alternating white-zero and black-zero runs exceed the three change entries
            // available for a one-pixel row without advancing the output position.
            var bits = string.Concat(Enumerable.Repeat("001101010000110111", 4));
            var input = Enumerable.Range(0, (bits.Length + 7) / 8)
                .Select(i => Convert.ToByte(bits.Substring(i * 8, Math.Min(8, bits.Length - i * 8)).PadRight(8, '0'), 2)).ToArray();
            using var decoder = new CcittFaxDecoderStream(new MemoryStream(input), 1, CcittFaxCompressionType.ModifiedHuffman, false, lenient);
            var exception = Assert.Throws<CorruptCompressedDataException>(() => decoder.Read(new byte[1], 0, 1));
            Assert.IsType<IndexOutOfRangeException>(exception.InnerException);
        }

#if NET
        [Fact]
        public void StrictSpanReadRejectsInvalidHuffmanCode()
        {
            using var decoder = new CcittFaxDecoderStream(new MemoryStream(new byte[] { 0x00, 0x80 }), 8, CcittFaxCompressionType.ModifiedHuffman, false, false);
            Assert.Throws<CorruptCompressedDataException>(() => decoder.Read(new byte[1].AsSpan()));
        }
#endif

#if NET
        [Theory]
        [InlineData(1)]
        [InlineData(17)]
        [InlineData(512)]
        [InlineData(65536)]
        public void SpanReadsMatchArrayReadsAndExpectedImage(int chunkSize)
        {
            var input = ImageHelpers.LoadFileBytes("ccittfax-encoded.bin");
            var expected = ImageHelpers.LoadFileBytes("ccittfax-decoded.bin");
            using var spanDecoder = new CcittFaxDecoderStream(new MemoryStream(input), 1800, CcittFaxCompressionType.Group4_2D, false);
            using var arrayDecoder = new CcittFaxDecoderStream(new MemoryStream(input), 1800, CcittFaxCompressionType.Group4_2D, false);
            var spanOutput = new byte[expected.Length];
            var arrayOutput = new byte[expected.Length];
            var position = 0;
            while (position < expected.Length)
            {
                var requested = Math.Min(chunkSize, expected.Length - position);
                var read = spanDecoder.Read(spanOutput.AsSpan(position, requested));
                var arrayRead = arrayDecoder.Read(arrayOutput, position, requested);
                Assert.Equal(arrayRead, read);
                Assert.InRange(read, 1, requested);
                position += read;
            }
            Assert.Equal(expected, spanOutput);
            Assert.Equal(arrayOutput, spanOutput);
        }

        [Fact]
        public void SpanAndByteReadsShareDecodedPositionAndPreserveSliceBoundaries()
        {
            // Byte-aligned RLE: one white row, then white zero and black eight.
            using var decoder = new CcittFaxDecoderStream(new MemoryStream(new byte[] { 0x98, 0x35, 0x14 }), 8, CcittFaxCompressionType.ModifiedHuffman, true);
            var output = new byte[] { 0xAA, 0xAA, 0xAA, 0xAA };
            Assert.Equal(1, decoder.Read(output.AsSpan(1, 2)));
            Assert.Equal(new byte[] { 0xAA, 0x00, 0xAA, 0xAA }, output);
            Assert.Equal(0xFF, decoder.ReadByte());
            Assert.Equal(2, decoder.Read(output.AsSpan(1, 2)));
            Assert.Equal(new byte[] { 0xAA, 0x00, 0x00, 0xAA }, output);
        }

        [Fact]
        public void ArrayAndSpanReadsSharePositionWithinARow()
        {
            // White eight and black eight form a single two-byte decoded row.
            using var decoder = new CcittFaxDecoderStream(new MemoryStream(new byte[] { 0x98, 0xA0 }), 16, CcittFaxCompressionType.ModifiedHuffman, false);
            var output = new byte[] { 0xAA, 0xAA, 0xAA, 0xAA };
            Assert.Equal(1, decoder.Read(output, 1, 1));
            Assert.Equal(1, decoder.Read(output.AsSpan(2, 2)));
            Assert.Equal(new byte[] { 0xAA, 0x00, 0xFF, 0xAA }, output);
        }

        [Fact]
        public void EmptySpanReadDoesNotConsumeCompressedInput()
        {
            using var input = new MemoryStream(new byte[] { 0x98 });
            using var decoder = new CcittFaxDecoderStream(input, 8, CcittFaxCompressionType.ModifiedHuffman, false);
            Assert.Equal(0, decoder.Read(Span<byte>.Empty));
            Assert.Equal(0, input.Position);
            Assert.Equal(0, decoder.ReadByte());
        }
#endif

        [Fact]
        public void EmptyArrayReadDoesNotConsumeCompressedInput()
        {
            using var input = new MemoryStream(new byte[] { 0x98 });
            using var decoder = new CcittFaxDecoderStream(input, 8, CcittFaxCompressionType.ModifiedHuffman, false);
            Assert.Equal(0, decoder.Read(Array.Empty<byte>(), 0, 0));
            Assert.Equal(0, input.Position);
            Assert.Equal(0, decoder.ReadByte());
        }
    }
}