using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UglyToad.PdfPig.Filters;
using UglyToad.PdfPig.Filters.CcittFax;
using UglyToad.PdfPig.Fonts;
using UglyToad.PdfPig.Tests.Images;
using UglyToad.PdfPig.Tokens;

namespace UglyToad.PdfPig.Tests.Filters;
public class CcittFaxDecoderTests
{
    // Targeted regressions: independent pixel expectations and explicit failure contracts.
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void ShortRunMasksMatchExpectedPixelsAtEveryByteOffset(bool lenient, bool blackIsOne)
    {
        var whiteCodes = new[]
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
        var blackCodes = new[]
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
        for (var white = 0; white <= 8; white++)
            for (var black = 0; black <= 8; black++)
            {
                var columns = white + black;
                if (columns == 0)
                    continue;
                var rowBits = (whiteCodes[white] + (black == 0 ? "" : blackCodes[black]));
                rowBits = rowBits.PadRight((rowBits.Length + 7) / 8 * 8, '0');
                Action<byte[], bool> decoder = (destination, polarity) => DecodeInto(Bits(rowBits + rowBits), columns, CcittFaxCompressionType.ModifiedHuffman, true, lenient, destination, polarity);
                var rowBytes = (columns + 7) / 8;
                var expected = new byte[rowBytes * 2];
                for (var row = 0; row < 2; row++)
                    for (var x = white; x < columns; x++)
                        expected[row * rowBytes + x / 8] |= (byte)(1 << (7 - x % 8));
                if (!blackIsOne)
                    for (var i = 0; i < expected.Length; i++)
                        expected[i] = (byte)~expected[i];
                var actual = Enumerable.Repeat((byte)0xAA, expected.Length).ToArray();
                decoder(actual, blackIsOne);
                Assert.Equal(expected, actual);
            }
    }

