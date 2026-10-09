using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UglyToad.PdfPig.Filters;
using UglyToad.PdfPig.Filters.CcittFax;
using UglyToad.PdfPig.Fonts;
using UglyToad.PdfPig.Tests.Images;
using UglyToad.PdfPig.Tokens;

namespace UglyToad.PdfPig.Tests.Filters;
/// <summary>Verifies CCITT pixels, row framing, compact dispatch and strict/lenient recovery.</summary>
/// <remarks>
/// <para>Valid inputs are checked against explicit pixel expectations and the unchanged master
/// decoder. A small test encoder creates 1D or 2D rows from source pixels using its own stored
/// standard codewords; it does not use the production lookup builders. The standard codewords
/// were retained from Apache-2.0 PdfPig master at
/// <see href="https://github.com/UglyToad/PdfPig/blob/bdbc5f47fdbca11542db7ee876426ee601374427/src/UglyToad.PdfPig/Filters/CcittFax/CcittFaxDecoderStream.cs">this pinned source</see>.</para>
/// <para>Malformed-input tests use fixed examples and deterministic random cases. Comparing
/// normal dispatch with the signed entry checks retry and exception behavior within the new
/// decoder; that comparison is not an independent proof of compatibility with master.</para>
/// <para>The master reference is compiled only into this test assembly and stays byte-for-byte unchanged.</para>
/// </remarks>
public class CcittFaxDecoderTests
{
    [Theory]
    [InlineData("00000010000011010011", 64, 29)]
    [InlineData("000000010011001101010000000100110000110111", 4096, 2048)]
    [InlineData("0011010100000011011000000110111", 512, 0)]
    public void LongCodesMatchExpectedPixelsIncludingMakeupAndThirteenBitCodes(string bits, int columns, int whitePixels)
    {
        foreach (var lenient in new[] { false, true })
            foreach (var blackIsOne in new[] { false, true })
            {
                Action<byte[], bool> decoder = (destination, polarity) =>
                    DecodeInto(PackBits(bits), columns, CcittFaxCompressionType.ModifiedHuffman, false, lenient, destination, polarity);
                var expected = new byte[(columns + 7) / 8];
                for (var x = whitePixels; x < columns; x++)
                    expected[x / 8] |= (byte)(1 << (7 - x % 8));
                if (!blackIsOne)
                    for (var i = 0; i < expected.Length; i++)
                        expected[i] = (byte)~expected[i];
                var actual = new byte[expected.Length];
                decoder(actual, blackIsOne);
                Assert.Equal(expected, actual);
            }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Group3AcceptsLongFillBeforeEndOfLine(bool lenient)
    {
        var input = PackBits(new string('0', 4096) + "000000000001" + "10011");
        var output = new byte[]
        {
            0xAA
        };
        DecodeInto(input, 8, CcittFaxCompressionType.Group3_1D, false, lenient, output, true);
        Assert.Equal(new byte[] { 0 }, output);
        var compact = new byte[]
        {
            0xAA
        };
        Assert.True(CcittFaxCompactDecoder.TryDecode(input, compact, 8, 1, CcittFaxCompressionType.Group3_1D, false, true));
        Assert.Equal(output, compact);
    }

    [Theory]
    [InlineData("ModifiedHuffman")]
    [InlineData("Group3_1D")]
    [InlineData("Group3_2D")]
    [InlineData("Group4_2D")]
    public void WholeAndSlicedInputMatchForShortAndMalformedData(string compression)
    {
        var type = (CcittFaxCompressionType)Enum.Parse(typeof(CcittFaxCompressionType), compression);
        var random = new Random(1435);
        foreach (var columns in new[] { 1, 7, 8, 9, 16, 31, 64 })
            foreach (var lenient in new[] { false, true })
                foreach (var aligned in new[] { false, true })
                    for (var sample = 0; sample < 32; sample++)
                    {
                        var input = new byte[random.Next(0, 49)];
                        random.NextBytes(input);
                        foreach (var blackIsOne in new[] { false, true })
                        {
                            Action<byte[], bool> wholeDecoder = (destination, polarity) =>
                                DecodeInto(input, columns, type, aligned, lenient, destination, polarity);
                            var paddedInput = Enumerable.Repeat((byte)0xAA, input.Length + 10).ToArray();
                            input.CopyTo(paddedInput, 5);
                            Action<byte[], bool> directDecoder = (destination, polarity) =>
                                DecodeInto(paddedInput.AsMemory(5, input.Length), columns, type, aligned, lenient, destination, polarity);
                            var expected = new byte[(columns + 7) / 8 * 4];
                            var actual = new byte[expected.Length];
                            var wholeException = Record.Exception(() => wholeDecoder(expected, blackIsOne));
                            var directException = Record.Exception(() => directDecoder(actual, blackIsOne));
                            if (wholeException != null)
                            {
                                Assert.NotNull(directException);
                                Assert.Equal(wholeException.GetType(), directException.GetType());
                                Assert.Equal(wholeException.Message, directException.Message);
                                Assert.Equal(wholeException.InnerException?.GetType(), directException.InnerException?.GetType());
                            }
                            else
                            {
                                Assert.Null(directException);
                                Assert.Equal(expected, actual);
                            }
                        }
                    }
    }

    [Theory]
    [InlineData(false, 0x7F)]
    [InlineData(true, 0x80)]
    public void DirectOutputPreservesPaddingBitsAndFillsTruncatedRows(bool blackIsOne, int firstByte)
    {
        // A zero-length white run followed by one black pixel completes the first row.
        // Its seven padding bits and the unread rows must stay white in the requested polarity.
        Action<byte[], bool> decoder = (destination, polarity) =>
            DecodeInto(new byte[] { 0x35, 0x40 }, 1, CcittFaxCompressionType.ModifiedHuffman, true, false, destination, polarity);
        var output = new byte[]
        {
            0xAA,
            0xAA,
            0xAA
        };
        decoder(output, blackIsOne);
        Assert.Equal(new byte[] { (byte)firstByte, blackIsOne ? (byte)0 : (byte)255, blackIsOne ? (byte)0 : (byte)255 }, output);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ByteAlignmentRetainsPrefetchedNextRow(bool lenient)
    {
        // First row: one white pixel, one black pixel and six white pixels (13 bits, then padding).
        // A lookup for its last code may buffer the second row; byte alignment must discard
        // only the first row's padding, preserving the buffered next-row bits.
        Action<byte[], bool> decoder = (destination, polarity) =>
            DecodeInto(new byte[] { 0x1D, 0x70, 0x98 }, 8, CcittFaxCompressionType.ModifiedHuffman, true, lenient, destination, polarity);
        Assert.Equal(new byte[] { 0x40, 0 }, DecodeBytes(decoder, 2));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OversizedRunRespectsParsingMode(bool lenient)
    {
        Action<byte[], bool> decoder = (destination, polarity) =>
            DecodeInto(new byte[] { 0xA8 }, 8, CcittFaxCompressionType.ModifiedHuffman, false, lenient, destination, polarity);
        if (lenient)
            Assert.Equal(0, DecodeBytes(decoder, 1)[0]);
        else
            Assert.Throws<CorruptCompressedDataException>(() => DecodeBytes(decoder, 1)[0]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UnknownTwoDimensionalCodeRespectsParsingMode(bool lenient)
    {
        Action<byte[], bool> decoder = (destination, polarity) =>
            DecodeInto(new byte[] { 0x00, 0x80 }, 8, CcittFaxCompressionType.Group4_2D, false, lenient, destination, polarity);
        if (lenient)
            Assert.Equal(0, DecodeBytes(decoder, 1)[0]);
        else
            Assert.Throws<CorruptCompressedDataException>(() => DecodeBytes(decoder, 1)[0]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NegativeVerticalPositionRespectsParsingMode(bool lenient)
    {
        Action<byte[], bool> decoder = (destination, polarity) =>
            DecodeInto(new byte[] { 0x05 }, 1, CcittFaxCompressionType.Group4_2D, false, lenient, destination, polarity);
        if (lenient)
            Assert.Equal(0x80, DecodeBytes(decoder, 1)[0]);
        else
            Assert.Throws<CorruptCompressedDataException>(() => DecodeBytes(decoder, 1)[0]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Group4EndOfBlockRetainsZeroPadding(bool lenient)
    {
        // Two consecutive end-of-line (EOL) codes form the Group 4 end-of-facsimile-block
        // (EOFB) marker. Rows requested after this marker remain white.
        Action<byte[], bool> decoder = (destination, polarity) =>
            DecodeInto(new byte[] { 0x00, 0x10, 0x01 }, 8, CcittFaxCompressionType.Group4_2D, false, lenient, destination, polarity);
        Assert.Equal(new byte[] { 0, 0 }, DecodeBytes(decoder, 2));
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

        Action<byte[], bool> decoder = (destination, polarity) =>
            DecodeInto(input, 8, CcittFaxCompressionType.ModifiedHuffman, false, true, destination, polarity);
        var exception = Assert.Throws<CorruptCompressedDataException>(() => DecodeBytes(decoder, 1)[0]);
        Assert.IsType<OverflowException>(exception.InnerException);
        AssertMatchesCompatibilityPath(input, 8, 1, CcittFaxCompressionType.ModifiedHuffman, false, true, true);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ActualEndOfInputStillPadsWithZeros(bool lenient)
    {
        Action<byte[], bool> decoder = (destination, polarity) =>
            DecodeInto(new byte[] { 0x00 }, 8, CcittFaxCompressionType.ModifiedHuffman, false, lenient, destination, polarity);
        var output = new byte[]
        {
            0xAA,
            0xAA
        };
        decoder(output, true);
        Assert.Equal(new byte[] { 0, 0 }, output);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void InvalidHuffmanCodeRespectsParsingMode(bool lenient)
    {
        Action<byte[], bool> decoder = (destination, polarity) =>
            DecodeInto(new byte[] { 0x00, 0x80 }, 8, CcittFaxCompressionType.ModifiedHuffman, false, lenient, destination, polarity);
        if (lenient)
        {
            Assert.Equal(0, DecodeBytes(decoder, 1)[0]);
        }
        else
        {
            var exception = Assert.Throws<CorruptCompressedDataException>(() => DecodeBytes(decoder, 1)[0]);
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
        var input = new byte[]
        {
            0x00,
            0x80
        };
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
        // Repeated zero-length white and black runs never advance the pixel position.
        // Each color change is recorded, exhausting the three-entry array for this one-pixel row.
        var bits = string.Concat(Enumerable.Repeat("001101010000110111", 4));
        var input = Enumerable.Range(0, (bits.Length + 7) / 8).Select(i => Convert.ToByte(bits.Substring(i * 8, Math.Min(8, bits.Length - i * 8)).PadRight(8, '0'), 2)).ToArray();
        Action<byte[], bool> decoder = (destination, polarity) =>
            DecodeInto(input, 1, CcittFaxCompressionType.ModifiedHuffman, false, lenient, destination, polarity);
        var exception = Assert.Throws<CorruptCompressedDataException>(() => decoder(new byte[1], true));
        Assert.IsType<IndexOutOfRangeException>(exception.InnerException);
    }

    private static byte[] DecodeBytes(Action<byte[], bool> decoder, int count)
    {
        var output = new byte[count];
        decoder(output, true);
        return output;
    }

    private static void DecodeInto(ReadOnlyMemory<byte> input, int width, CcittFaxCompressionType mode, bool aligned, bool lenient, byte[] output, bool polarity)
    {
        int stride = (width + 7) / 8;
        Assert.Equal(0, output.Length % stride);
        CcittFaxCompactDecoder.Decode(input.Span, output, width, output.Length / stride, mode, aligned, polarity, lenient);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ActualFixtureUsesCompactPathAndMatchesEveryByte(bool polarity)
    {
        var input = ImageHelpers.LoadFileBytes("ccittfax-encoded.bin");
        var expected = ImageHelpers.LoadFileBytes("ccittfax-decoded.bin");
        if (!polarity)
            for (int i = 0; i < expected.Length; i++)
                expected[i] = (byte)~expected[i];
        var output = Enumerable.Repeat((byte)0xAA, expected.Length).ToArray();
        CcittFaxCompactDecoder.Decode(input, output, 1800, 3113, CcittFaxCompressionType.Group4_2D, false, polarity, useLenientParsing: false);
        Assert.Equal(expected, output);
        output.AsSpan().Fill(0xAA);
        Assert.True(CcittFaxCompactDecoder.TryDecode(input, output, 1800, 3113, CcittFaxCompressionType.Group4_2D, false, polarity));
        Assert.Equal(expected, output);
        var filter = new CcittFaxDecodeFilter().Decode(input,
                CreateImageDictionary(new DecodeOptions(1800, 3113, CcittFaxCompressionType.Group4_2D, false, polarity)),
                DefaultFilterProvider.Instance, 0);
        Assert.Equal(expected, filter.ToArray());
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void IndependentHuffmanPixelsCoverPaddingAlignmentAndFill(bool lenient, bool polarity)
    {
        string[] white =
        {
            "00110101",
            "000111",
            "0111",
            "1000",
            "1011",
            "1100",
            "1110",
            "1111",
            "10011"
        };
        string[] black =
        {
            "0000110111",
            "010",
            "11",
            "10",
            "011",
            "0011",
            "0010",
            "00011",
            "000101"
        };
        foreach (bool aligned in new[] { true, false })
            foreach (var mode in new[] { CcittFaxCompressionType.ModifiedHuffman, CcittFaxCompressionType.Group3_1D })
                for (int w = 0; w <= 8; w++)
                    for (int b = 0; b <= 8; b++)
                    {
                        if (w + b == 0)
                            continue;
                        int width = w + b, stride = (width + 7) / 8;
                        string row = white[w] + (b > 0 ? black[b] : "");
                        if (mode == CcittFaxCompressionType.Group3_1D)
                            row = "0000" + "000000000001" + row;
                        if (aligned)
                            row = row.PadRight((row.Length + 7) / 8 * 8, '0');
                        byte[] input = PackBits(row + row);
                        var expected = new byte[stride * 2];
                        for (int y = 0; y < 2; y++)
                            for (int x = w; x < width; x++)
                                expected[y * stride + x / 8] |= (byte)(128 >> (x % 8));
                        if (!polarity)
                            for (int i = 0; i < expected.Length; i++)
                                expected[i] = (byte)~expected[i];
                        var output = Enumerable.Repeat((byte)0xAA, expected.Length).ToArray();
                        CcittFaxCompactDecoder.Decode(input, output, width, 2, mode, aligned, polarity, lenient);
                        Assert.Equal(expected, output);
                        output.AsSpan().Fill(0xAA);
                        Assert.True(CcittFaxCompactDecoder.TryDecode(input, output, width, 2, mode, aligned, polarity));
                        Assert.Equal(expected, output);
                        Assert.Equal(expected, new CcittFaxDecodeFilter(lenient).Decode(input,
                                CreateImageDictionary(new DecodeOptions(width, 2, mode, aligned, polarity)),
                                DefaultFilterProvider.Instance, 0).ToArray());
                    }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void IndependentTwoDimensionalRowsCoverGroup4AndMixedGroup3(bool polarity)
    {
        const string eol = "000000000001";
        var expected = polarity ? new byte[]
        {
            31,
            31,
            0,
            0
        }

        : new byte[]
        {
            224,
            224,
            255,
            255
        };
        foreach (var mode in new[] { CcittFaxCompressionType.Group4_2D, CcittFaxCompressionType.Group3_2D })
        {
            string bits = mode == CcittFaxCompressionType.Group4_2D
                ? "00110000011" + "11" + "0001" + "1"
                : eol + "1" + "10000011" + eol + "0" + "11" + eol + "1" + "10011" + eol + "0" + "1";
            var input = PackBits(bits);
            var output = new byte[4];
            Assert.True(CcittFaxCompactDecoder.TryDecode(input, output, 8, 4, mode, false, polarity));
            Assert.Equal(expected, output);
            Assert.Equal(expected, new CcittFaxDecodeFilter().Decode(input,
                    CreateImageDictionary(new DecodeOptions(8, 4, mode, false, polarity)),
                    DefaultFilterProvider.Instance, 0).ToArray());
        }
    }

    private static void AssertMatchesCompatibilityPath(byte[] input, int width, int rows, CcittFaxCompressionType mode, bool aligned, bool polarity, bool lenient)
    {
        var expected = new byte[(width + 7) / 8 * rows];
        var actual = Enumerable.Repeat((byte)0xAA, expected.Length).ToArray();
        var compatibilityError = Record.Exception(() => CcittFaxCompactDecoder.DecodeCompatibility(input, expected, width, rows, mode, aligned, polarity, lenient));
        var newError = Record.Exception(() => CcittFaxCompactDecoder.Decode(input, actual, width, rows, mode, aligned, polarity, lenient));
        string detail = $"width={width},rows={rows},mode={mode},aligned={aligned},polarity={polarity},lenient={lenient},inputLength={input.Length},prefix={Convert.ToBase64String(input.Take(96).ToArray())}";
        Assert.True(compatibilityError?.GetType() == newError?.GetType(), detail + $",compatibility={compatibilityError},new={newError}");
        if (compatibilityError == null)
            Assert.True(expected.AsSpan().SequenceEqual(actual), detail);
        else
        {
            Assert.Equal(compatibilityError.InnerException?.GetType(), newError!.InnerException?.GetType());
            Assert.Equal(compatibilityError.Message, newError.Message);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MalformedAndTruncatedRowsMatchCompatibilityPath(bool lenient)
    {
        var random = new Random(7716080);
        foreach (var mode in new[]
        {
            CcittFaxCompressionType.ModifiedHuffman,
            CcittFaxCompressionType.Group3_1D,
            CcittFaxCompressionType.Group3_2D,
            CcittFaxCompressionType.Group4_2D
        })
            foreach (int width in new[] { 1, 7, 8, 9, 16, 31, 64, 257 })
                for (int sample = 0; sample < 512; sample++)
                {
                    var input = new byte[random.Next(0, 97)];
                    random.NextBytes(input);
                    AssertMatchesCompatibilityPath(input, width, 8, mode, sample % 2 == 0, sample % 3 == 0, lenient);
                }
    }

    private static byte[] PackBits(string bits)
    {
        var data = new byte[(bits.Length + 7) / 8];
        for (int i = 0; i < bits.Length; i++)
            if (bits[i] == '1')
                data[i / 8] |= (byte)(128 >> (i & 7));
        return data;
    }

    [Theory]
    [InlineData(65535)]
    [InlineData(65536)]
    [InlineData(100003)]
    public void WideRowsAndWidthBoundaryMatchIndependentPixels(int width)
    {
        const string eol = "000000000001";
        foreach (var mode in new[]
        {
            CcittFaxCompressionType.ModifiedHuffman,
            CcittFaxCompressionType.Group3_1D,
            CcittFaxCompressionType.Group3_2D,
            CcittFaxCompressionType.Group4_2D
        })
            foreach (bool polarity in new[] { false, true })
                foreach (bool aligned in new[] { false, true })
                {
                    var rowCodes = new StringBuilder();
                    EncodeRun(rowCodes, 0, true);
                    EncodeRun(rowCodes, width, false);
                    string row = rowCodes.ToString();
                    row = mode == CcittFaxCompressionType.Group4_2D
                        ? "001" + row
                        : mode == CcittFaxCompressionType.ModifiedHuffman ? row : eol + (mode == CcittFaxCompressionType.Group3_2D ? "1" : "") + row;
                    if (aligned)
                        row = row.PadRight((row.Length + 7) / 8 * 8, '0');
                    var input = PackBits(row + row);
                    AssertMatchesCompatibilityPath(input, width, 2, mode, aligned, polarity, false);
                    var actual = new byte[(width + 7) / 8 * 2];
                    CcittFaxCompactDecoder.Decode(input, actual, width, 2, mode, aligned, polarity, false);
                    int stride = (width + 7) / 8;
                    for (int y = 0; y < 2; y++)
                        for (int x = 0; x < width; x++)
                            Assert.Equal(polarity, (actual[y * stride + x / 8] & (128 >> (x & 7))) != 0);
                }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FixtureAndEveryTruncatedSuffixMatchCompatibilityPath(bool lenient)
    {
        var input = ImageHelpers.LoadFileBytes("ccittfax-encoded.bin");
        var expected = ImageHelpers.LoadFileBytes("ccittfax-decoded.bin");
        var actual = new byte[expected.Length];
        CcittFaxCompactDecoder.Decode(input, actual, 1800, 3113, CcittFaxCompressionType.Group4_2D, false, true, lenient);
        Assert.Equal(expected, actual);
        CcittFaxCompactDecoder.DecodeCompatibility(input, actual, 1800, 3113, CcittFaxCompressionType.Group4_2D, false, true, lenient);
        Assert.Equal(expected, actual);
        for (int length = 0; length <= Math.Min(input.Length, 96); length++)
            AssertMatchesCompatibilityPath(input.Take(length).ToArray(), 1800, 8, CcittFaxCompressionType.Group4_2D, false, length % 2 == 0, lenient);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void WideMalformedRowsMatchCompatibilityPath(bool lenient)
    {
        var random = new Random(1435);
        foreach (var mode in new[]
        {
            CcittFaxCompressionType.ModifiedHuffman,
            CcittFaxCompressionType.Group3_1D,
            CcittFaxCompressionType.Group3_2D,
            CcittFaxCompressionType.Group4_2D
        })
            for (int sample = 0; sample < 32; sample++)
            {
                var input = new byte[random.Next(0, 97)];
                random.NextBytes(input);
                AssertMatchesCompatibilityPath(input, 65536, 3, mode, sample % 2 == 0, sample % 3 == 0, lenient);
            }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LateRowFailuresKeepEarlierRowsAndReferenceTransitions(bool lenient)
    {
        foreach (bool polarity in new[] { false, true })
            foreach (string suffix in new[] { "00000011", "0000101", "000000000001000000000001", "00100110101000101" })
            {
                var input = PackBits(new string('1', 17) + suffix);
                AssertMatchesCompatibilityPath(input, 1, 24, CcittFaxCompressionType.Group4_2D, false, polarity, lenient);
            }

        var fixture = ImageHelpers.LoadFileBytes("ccittfax-encoded.bin").Take(96).ToArray();
        for (int bit = 0; bit < fixture.Length * 8; bit++)
        {
            var input = (byte[])fixture.Clone();
            input[bit / 8] ^= (byte)(128 >> (bit & 7));
            AssertMatchesCompatibilityPath(input, 1800, 32, CcittFaxCompressionType.Group4_2D, false, bit % 2 == 0, lenient);
        }
    }

    private static DictionaryToken CreateImageDictionary(DecodeOptions options)
    {
        var parameters = new DictionaryToken(new Dictionary<NameToken, IToken>
        {
            [NameToken.Columns] = new NumericToken(options.Width),
            [NameToken.Rows] = new NumericToken(options.Height),
            [NameToken.K] = new NumericToken(options.K),
            [NameToken.EndOfLine] = options.EndOfLine ? BooleanToken.True : BooleanToken.False,
            [NameToken.EncodedByteAlign] = options.Aligned ? BooleanToken.True : BooleanToken.False,
            [NameToken.BlackIs1] = options.BlackIsOne ? BooleanToken.True : BooleanToken.False
        });
        return new DictionaryToken(new Dictionary<NameToken, IToken>
        {
            [NameToken.Filter] = NameToken.CcittfaxDecode,
            [NameToken.DecodeParms] = parameters
        });
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void IntegratedFilterMatchesIndependentPixelsAndMasterBytes(bool lenient)
    {
        foreach (var vector in GenerateTestImages())
        {
            var options = vector.Options;
            var dictionary = CreateImageDictionary(options);
            var actual = new CcittFaxDecodeFilter(lenient).Decode(vector.Input, dictionary, DefaultFilterProvider.Instance, 0);
            var master = DecodeMaster(vector.Input, options);
            Assert.Equal(master, actual.ToArray());
            AssertPixelsEqual(vector.Expected, actual.ToArray(), options.Width, options.Height, vector.Name);
            var compact = new byte[actual.Length];
            var mode = options.Mode;
            Assert.True(CcittFaxCompactDecoder.TryDecode(vector.Input, compact, options.Width, options.Height, mode, options.Aligned, options.BlackIsOne));
            Assert.Equal(actual.ToArray(), compact);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void MalformedFilterMatchesCompatibilityPathAndFailureContract(bool lenient)
    {
        // Exercise empty and nonempty input with different EOL hints. Reset the seed per
        // set to keep failures reproducible.
        foreach (bool includeEmptyInput in new[] { false, true })
        {
            var random = new Random(1435);
            foreach (int mode in includeEmptyInput ? new[] { -1, 0, 1, 2 } : new[] { 1, 0, 2, -1 })
            {
                foreach (int width in new[] { 1, 7, 8, 9, 31, 64 })
                {
                    for (int sample = 0; sample < 128; sample++)
                    {
                        var options = new DecodeOptions(width, 8,
                            k: mode == 1 ? 0 : mode,
                            rle: mode == 1,
                            aligned: sample % 2 == 0,
                            blackIsOne: sample % 3 == 0,
                            endOfLine: includeEmptyInput ? mode == 0 || mode == 2 : mode != 1);
                        var input = new byte[random.Next(includeEmptyInput ? 0 : 1, 65)];
                        random.NextBytes(input);
                        AssertFilterMatchesCompatibility(input, options, lenient, sample);
                    }
                }
            }
        }
    }

    private static void AssertFilterMatchesCompatibility(byte[] input, DecodeOptions options, bool lenient, int sample)
    {
        byte[]? compatibilityBytes = null;
        byte[]? actualBytes = null;
        var dictionary = CreateImageDictionary(options);
        var compatibilityError = Record.Exception(() => compatibilityBytes = DecodeCompatibilityFilter(input, options, lenient));
        var actualError = Record.Exception(() => actualBytes = new CcittFaxDecodeFilter(lenient).Decode(input, dictionary, DefaultFilterProvider.Instance, 0).ToArray());
        string caseInfo = $"mode={options.Mode},width={options.Width},sample={sample},lenient={lenient},input={Convert.ToBase64String(input)}";
        Assert.True(compatibilityError?.GetType() == actualError?.GetType(),
            caseInfo + $",compatibility={compatibilityError?.GetType().Name}:{compatibilityError?.Message},actual={actualError?.GetType().Name}");
        Assert.True(compatibilityBytes == null ? actualBytes == null : actualBytes != null && compatibilityBytes.AsSpan().SequenceEqual(actualBytes), caseInfo);
    }

    [Theory]
    [InlineData(65535)]
    [InlineData(65536)]
    public void CompactWidthBoundaryPreservesOutput(int width)
    {
        foreach (bool polarity in new[] { true, false })
        {
            var options = new DecodeOptions(width, 2, blackIsOne: polarity);
            var dictionary = CreateImageDictionary(options);
            byte[] input =
            {
                0xC0
            };
            Assert.Equal(DecodeMaster(input, options), new CcittFaxDecodeFilter().Decode(input, dictionary, DefaultFilterProvider.Instance, 0).ToArray());
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void FillAndIncompleteRtcPreserveIntegratedAcceptance(bool lenient)
    {
        const string eol = "000000000001";
        byte[] input = PackBits("0000" + eol + "10011" + "0000000" + eol + "10011" + eol + eol + eol);
        var options = new DecodeOptions(8, 2, 0, endOfLine: true);
        var dictionary = CreateImageDictionary(options);
        var actual = new CcittFaxDecodeFilter(lenient).Decode(input, dictionary, DefaultFilterProvider.Instance, 0);
        Assert.Equal(new byte[2], actual.ToArray());
        Assert.Equal(DecodeMaster(input, options), actual.ToArray());
    }

    // Empty input follows the filter policy rather than the signed decoder's white-row padding.
    private static byte[] DecodeCompatibilityFilter(byte[] input, DecodeOptions options, bool lenient)
    {
        if (input.Length == 0)
        {
            if (lenient)
                return Array.Empty<byte>();
            throw new CorruptCompressedDataException("Empty CCITT compressed data.");
        }

        var output = new byte[(options.Width + 7) / 8 * options.Height];
        CcittFaxCompactDecoder.DecodeCompatibility(input, output, options.Width, options.Height, options.Mode, options.Aligned, options.BlackIsOne, lenient);
        return output;
    }

    private static void AssertPixelsEqual(byte[] expected, byte[] actual, int width, int rows, string context)
    {
        int stride = (width + 7) / 8;
        Assert.Equal(stride * rows, expected.Length);
        Assert.Equal(expected.Length, actual.Length);
        int lastByteMask = (width & 7) == 0 ? 255 : 255 << (8 - (width & 7));
        for (int y = 0; y < rows; y++)
            for (int x = 0; x < stride; x++)
            {
                int mask = x == stride - 1 ? lastByteMask : 255;
                int index = y * stride + x;
                Assert.True((expected[index] & mask) == (actual[index] & mask), context + $", row={y}, byte={x}");
            }
    }

    /// <summary>Groups row format, dimensions, alignment and polarity for a generated CCITT test case.</summary>
    /// <remarks>Mode is the resolved decoder family; K and EndOfLine retain the corresponding PDF
    /// parameters so the same case can exercise both the filter and decoder directly.</remarks>
    private readonly struct DecodeOptions
    {
        internal int Width { get; }
        internal int Height { get; }
        internal int K { get; }
        internal bool Rle { get; }
        internal bool Aligned { get; }
        internal bool BlackIsOne { get; }
        internal bool EndOfLine { get; }
        internal bool EndOfBlock { get; }
        internal CcittFaxCompressionType Mode =>
            K < 0 ? CcittFaxCompressionType.Group4_2D
            : K > 0 ? CcittFaxCompressionType.Group3_2D
            : Rle ? CcittFaxCompressionType.ModifiedHuffman
            : CcittFaxCompressionType.Group3_1D;

        internal DecodeOptions(int width, int height, CcittFaxCompressionType mode, bool aligned, bool blackIsOne)
            : this(width, height,
            k: mode == CcittFaxCompressionType.Group4_2D ? -1 : mode == CcittFaxCompressionType.Group3_2D ? 2 : 0,
                rle: mode == CcittFaxCompressionType.ModifiedHuffman,
                aligned: aligned,
                blackIsOne: blackIsOne,
                endOfLine: mode != CcittFaxCompressionType.ModifiedHuffman)
        {
        }

        internal DecodeOptions(int width, int height, int k = -1, bool rle = false, bool aligned = false, bool blackIsOne = true, bool endOfLine = false, bool endOfBlock = true)
        {
            Width = width;
            Height = height;
            K = k;
            Rle = rle;
            Aligned = aligned;
            BlackIsOne = blackIsOne;
            EndOfLine = endOfLine;
            EndOfBlock = endOfBlock;
        }
    }

    /// <summary>Holds encoded test input, independently packed expected pixels and its decode parameters.</summary>
    /// <remarks>Expected bytes are built from the source pixels, not from a decoder result.</remarks>
    private sealed class TestImage
    {
        internal string Name { get; }
        internal byte[] Input { get; }
        internal byte[] Expected { get; }
        internal DecodeOptions Options { get; }

        internal TestImage(string name, byte[] input, byte[] expected, DecodeOptions options)
        {
            Name = name;
            Input = input;
            Expected = expected;
            Options = options;
        }
    }

    private static TestImage EncodeTestImage(byte[][] pixels, DecodeOptions options, string name)
    {
        var bits = new StringBuilder();
        byte[] previous = new byte[options.Width];
        for (int y = 0; y < pixels.Length; y++)
        {
            if (options.Aligned)
                while ((bits.Length & 7) != 0)
                    bits.Append('0');
            bool oneD = options.Rle || options.K == 0 || options.K > 0 && y % 2 == 0;
            if (!options.Rle && options.K >= 0)
                bits.Append("000000000001");
            if (options.K > 0)
                bits.Append(oneD ? '1' : '0');
            if (oneD)
                Encode1D(bits, pixels[y]);
            else
                Encode2D(bits, pixels[y], previous);
            previous = pixels[y];
        }

        if (options.K < 0 && options.EndOfBlock)
        {
            if (options.Aligned)
                while ((bits.Length & 7) != 0)
                    bits.Append('0');
            bits.Append("000000000001000000000001");
        }

        if (!options.Rle && options.K == 0)
            for (int i = 0; i < 6; i++)
                bits.Append("000000000001");
        int stride = (options.Width + 7) / 8;
        var expected = new byte[stride * pixels.Length];
        for (int y = 0; y < pixels.Length; y++)
            for (int x = 0; x < options.Width; x++)
                if ((pixels[y][x] == 1) == options.BlackIsOne)
                    expected[y * stride + x / 8] |= (byte)(128 >> (x % 8));
        return new(name, PackBits(bits.ToString()), expected, options);
    }

    private static IEnumerable<TestImage> GenerateTestImages()
    {
        var random = new Random(409);
        foreach (int k in new[] { -1, 0, 2 })
            foreach (int width in new[] { 1, 7, 8, 9, 31, 64, 127, 512, 1800, 4096 })
                foreach (bool polarity in new[] { true, false })
                    for (int sample = 0; sample < 12; sample++)
                    {
                        var pixels = new byte[8][];
                        for (int y = 0; y < pixels.Length; y++)
                        {
                            pixels[y] = new byte[width];
                            for (int x = 0; x < width; x++)
                                pixels[y][x] = sample == 0 ? (byte)0 : sample == 1 ? (byte)1 : sample == 2 ? (byte)((x + y) % 2) : sample == 3 && y > 0 ? pixels[y - 1][Math.Max(0, x - 1)] : (byte)(random.Next(8) == 0 ? 1 : 0);
                        }

                        yield return EncodeTestImage(pixels, new(width, 8, k, blackIsOne: polarity, endOfLine: k >= 0), $"k{k}-w{width}-p{polarity}-s{sample}");
                        if (k == 0)
                            yield return EncodeTestImage(pixels, new(width, 8, 0, rle: true, blackIsOne: polarity), $"rle-w{width}-p{polarity}-s{sample}");
                        if (k < 0)
                            yield return EncodeTestImage(pixels, new(width, 8, -1, aligned: true, blackIsOne: polarity), $"g4aligned-w{width}-p{polarity}-s{sample}");
                    }
    }

    // Use the resolved row format; header detection is tested separately.
    private static byte[] DecodeMaster(byte[] input, DecodeOptions options)
    {
        using var memory = new MemoryStream(input, writable: false);
        using var decoder = new CcittFaxDecoderStream(memory, options.Width, options.Mode, options.Aligned);
        var output = new byte[(options.Width + 7) / 8 * options.Height];
        var offset = 0;
        while (offset < output.Length)
        {
            // Master overrides array Read, but inherits Span Read from StreamWrapper. That
            // inherited method reads compressed input, so comparisons must use the array overload.
            var count = decoder.Read(output, offset, output.Length - offset);
            Assert.True(count > 0, "Master must make progress while reading a positive-width row.");
            offset += count;
        }

        if (!options.BlackIsOne)
            for (var i = 0; i < output.Length; i++)
                output[i] = (byte)~output[i];
        return output;
    }

    [Fact]
    public void StrictInvalidModeRejectionDiffersIntentionallyFromMaster()
    {
        // Master suppresses an invalid 2D mode as image termination. The replacement reports
        // corruption in strict mode; lenient decoding preserves white output for this input.
        var input = new byte[8];
        var options = new DecodeOptions(8, 1);
        Assert.Equal(new byte[1], DecodeMaster(input, options));
        Assert.Throws<CorruptCompressedDataException>(() => CcittFaxCompactDecoder.Decode(input, new byte[1], 8, 1, CcittFaxCompressionType.Group4_2D, false, true, false));
        var lenient = new byte[1];
        CcittFaxCompactDecoder.Decode(input, lenient, 8, 1, CcittFaxCompressionType.Group4_2D, false, true, true);
        Assert.Equal(new byte[1], lenient);
    }

    // Standard T.4 run codewords from the Apache-2.0 PdfPig master source cited on this class.
    private static readonly (int Bits, int Length, int Run)[] WhiteRunCodes =
    {
        (0x7, 4, 2),
        (0x8, 4, 3),
        (0xB, 4, 4),
        (0xC, 4, 5),
        (0xE, 4, 6),
        (0xF, 4, 7),
        (0x12, 5, 128),
        (0x13, 5, 8),
        (0x14, 5, 9),
        (0x1B, 5, 64),
        (0x7, 5, 10),
        (0x8, 5, 11),
        (0x17, 6, 192),
        (0x18, 6, 1664),
        (0x2A, 6, 16),
        (0x2B, 6, 17),
        (0x3, 6, 13),
        (0x34, 6, 14),
        (0x35, 6, 15),
        (0x7, 6, 1),
        (0x8, 6, 12),
        (0x13, 7, 26),
        (0x17, 7, 21),
        (0x18, 7, 28),
        (0x24, 7, 27),
        (0x27, 7, 18),
        (0x28, 7, 24),
        (0x2B, 7, 25),
        (0x3, 7, 22),
        (0x37, 7, 256),
        (0x4, 7, 23),
        (0x8, 7, 20),
        (0xC, 7, 19),
        (0x12, 8, 33),
        (0x13, 8, 34),
        (0x14, 8, 35),
        (0x15, 8, 36),
        (0x16, 8, 37),
        (0x17, 8, 38),
        (0x1A, 8, 31),
        (0x1B, 8, 32),
        (0x2, 8, 29),
        (0x24, 8, 53),
        (0x25, 8, 54),
        (0x28, 8, 39),
        (0x29, 8, 40),
        (0x2A, 8, 41),
        (0x2B, 8, 42),
        (0x2C, 8, 43),
        (0x2D, 8, 44),
        (0x3, 8, 30),
        (0x32, 8, 61),
        (0x33, 8, 62),
        (0x34, 8, 63),
        (0x35, 8, 0),
        (0x36, 8, 320),
        (0x37, 8, 384),
        (0x4, 8, 45),
        (0x4A, 8, 59),
        (0x4B, 8, 60),
        (0x5, 8, 46),
        (0x52, 8, 49),
        (0x53, 8, 50),
        (0x54, 8, 51),
        (0x55, 8, 52),
        (0x58, 8, 55),
        (0x59, 8, 56),
        (0x5A, 8, 57),
        (0x5B, 8, 58),
        (0x64, 8, 448),
        (0x65, 8, 512),
        (0x67, 8, 640),
        (0x68, 8, 576),
        (0xA, 8, 47),
        (0xB, 8, 48),
        (0x98, 9, 1472),
        (0x99, 9, 1536),
        (0x9A, 9, 1600),
        (0x9B, 9, 1728),
        (0xCC, 9, 704),
        (0xCD, 9, 768),
        (0xD2, 9, 832),
        (0xD3, 9, 896),
        (0xD4, 9, 960),
        (0xD5, 9, 1024),
        (0xD6, 9, 1088),
        (0xD7, 9, 1152),
        (0xD8, 9, 1216),
        (0xD9, 9, 1280),
        (0xDA, 9, 1344),
        (0xDB, 9, 1408),
        (0x8, 11, 1792),
        (0xC, 11, 1856),
        (0xD, 11, 1920),
        (0x12, 12, 1984),
        (0x13, 12, 2048),
        (0x14, 12, 2112),
        (0x15, 12, 2176),
        (0x16, 12, 2240),
        (0x17, 12, 2304),
        (0x1C, 12, 2368),
        (0x1D, 12, 2432),
        (0x1E, 12, 2496),
        (0x1F, 12, 2560),
    };
    private static readonly (int Bits, int Length, int Run)[] BlackRunCodes =
    {
        (0x2, 2, 3),
        (0x3, 2, 2),
        (0x2, 3, 1),
        (0x3, 3, 4),
        (0x2, 4, 6),
        (0x3, 4, 5),
        (0x3, 5, 7),
        (0x4, 6, 9),
        (0x5, 6, 8),
        (0x4, 7, 10),
        (0x5, 7, 11),
        (0x7, 7, 12),
        (0x4, 8, 13),
        (0x7, 8, 14),
        (0x18, 9, 15),
        (0x17, 10, 16),
        (0x18, 10, 17),
        (0x37, 10, 0),
        (0x8, 10, 18),
        (0xF, 10, 64),
        (0x17, 11, 24),
        (0x18, 11, 25),
        (0x28, 11, 23),
        (0x37, 11, 22),
        (0x67, 11, 19),
        (0x68, 11, 20),
        (0x6C, 11, 21),
        (0x8, 11, 1792),
        (0xC, 11, 1856),
        (0xD, 11, 1920),
        (0x12, 12, 1984),
        (0x13, 12, 2048),
        (0x14, 12, 2112),
        (0x15, 12, 2176),
        (0x16, 12, 2240),
        (0x17, 12, 2304),
        (0x1C, 12, 2368),
        (0x1D, 12, 2432),
        (0x1E, 12, 2496),
        (0x1F, 12, 2560),
        (0x24, 12, 52),
        (0x27, 12, 55),
        (0x28, 12, 56),
        (0x2B, 12, 59),
        (0x2C, 12, 60),
        (0x33, 12, 320),
        (0x34, 12, 384),
        (0x35, 12, 448),
        (0x37, 12, 53),
        (0x38, 12, 54),
        (0x52, 12, 50),
        (0x53, 12, 51),
        (0x54, 12, 44),
        (0x55, 12, 45),
        (0x56, 12, 46),
        (0x57, 12, 47),
        (0x58, 12, 57),
        (0x59, 12, 58),
        (0x5A, 12, 61),
        (0x5B, 12, 256),
        (0x64, 12, 48),
        (0x65, 12, 49),
        (0x66, 12, 62),
        (0x67, 12, 63),
        (0x68, 12, 30),
        (0x69, 12, 31),
        (0x6A, 12, 32),
        (0x6B, 12, 33),
        (0x6C, 12, 40),
        (0x6D, 12, 41),
        (0xC8, 12, 128),
        (0xC9, 12, 192),
        (0xCA, 12, 26),
        (0xCB, 12, 27),
        (0xCC, 12, 28),
        (0xCD, 12, 29),
        (0xD2, 12, 34),
        (0xD3, 12, 35),
        (0xD4, 12, 36),
        (0xD5, 12, 37),
        (0xD6, 12, 38),
        (0xD7, 12, 39),
        (0xDA, 12, 42),
        (0xDB, 12, 43),
        (0x4A, 13, 640),
        (0x4B, 13, 704),
        (0x4C, 13, 768),
        (0x4D, 13, 832),
        (0x52, 13, 1280),
        (0x53, 13, 1344),
        (0x54, 13, 1408),
        (0x55, 13, 1472),
        (0x5A, 13, 1536),
        (0x5B, 13, 1600),
        (0x64, 13, 1664),
        (0x65, 13, 1728),
        (0x6C, 13, 512),
        (0x6D, 13, 576),
        (0x72, 13, 896),
        (0x73, 13, 960),
        (0x74, 13, 1024),
        (0x75, 13, 1088),
        (0x76, 13, 1152),
        (0x77, 13, 1216),
    };
    private static void EncodeRun(StringBuilder bits, int run, bool white)
    {
        var codes = white ? WhiteRunCodes : BlackRunCodes;
        while (run >= 64)
        {
            var code = codes.Where(c => c.Run >= 64 && c.Run <= run).OrderByDescending(c => c.Run).First();
            AppendCodeBits(bits, code.Bits, code.Length);
            run -= code.Run;
        }

        var terminating = codes.Single(c => c.Run == run);
        AppendCodeBits(bits, terminating.Bits, terminating.Length);
    }

    private static void AppendCodeBits(StringBuilder bits, int code, int length) => bits.Append(Convert.ToString(code, 2).PadLeft(length, '0'));
    private static int[] FindPixelTransitions(byte[] pixels)
    {
        var result = new List<int>();
        byte previous = 0;
        for (int x = 0; x < pixels.Length; x++)
            if (pixels[x] != previous)
            {
                result.Add(x);
                previous = pixels[x];
            }

        result.Add(pixels.Length);
        return result.ToArray();
    }

    private static void Encode1D(StringBuilder bits, byte[] pixels)
    {
        bool white = true;
        int x = 0;
        foreach (int end in FindPixelTransitions(pixels))
        {
            EncodeRun(bits, end - x, white);
            x = end;
            white = !white;
        }
    }

    private static void Encode2D(StringBuilder bits, byte[] pixels, byte[] previous)
    {
        int width = pixels.Length, x = 0;
        var current = FindPixelTransitions(pixels);
        var reference = FindPixelTransitions(previous);
        bool white = true;
        while (x < width)
        {
            int ai = white ? 0 : 1;
            while (ai < current.Length && (x == 0 ? current[ai] < x : current[ai] <= x))
                ai += 2;
            int a1 = ai < current.Length ? current[ai] : width, a2 = ai + 1 < current.Length ? current[ai + 1] : width;
            int bi = white ? 0 : 1;
            while (bi < reference.Length && x != 0 && reference[bi] <= x)
                bi += 2;
            int b1 = bi < reference.Length ? reference[bi] : width, b2 = bi + 1 < reference.Length ? reference[bi + 1] : width;
            if (b2 < a1)
            {
                bits.Append("0001");
                x = b2;
            }
            else if (Math.Abs(a1 - b1) <= 3)
            {
                int delta = a1 - b1;
                bits.Append(delta switch
                {
                    0 => "1",
                    1 => "011",
                    -1 => "010",
                    2 => "000011",
                    -2 => "000010",
                    3 => "0000011",
                    _ => "0000010"
                });
                x = a1;
                white = !white;
            }
            else
            {
                bits.Append("001");
                EncodeRun(bits, a1 - x, white);
                EncodeRun(bits, a2 - a1, !white);
                x = a2;
            }
        }
    }
}