    [Theory]
    [InlineData("00000010000011010011", 64, 29)]
    [InlineData("000000010011001101010000000100110000110111", 4096, 2048)]
    [InlineData("0011010100000011011000000110111", 512, 0)]
    public void LongCodesMatchExpectedPixelsIncludingMakeupAndThirteenBitCodes(string bits, int columns, int whitePixels)
    {
        foreach (var lenient in new[]
        {
            false,
            true
        }

        )
            foreach (var blackIsOne in new[]
            {
                false,
                true
            }

            )
            {
                Action<byte[], bool> decoder = (destination, polarity) => DecodeInto(Bits(bits), columns, CcittFaxCompressionType.ModifiedHuffman, false, lenient, destination, polarity);
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
        var input = Bits(new string ('0', 4096) + "000000000001" + "10011");
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
    [InlineData(false)]
    [InlineData(true)]
    public void DirectOutputMatchesFixtureInBothPolarities(bool blackIsOne)
    {
        var input = ImageHelpers.LoadFileBytes("ccittfax-encoded.bin");
        var expected = ImageHelpers.LoadFileBytes("ccittfax-decoded.bin");
        if (!blackIsOne)
            for (var i = 0; i < expected.Length; i++)
                expected[i] = (byte)~expected[i];
        Action<byte[], bool> decoder = (destination, polarity) => DecodeInto(input, 1800, CcittFaxCompressionType.Group4_2D, false, false, destination, polarity);
        var output = Enumerable.Repeat((byte)0xAA, expected.Length).ToArray();
        decoder(output, blackIsOne);
        Assert.Equal(expected, output);
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
        foreach (var columns in new[]
        {
            1,
            7,
            8,
            9,
            16,
            31,
            64
        }

        )
            foreach (var lenient in new[]
            {
                false,
                true
            }

            )
                foreach (var aligned in new[]
                {
                    false,
                    true
                }

                )
                    for (var sample = 0; sample < 32; sample++)
                    {
                        var input = new byte[random.Next(0, 49)];
                        random.NextBytes(input);
                        foreach (var blackIsOne in new[]
                        {
                            false,
                            true
                        }

                        )
                        {
                            Action<byte[], bool> wholeDecoder = (destination, polarity) => DecodeInto(input, columns, type, aligned, lenient, destination, polarity);
                            var paddedInput = Enumerable.Repeat((byte)0xAA, input.Length + 10).ToArray();
                            input.CopyTo(paddedInput, 5);
                            Action<byte[], bool> directDecoder = (destination, polarity) => DecodeInto(paddedInput.AsMemory(5, input.Length), columns, type, aligned, lenient, destination, polarity);
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
        // White zero followed by black one, then EOF: seven unused bits must also invert.
        Action<byte[], bool> decoder = (destination, polarity) => DecodeInto(new byte[] { 0x35, 0x40 }, 1, CcittFaxCompressionType.ModifiedHuffman, true, false, destination, polarity);
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
        // First row: white one, black one, white six (13 bits plus byte padding).
        // Looking ahead for the final short code can prefetch the second white row.
        Action<byte[], bool> decoder = (destination, polarity) => DecodeInto(new byte[] { 0x1D, 0x70, 0x98 }, 8, CcittFaxCompressionType.ModifiedHuffman, true, lenient, destination, polarity);
        Assert.Equal(new byte[] { 0x40, 0 }, DecodeBytes(decoder, 2));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OversizedRunRespectsParsingMode(bool lenient)
    {
        Action<byte[], bool> decoder = (destination, polarity) => DecodeInto(new byte[] { 0xA8 }, 8, CcittFaxCompressionType.ModifiedHuffman, false, lenient, destination, polarity);
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
        Action<byte[], bool> decoder = (destination, polarity) => DecodeInto(new byte[] { 0x00, 0x80 }, 8, CcittFaxCompressionType.Group4_2D, false, lenient, destination, polarity);
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
        Action<byte[], bool> decoder = (destination, polarity) => DecodeInto(new byte[] { 0x05 }, 1, CcittFaxCompressionType.Group4_2D, false, lenient, destination, polarity);
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
        // Two consecutive EOL codes form a legal Group 4 EOFB marker.
        Action<byte[], bool> decoder = (destination, polarity) => DecodeInto(new byte[] { 0x00, 0x10, 0x01 }, 8, CcittFaxCompressionType.Group4_2D, false, lenient, destination, polarity);
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

        Action<byte[], bool> decoder = (destination, polarity) => DecodeInto(input, 8, CcittFaxCompressionType.ModifiedHuffman, false, true, destination, polarity);
        var exception = Assert.Throws<CorruptCompressedDataException>(() => DecodeBytes(decoder, 1)[0]);
        Assert.IsType<OverflowException>(exception.InnerException);
        Compare(input, 8, 1, CcittFaxCompressionType.ModifiedHuffman, false, true, true);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ActualEndOfInputStillPadsWithZeros(bool lenient)
    {
        Action<byte[], bool> decoder = (destination, polarity) => DecodeInto(new byte[] { 0x00 }, 8, CcittFaxCompressionType.ModifiedHuffman, false, lenient, destination, polarity);
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
        Action<byte[], bool> decoder = (destination, polarity) => DecodeInto(new byte[] { 0x00, 0x80 }, 8, CcittFaxCompressionType.ModifiedHuffman, false, lenient, destination, polarity);
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
        var parameters = new DictionaryToken(new Dictionary<NameToken, IToken> { [NameToken.Columns] = new NumericToken(8), [NameToken.Rows] = new NumericToken(1), [NameToken.EndOfLine] = BooleanToken.False, [NameToken.BlackIs1] = BooleanToken.True });
        var dictionary = new DictionaryToken(new Dictionary<NameToken, IToken> { [NameToken.Filter] = NameToken.CcittfaxDecode, [NameToken.DecodeParms] = parameters });
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
        // Alternating white-zero and black-zero runs exceed the three change entries
        // available for a one-pixel row without advancing the output position.
        var bits = string.Concat(Enumerable.Repeat("001101010000110111", 4));
        var input = Enumerable.Range(0, (bits.Length + 7) / 8).Select(i => Convert.ToByte(bits.Substring(i * 8, Math.Min(8, bits.Length - i * 8)).PadRight(8, '0'), 2)).ToArray();
        Action<byte[], bool> decoder = (destination, polarity) => DecodeInto(input, 1, CcittFaxCompressionType.ModifiedHuffman, false, lenient, destination, polarity);
        var exception = Assert.Throws<CorruptCompressedDataException>(() => decoder(new byte[1], true));
        Assert.IsType<IndexOutOfRangeException>(exception.InnerException);
    }

    private static byte[] DecodeBytes(Action<byte[], bool> decoder, int count)
    {
        var output = new byte[count];
        decoder(output, true);
        return output;
    }

    // Output capacity specifies whole rows; every targeted regression invokes the production decoder.
    private static void DecodeInto(ReadOnlyMemory<byte> input, int width, CcittFaxCompressionType mode, bool aligned, bool lenient, byte[] output, bool polarity)
    {
        int stride = (width + 7) / 8;
        Assert.Equal(0, output.Length % stride);
        CcittFaxCompactDecoder.Decode(input.Span, output, width, output.Length / stride, mode, aligned, polarity, lenient);
    }

    // Compact path and public filter: fixtures, row formats, alignment and output polarity.
    private static DictionaryToken Dictionary(int width, int rows, CcittFaxCompressionType mode, bool aligned, bool polarity)
    {
        var parms = new DictionaryToken(new System.Collections.Generic.Dictionary<NameToken, IToken> { { NameToken.Columns, new NumericToken(width) }, { NameToken.Rows, new NumericToken(rows) }, { NameToken.K, new NumericToken(mode == CcittFaxCompressionType.Group4_2D ? -1 : mode == CcittFaxCompressionType.Group3_2D ? 2 : 0) }, { NameToken.EndOfLine, mode == CcittFaxCompressionType.ModifiedHuffman ? BooleanToken.False : BooleanToken.True }, { NameToken.EncodedByteAlign, aligned ? BooleanToken.True : BooleanToken.False }, { NameToken.BlackIs1, polarity ? BooleanToken.True : BooleanToken.False } });
        return new DictionaryToken(new System.Collections.Generic.Dictionary<NameToken, IToken> { { NameToken.Filter, NameToken.CcittfaxDecode }, { NameToken.DecodeParms, parms } });
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
        var output = new byte[expected.Length];
        Assert.True(CcittFaxCompactDecoder.TryDecode(input, output, 1800, 3113, CcittFaxCompressionType.Group4_2D, false, polarity));
        Assert.Equal(expected, output);
        var filter = new CcittFaxDecodeFilter().Decode(input, Dictionary(1800, 3113, CcittFaxCompressionType.Group4_2D, false, polarity), DefaultFilterProvider.Instance, 0);
        Assert.Equal(expected, filter.ToArray());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void IndependentHuffmanPixelsCoverPaddingAlignmentAndFill(bool polarity)
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
        foreach (bool aligned in new[]
        {
            true,
            false
        }

        )
            foreach (var mode in new[]
            {
                CcittFaxCompressionType.ModifiedHuffman,
                CcittFaxCompressionType.Group3_1D
            }

            )
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
                        byte[] input = Bits(row + row);
                        var expected = new byte[stride * 2];
                        for (int y = 0; y < 2; y++)
                            for (int x = w; x < width; x++)
                                expected[y * stride + x / 8] |= (byte)(128 >> (x % 8));
                        if (!polarity)
                            for (int i = 0; i < expected.Length; i++)
                                expected[i] = (byte)~expected[i];
                        var output = new byte[expected.Length];
                        Assert.True(CcittFaxCompactDecoder.TryDecode(input, output, width, 2, mode, aligned, polarity));
                        Assert.Equal(expected, output);
                        Assert.Equal(expected, new CcittFaxDecodeFilter().Decode(input, Dictionary(width, 2, mode, aligned, polarity), DefaultFilterProvider.Instance, 0).ToArray());
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
        foreach (var mode in new[]
        {
            CcittFaxCompressionType.Group4_2D,
            CcittFaxCompressionType.Group3_2D
        }

        )
        {
            string bits = mode == CcittFaxCompressionType.Group4_2D ? "00110000011" + "11" + "0001" + "1" : eol + "1" + "10000011" + eol + "0" + "11" + eol + "1" + "10011" + eol + "0" + "1";
            var input = Bits(bits);
            var output = new byte[4];
            Assert.True(CcittFaxCompactDecoder.TryDecode(input, output, 8, 4, mode, false, polarity));
            Assert.Equal(expected, output);
            Assert.Equal(expected, new CcittFaxDecodeFilter().Decode(input, Dictionary(8, 4, mode, false, polarity), DefaultFilterProvider.Instance, 0).ToArray());
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void MalformedFilterRetainsCompatibilityResultsAndExceptions(bool lenient)
    {
        var random = new Random(1435);
        foreach (var mode in new[]
        {
            CcittFaxCompressionType.ModifiedHuffman,
            CcittFaxCompressionType.Group3_1D,
            CcittFaxCompressionType.Group3_2D,
            CcittFaxCompressionType.Group4_2D
        }

        )
            foreach (int width in new[]
            {
                1,
                7,
                8,
                9,
                31,
                64
            }

            )
                for (int sample = 0; sample < 128; sample++)
                {
                    var input = new byte[random.Next(1, 65)];
                    random.NextBytes(input);
                    bool aligned = sample % 2 == 0, polarity = sample % 3 == 0;
                    byte[]? actual = null;
                    var expected = new byte[(width + 7) / 8 * 8];
                    var previousError = Record.Exception(() => new CcittBaseline.CcittFaxDecoder(input, width, mode, aligned, lenient).DecodeInto(expected, polarity));
                    var actualError = Record.Exception(() => actual = new CcittFaxDecodeFilter(lenient).Decode(input, Dictionary(width, 8, mode, aligned, polarity), DefaultFilterProvider.Instance, 0).ToArray());
                    Assert.Equal(previousError?.GetType(), actualError?.GetType());
                    if (previousError == null)
                        Assert.Equal(expected, actual);
                }
    }

    // Compatibility: compare production and signed paths against the frozen former decoder.
    private static void Compare(byte[] input, int width, int rows, CcittFaxCompressionType mode, bool aligned, bool polarity, bool lenient)
    {
        var expected = new byte[(width + 7) / 8 * rows];
        var actual = Enumerable.Repeat((byte)0xAA, expected.Length).ToArray();
        var oldError = Record.Exception(() => new CcittBaseline.CcittFaxDecoder(input, width, mode, aligned, lenient).DecodeInto(expected, polarity));
        var newError = Record.Exception(() => CcittFaxCompactDecoder.Decode(input, actual, width, rows, mode, aligned, polarity, lenient));
        var signed = new byte[expected.Length];
        var signedError = Record.Exception(() => CcittFaxCompactDecoder.DecodeFullWidth(input, signed, width, rows, mode, aligned, polarity, lenient));
        Assert.Equal(oldError?.GetType(), signedError?.GetType());
        if (oldError == null)
            Assert.Equal(expected, signed);
        else
        {
            Assert.Equal(oldError.Message, signedError!.Message);
            Assert.Equal(oldError.InnerException?.GetType(), signedError.InnerException?.GetType());
        }

        string detail = $"width={width},rows={rows},mode={mode},aligned={aligned},polarity={polarity},lenient={lenient},inputLength={input.Length},prefix={Convert.ToBase64String(input.Take(96).ToArray())}";
        Assert.True(oldError?.GetType() == newError?.GetType(), detail + $",old={oldError},new={newError}");
        if (oldError == null)
            Assert.True(expected.AsSpan().SequenceEqual(actual), detail);
        else
        {
            Assert.Equal(oldError.InnerException?.GetType(), newError!.InnerException?.GetType());
            Assert.Equal(oldError.Message, newError.Message);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MalformedAndTruncatedRowsMatchPreviousDecoder(bool lenient)
    {
        var random = new Random(7716080);
        foreach (var mode in new[]
        {
            CcittFaxCompressionType.ModifiedHuffman,
            CcittFaxCompressionType.Group3_1D,
            CcittFaxCompressionType.Group3_2D,
            CcittFaxCompressionType.Group4_2D
        }

        )
            foreach (int width in new[]
            {
                1,
                7,
                8,
                9,
                16,
                31,
                64,
                257
            }

            )
                for (int sample = 0; sample < 512; sample++)
                {
                    var input = new byte[random.Next(0, 97)];
                    random.NextBytes(input);
                    Compare(input, width, 8, mode, sample % 2 == 0, sample % 3 == 0, lenient);
                }
    }

    private static byte[] Bits(string bits)
    {
        var data = new byte[(bits.Length + 7) / 8];
        for (int i = 0; i < bits.Length; i++)
            if (bits[i] == '1')
                data[i / 8] |= (byte)(128 >> (i & 7));
        return data;
    }

    private static string Run(int length, bool white)
    {
        var codes = white ? CcittFaxCodebook.White : CcittFaxCodebook.Black;
        string code(int run)
        {
            var c = codes.First(c => c.Run == run);
            return Convert.ToString(c.Bits, 2).PadLeft(c.Length, '0');
        }

        string result = "";
        while (length >= 2560)
        {
            result += code(2560);
            length -= 2560;
        }

        if (length >= 64)
        {
            int makeup = length / 64 * 64;
            result += code(makeup);
            length -= makeup;
        }

        return result + code(length);
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
        }

        )
            foreach (bool polarity in new[]
            {
                false,
                true
            }

            )
                foreach (bool aligned in new[]
                {
                    false,
                    true
                }

                )
                {
                    string row = Run(0, true) + Run(width, false);
                    row = mode == CcittFaxCompressionType.Group4_2D ? "001" + row : mode == CcittFaxCompressionType.ModifiedHuffman ? row : eol + (mode == CcittFaxCompressionType.Group3_2D ? "1" : "") + row;
                    if (aligned)
                        row = row.PadRight((row.Length + 7) / 8 * 8, '0');
                    var input = Bits(row + row);
                    Compare(input, width, 2, mode, aligned, polarity, false);
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
    public void FixtureAndEveryTruncatedSuffixMatchPreviousDecoder(bool lenient)
    {
        var input = ImageHelpers.LoadFileBytes("ccittfax-encoded.bin");
        var expected = ImageHelpers.LoadFileBytes("ccittfax-decoded.bin");
        var actual = new byte[expected.Length];
        CcittFaxCompactDecoder.Decode(input, actual, 1800, 3113, CcittFaxCompressionType.Group4_2D, false, true, lenient);
        Assert.Equal(expected, actual);
        CcittFaxCompactDecoder.DecodeFullWidth(input, actual, 1800, 3113, CcittFaxCompressionType.Group4_2D, false, true, lenient);
        Assert.Equal(expected, actual);
        for (int length = 0; length <= Math.Min(input.Length, 96); length++)
            Compare(input.Take(length).ToArray(), 1800, 8, CcittFaxCompressionType.Group4_2D, false, length % 2 == 0, lenient);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FullWidthMalformedRowsMatchPreviousDecoder(bool lenient)
    {
        var random = new Random(1435);
        foreach (var mode in new[]
        {
            CcittFaxCompressionType.ModifiedHuffman,
            CcittFaxCompressionType.Group3_1D,
            CcittFaxCompressionType.Group3_2D,
            CcittFaxCompressionType.Group4_2D
        }

        )
            for (int sample = 0; sample < 32; sample++)
            {
                var input = new byte[random.Next(0, 97)];
                random.NextBytes(input);
                Compare(input, 65536, 3, mode, sample % 2 == 0, sample % 3 == 0, lenient);
            }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LateRowFailuresKeepEarlierRowsAndReferenceTransitions(bool lenient)
    {
        foreach (bool polarity in new[]
        {
            false,
            true
        }

        )
            foreach (string suffix in new[]
            {
                "00000011",
                "0000101",
                "000000000001000000000001",
                "00100110101000101"
            }

            )
            {
                var input = Bits(new string ('1', 17) + suffix);
                Compare(input, 1, 24, CcittFaxCompressionType.Group4_2D, false, polarity, lenient);
            }

        var fixture = ImageHelpers.LoadFileBytes("ccittfax-encoded.bin").Take(96).ToArray();
        for (int bit = 0; bit < fixture.Length * 8; bit++)
        {
            var input = (byte[])fixture.Clone();
            input[bit / 8] ^= (byte)(128 >> (bit & 7));
            Compare(input, 1800, 32, CcittFaxCompressionType.Group4_2D, false, bit % 2 == 0, lenient);
        }
    }

    // Filter integration uses self-contained synthetic vectors and the frozen decoder.
    private static DictionaryToken Dictionary(DecodeOptions options)
    {
        var parms = new DictionaryToken(new System.Collections.Generic.Dictionary<NameToken, IToken> { { NameToken.Columns, new NumericToken(options.Width) }, { NameToken.Rows, new NumericToken(options.Height) }, { NameToken.K, new NumericToken(options.K) }, { NameToken.EndOfLine, options.EndOfLine ? BooleanToken.True : BooleanToken.False }, { NameToken.EncodedByteAlign, options.Aligned ? BooleanToken.True : BooleanToken.False }, { NameToken.BlackIs1, options.BlackIsOne ? BooleanToken.True : BooleanToken.False } });
        return new DictionaryToken(new System.Collections.Generic.Dictionary<NameToken, IToken> { { NameToken.Filter, NameToken.CcittfaxDecode }, { NameToken.DecodeParms, parms } });
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void IntegratedFilterMatchesIndependentPixelsAndBaselineBytes(bool lenient)
    {
        foreach (var vector in GenerateTestImages())
        {
            var options = vector.Options;
            var dictionary = Dictionary(options);
            var actual = new CcittFaxDecodeFilter(lenient).Decode(vector.Input, dictionary, DefaultFilterProvider.Instance, 0);
            var previous = DecodeBaselineFilter(vector.Input, options, lenient);
            Assert.Equal(previous.ToArray(), actual.ToArray());
            AssertPixelsEqual(vector.Expected, actual.ToArray(), options.Width, options.Height, vector.Name);
            var compact = new byte[actual.Length];
            var mode = options.K < 0 ? CcittFaxCompressionType.Group4_2D : options.K > 0 ? CcittFaxCompressionType.Group3_2D : options.Rle ? CcittFaxCompressionType.ModifiedHuffman : CcittFaxCompressionType.Group3_1D;
            Assert.True(CcittFaxCompactDecoder.TryDecode(vector.Input, compact, options.Width, options.Height, mode, options.Aligned, options.BlackIsOne));
            Assert.Equal(actual.ToArray(), compact);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void MalformedFilterMatchesBaselineBytesAndFailureContract(bool lenient)
    {
        var random = new Random(1435);
        foreach (int mode in new[]
        {
            -1,
            0,
            1,
            2
        }

        )
            foreach (int width in new[]
            {
                1,
                7,
                8,
                9,
                31,
                64
            }

            )
                for (int sample = 0; sample < 128; sample++)
                {
                    var options = new DecodeOptions(width, 8, mode == 1 ? 0 : mode, rle: mode == 1, aligned: sample % 2 == 0, blackIsOne: sample % 3 == 0, endOfLine: mode == 0 || mode == 2);
                    var dictionary = Dictionary(options);
                    var input = new byte[random.Next(65)];
                    random.NextBytes(input);
                    byte[]? oldBytes = null, newBytes = null;
                    var oldError = Record.Exception(() => oldBytes = DecodeBaselineFilter(input, options, lenient));
                    var newError = Record.Exception(() => newBytes = new CcittFaxDecodeFilter(lenient).Decode(input, dictionary, DefaultFilterProvider.Instance, 0).ToArray());
                    string caseInfo = $"mode={mode},width={width},sample={sample},lenient={lenient},input={Convert.ToBase64String(input)}";
                    Assert.True(oldError?.GetType() == newError?.GetType(), caseInfo + $",old={oldError?.GetType().Name}:{oldError?.Message},new={newError?.GetType().Name}");
                    Assert.True(oldBytes == null ? newBytes == null : newBytes != null && oldBytes.AsSpan().SequenceEqual(newBytes), caseInfo);
                }
    }

    [Theory]
    [InlineData(65535)]
    [InlineData(65536)]
    public void CompactWidthBoundaryPreservesOutput(int width)
    {
        foreach (bool polarity in new[]
        {
            true,
            false
        }

        )
        {
            var options = new DecodeOptions(width, 2, blackIsOne: polarity);
            var dictionary = Dictionary(options);
            byte[] input =
            {
                0xC0
            };
            Assert.Equal(DecodeBaselineFilter(input, options, true), new CcittFaxDecodeFilter().Decode(input, dictionary, DefaultFilterProvider.Instance, 0).ToArray());
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void FillAndIncompleteRtcPreserveIntegratedAcceptance(bool lenient)
    {
        const string eol = "000000000001";
        byte[] input = Bits("0000" + eol + "10011" + "0000000" + eol + "10011" + eol + eol + eol);
        var options = new DecodeOptions(8, 2, 0, endOfLine: true);
        var dictionary = Dictionary(options);
        var actual = new CcittFaxDecodeFilter(lenient).Decode(input, dictionary, DefaultFilterProvider.Instance, 0);
        Assert.Equal(new byte[2], actual.ToArray());
        Assert.Equal(DecodeBaselineFilter(input, options, lenient), actual.ToArray());
    }

    // These cases use positive dimensions and explicit row-format parameters. Expected bytes come
    // from the frozen decoder; the separate empty-input expectation retains the filter's contract.
    // Dimension limits and parameter resolution are tested independently by CcittFaxDecodeFilterTests.
    private static byte[] DecodeBaselineFilter(byte[] input, DecodeOptions options, bool lenient)
    {
        if (input.Length == 0)
        {
            if (lenient)
                return Array.Empty<byte>();
            throw new CorruptCompressedDataException("Empty CCITT compressed data.");
        }

        var output = new byte[(options.Width + 7) / 8 * options.Height];
        new CcittBaseline.CcittFaxDecoder(input, options.Width, options.Mode, options.Aligned, lenient).DecodeInto(output, options.BlackIsOne);
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
        internal CcittFaxCompressionType Mode => K < 0 ? CcittFaxCompressionType.Group4_2D : K > 0 ? CcittFaxCompressionType.Group3_2D : Rle ? CcittFaxCompressionType.ModifiedHuffman : CcittFaxCompressionType.Group3_1D;

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

    // The test encoder derives input from pixels with the frozen codebook, independently of the
    // production lookup builders. Expected pixels are packed directly, not decoded by either codec.
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
        return new(name, Bits(bits.ToString()), expected, options);
    }

    private static IEnumerable<TestImage> GenerateTestImages()
    {
        var random = new Random(409);
        foreach (int k in new[]
        {
            -1,
            0,
            2
        }

        )
            foreach (int width in new[]
            {
                1,
                7,
                8,
                9,
                31,
                64,
                127,
                512,
                1800,
                4096
            }

            )
                foreach (bool polarity in new[]
                {
                    true,
                    false
                }

                )
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

    private static void EncodeRun(StringBuilder bits, int run, bool white)
    {
        var codes = white ? CcittBaseline.CcittFaxCodebook.White : CcittBaseline.CcittFaxCodebook.Black;
        while (run >= 64)
        {
            var code = codes.Where(c => c.Run >= 64 && c.Run <= run).OrderByDescending(c => c.Run).First();
            Append(bits, code.Bits, code.Length);
            run -= code.Run;
        }

        var terminating = codes.Single(c => c.Run == run);
        Append(bits, terminating.Bits, terminating.Length);
    }

    private static void Append(StringBuilder bits, int code, int length) => bits.Append(Convert.ToString(code, 2).PadLeft(length, '0'));
    private static int[] Changes(byte[] pixels)
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
        foreach (int end in Changes(pixels))
        {
            EncodeRun(bits, end - x, white);
            x = end;
            white = !white;
        }
    }

    private static void Encode2D(StringBuilder bits, byte[] pixels, byte[] previous)
    {
        int width = pixels.Length, x = 0;
        var current = Changes(pixels);
        var reference = Changes(previous);
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
