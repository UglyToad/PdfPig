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
    /*
     * Portions adapted from Apache PDFBox 3.0.8 CCITTFactoryTest and its TIFF fixtures.
     * Copyright 2014 The Apache Software Foundation.
     * Licensed under the Apache License, Version 2.0 (the "License");
     * you may not use this file except in compliance with the License.
     * You may obtain a copy of the License at https://www.apache.org/licenses/LICENSE-2.0
     * Unless required by applicable law or agreed to in writing, software distributed
     * under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR
     * CONDITIONS OF ANY KIND, either express or implied. See the License for the
     * specific language governing permissions and limitations under the License.
     * Adaptation: extract the CCITT strips, verify decoded pixels without TIFF import,
     * and exercise both PdfPig polarity and parsing settings.
     */

    /// <summary>Checks PDFBox's single-image and multi-image TIFF cases against independently decoded pixels.</summary>
    /// <remarks>
    /// Adapted from testCreateFromRandomAccessSingle and testCreateFromRandomAccessMulti in
    /// <see href="https://github.com/apache/pdfbox/blob/3.0.8/pdfbox/src/test/java/org/apache/pdfbox/pdmodel/graphics/image/CCITTFactoryTest.java">Apache PDFBox 3.0.8 CCITTFactoryTest</see>.
    /// Base64 constants contain the unchanged single CCITT strip from each source image; no TIFF parser is tested here.
    /// Expected SHA-256 values were generated once with Pillow 12.3.0 (libtiff) from each decoded
    /// TIFF image in mode 1: MSB-first, byte-padded rows, white=1. They are independent of PdfPig.
    /// </remarks>
    [Theory]
    [InlineData("ccittg3.tif", 0, 344, 287, 0, PdfBoxGroup3, "3703A34E33947B4426A41AEAB8CD405420D1C5BA4911FA83B1CC41AF40A9ADED")]
    [InlineData("ccittg4.tif", 0, 344, 287, -1, PdfBoxGroup4, "3703A34E33947B4426A41AEAB8CD405420D1C5BA4911FA83B1CC41AF40A9ADED")]
    [InlineData("ccittg4multi.tif", 0, 344, 287, -1, PdfBoxMultiPage0, "08E2ABD03263C1076BE0EFE454937F5035AA8C1DB2442E21ABD57623D5D8F856")]
    [InlineData("ccittg4multi.tif", 1, 344, 287, -1, PdfBoxMultiPage1, "2861E68D2CF43CDF11DB188C9E0315FABFEF88D1BE58BAE00F2D96DFE17BBE7C")]
    [InlineData("ccittg4multi.tif", 2, 344, 287, -1, PdfBoxMultiPage2, "5D6C75C856CDCAD8204E1A4530EC14316AA932B0D3C7DDBE73638D80596A45E0")]
    public void PdfBoxTiffImagesMatchIndependentPixels(string sourceFile, int imageIndex,
        int columns, int rows, int k, string encodedStrip, string expectedPixelHash)
    {
        var input = Convert.FromBase64String(encodedStrip);
        foreach (var lenient in new[] { false, true })
            foreach (var blackIsOne in new[] { false, true })
            {
                var options = new DecodeOptions(columns, rows, k, blackIsOne: blackIsOne, endOfLine: k == 0);
                var actual = new CcittFaxDecodeFilter(lenient).Decode(input,
                    CreateImageDictionary(options), DefaultFilterProvider.Instance, 0).ToArray();
                Assert.Equal(((columns + 7) / 8) * rows, actual.Length);
                // Normalize polarity to the independently decoded TIFF raster before hashing.
                if (blackIsOne)
                    for (var i = 0; i < actual.Length; i++)
                        actual[i] = (byte)~actual[i];
                using var hash = System.Security.Cryptography.SHA256.Create();
                var actualPixelHash = BitConverter.ToString(hash.ComputeHash(actual)).Replace("-", "");
                Assert.True(expectedPixelHash == actualPixelHash,
                    $"{sourceFile}, image {imageIndex}, lenient={lenient}, blackIsOne={blackIsOne}: {actualPixelHash}");
            }
    }

    /// <summary>Verifies PDFBox's 343 by 287 alternating-pixel image, including the partial final byte.</summary>
    /// <remarks>
    /// Adapted from testCreateFromBufferedChessImage in
    /// <see href="https://github.com/apache/pdfbox/blob/3.0.8/pdfbox/src/test/java/org/apache/pdfbox/pdmodel/graphics/image/CCITTFactoryTest.java">Apache PDFBox 3.0.8 CCITTFactoryTest</see>.
    /// PDFBox starts with black and alternates pixels while walking columns. Both dimensions are odd,
    /// so this is equivalent to alternating x+y parity. The existing independent test encoder replaces
    /// PDFBox's image encoder; expected pixels are checked directly, excluding the unused row-padding bit.
    /// </remarks>
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void PdfBoxChessImagePreservesPixelsAtNonByteAlignedWidth(bool lenient, bool blackIsOne)
    {
        const int columns = 343;
        const int rows = 287;
        var pixels = new byte[rows][];
        for (var y = 0; y < rows; y++)
        {
            pixels[y] = new byte[columns];
            for (var x = 0; x < columns; x++)
                pixels[y][x] = (byte)(((x + y) & 1) == 0 ? 1 : 0);
        }
        var options = new DecodeOptions(columns, rows, blackIsOne: blackIsOne);
        var image = EncodeTestImage(pixels, options, "PDFBox chess image");
        var actual = new CcittFaxDecodeFilter(lenient).Decode(image.Input,
            CreateImageDictionary(options), DefaultFilterProvider.Instance, 0).ToArray();
        AssertPixelsEqual(image.Expected, actual, columns, rows, image.Name);
    }

    // ccittg3.tif, image 0; original TIFF SHA-256: B8BC24334D4B0D55A5DBB678FFD30A329C86A48C45F0416FEE224EDF3C0566CE.
    private const string PdfBoxGroup3 =
        "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" +
        "duRkADtVDRY3ORmADtYQ4Zi8AAmygA==";

    // ccittg4.tif, image 0; original TIFF SHA-256: 57254CEB1AB84172C5D346B90570971C40079444BBBC7F5BFE11F66CD675ACE7.
    private const string PdfBoxGroup4 =
        "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";

    // ccittg4multi.tif, image 0; original TIFF SHA-256: 6E46A84CCE1F7D22BE3C6EAEF45DF37C5B4878C4612C9524A2FD7A722573AE1C.
    private const string PdfBoxMultiPage0 =
        "8r+JCDhBwnThOnWnBacLWnWFpwWsLWta1hYLWFrC1hawsLWsLWta0QNx9BdBdLpdLhLoLhBdLhBdAul0Fwl0F0uguklS6XCXQXS6" +
        "XrpdLpXS6Cel4XCXQXS4QXS6XS6XS4S6C6XXpdLpeul66+l169eq14S4Xa9dqtdrtdhdrhhcMLj//////////////////////f/9" +
        "/9/++//ff/vv/3/////////3/7/zIFBSmRUBPMhYCsnCQhIY2NBB0HTp06fp+n////a9r2rVq1DChhSurBvH//////gAgAg=";

    // ccittg4multi.tif, image 1; original TIFF SHA-256: 6E46A84CCE1F7D22BE3C6EAEF45DF37C5B4878C4612C9524A2FD7A722573AE1C.
    private const string PdfBoxMultiPage1 =
        "/+ZCgeZahqzJAbcEDgg4QcJzsyA+na0B4lOnCDhB06dOE6e6dd090/Tp+nkDw37RBXVvIbY1pBvCegg+Q0wvQV99N99P29f9N2vt" +
        "2v9rtPa7XDCeGFx9/+///3/7/////////19a+l66rWlWEq0q0oWlWlSrSpQtKFpQlMjAEkyUATwlCChKlMiALISglCVKEoQVKEqV" +
        "KCVKgqUJUFQVBQgtUFQVECuSmiBRIlQVBUFhUQZF3QWCpaoLVBZAsMyJtUiHh0973Xf3/X3/99X1e1atQ1DU7UwPAkmQGB5R////" +
        "/////////4AIAIA=";

    // ccittg4multi.tif, image 2; original TIFF SHA-256: 6E46A84CCE1F7D22BE3C6EAEF45DF37C5B4878C4612C9524A2FD7A722573AE1C.
    private const string PdfBoxMultiPage2 =
        "/+dlAHg3nYoB4NUIgeGtKsIHQdB06Dw6e6e973v73b9vdu9u8PDcyWg2pkZBrweHv7d7/3///r61rWlWFrSrCVaVKFglBaULCUJQ" +
        "SpUFCUJQlShKCChKlSglMjgeoSglBBQShKEFO1gDwsIKEFCIHiSAwSggp2UAeDwgoIKEFCUIKgqCoOgdB4dPdPD3h73ve74dvdu3" +
        "btw3Dc7SgPB5k4HiweZAYKsNw3eG7d7d7duHt+3eG77vu993v7/3///9fWvrS9fXVaVaVLUJVpQsJTsVBuU4H07GwPBWhYSpUq0t" +
        "UtUtfWta1hWtWsKwrVhWFDChkDwsgQ7WALx////////4AIAI";

    [Theory]
    [InlineData("00000010000011010011", 64, 29)]
    [InlineData("000000010011001101010000000100110000110111", 4096, 2048)]
    [InlineData("0011010100000011011000000110111", 512, 0)]
    public void LongCodesMatchExpectedPixelsIncludingMakeupAndThirteenBitCodes(string bits, int columns, int whitePixels)
    {
        foreach (var lenient in new[] { false, true })
            foreach (var blackIsOne in new[] { false, true })
            {
                var expected = new byte[(columns + 7) / 8];
                for (var x = whitePixels; x < columns; x++)
                    expected[x / 8] |= (byte)(1 << (7 - x % 8));
                if (!blackIsOne)
                    for (var i = 0; i < expected.Length; i++)
                        expected[i] = (byte)~expected[i];
                var actual = new byte[expected.Length];
                CcittFaxCompactDecoder.Decode(PackBits(bits), actual, columns, 1, CcittFaxCompressionType.ModifiedHuffman, false, blackIsOne, lenient);
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
        CcittFaxCompactDecoder.Decode(input, output, 8, 1, CcittFaxCompressionType.Group3_1D, false, true, lenient);
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
                            var paddedInput = Enumerable.Repeat((byte)0xAA, input.Length + 10).ToArray();
                            input.CopyTo(paddedInput, 5);
                            var expected = new byte[(columns + 7) / 8 * 4];
                            var actual = new byte[expected.Length];
                            var wholeException = Record.Exception(() => CcittFaxCompactDecoder.Decode(input, expected, columns, 4, type, aligned, blackIsOne, lenient));
                            var directException = Record.Exception(() => CcittFaxCompactDecoder.Decode(paddedInput.AsSpan(5, input.Length), actual, columns, 4, type, aligned, blackIsOne, lenient));
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
        var output = new byte[]
        {
            0xAA,
            0xAA,
            0xAA
        };
        CcittFaxCompactDecoder.Decode(new byte[] { 0x35, 0x40 }, output, 1, 3, CcittFaxCompressionType.ModifiedHuffman, true, blackIsOne, false);
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
        var output = new byte[2];
        CcittFaxCompactDecoder.Decode(new byte[] { 0x1D, 0x70, 0x98 }, output, 8, 2, CcittFaxCompressionType.ModifiedHuffman, true, true, lenient);
        Assert.Equal(new byte[] { 0x40, 0 }, output);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OversizedRunRespectsParsingMode(bool lenient)
    {
        var input = new byte[] { 0xA8 };
        var output = new byte[1];
        if (lenient)
        {
            CcittFaxCompactDecoder.Decode(input, output, 8, 1, CcittFaxCompressionType.ModifiedHuffman, false, true, lenient);
            Assert.Equal(0, output[0]);
        }
        else
        {
            Assert.Throws<CorruptCompressedDataException>(() => CcittFaxCompactDecoder.Decode(input, output, 8, 1, CcittFaxCompressionType.ModifiedHuffman, false, true, lenient));
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UnknownTwoDimensionalCodeRespectsParsingMode(bool lenient)
    {
        var input = new byte[] { 0x00, 0x80 };
        var output = new byte[1];
        if (lenient)
        {
            CcittFaxCompactDecoder.Decode(input, output, 8, 1, CcittFaxCompressionType.Group4_2D, false, true, lenient);
            Assert.Equal(0, output[0]);
        }
        else
        {
            Assert.Throws<CorruptCompressedDataException>(() => CcittFaxCompactDecoder.Decode(input, output, 8, 1, CcittFaxCompressionType.Group4_2D, false, true, lenient));
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NegativeVerticalPositionRespectsParsingMode(bool lenient)
    {
        var input = new byte[] { 0x05 };
        var output = new byte[1];
        if (lenient)
        {
            CcittFaxCompactDecoder.Decode(input, output, 1, 1, CcittFaxCompressionType.Group4_2D, false, true, lenient);
            Assert.Equal(0x80, output[0]);
        }
        else
        {
            Assert.Throws<CorruptCompressedDataException>(() => CcittFaxCompactDecoder.Decode(input, output, 1, 1, CcittFaxCompressionType.Group4_2D, false, true, lenient));
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Group4EndOfBlockRetainsZeroPadding(bool lenient)
    {
        // Two consecutive end-of-line (EOL) codes form the Group 4 end-of-facsimile-block
        // (EOFB) marker. Rows requested after this marker remain white.
        var output = new byte[2];
        CcittFaxCompactDecoder.Decode(new byte[] { 0x00, 0x10, 0x01 }, output, 8, 2, CcittFaxCompressionType.Group4_2D, false, true, lenient);
        Assert.Equal(new byte[] { 0, 0 }, output);
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

        var output = new byte[1];
        var exception = Assert.Throws<CorruptCompressedDataException>(() => CcittFaxCompactDecoder.Decode(input, output, 8, 1, CcittFaxCompressionType.ModifiedHuffman, false, true, true));
        Assert.IsType<OverflowException>(exception.InnerException);
        AssertMatchesCompatibilityPath(input, 8, 1, CcittFaxCompressionType.ModifiedHuffman, false, true, true);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ActualEndOfInputStillPadsWithZeros(bool lenient)
    {
        var output = new byte[]
        {
            0xAA,
            0xAA
        };
        CcittFaxCompactDecoder.Decode(new byte[] { 0x00 }, output, 8, 2, CcittFaxCompressionType.ModifiedHuffman, false, true, lenient);
        Assert.Equal(new byte[] { 0, 0 }, output);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void InvalidHuffmanCodeRespectsParsingMode(bool lenient)
    {
        var input = new byte[] { 0x00, 0x80 };
        var output = new byte[1];
        if (lenient)
        {
            CcittFaxCompactDecoder.Decode(input, output, 8, 1, CcittFaxCompressionType.ModifiedHuffman, false, true, lenient);
            Assert.Equal(0, output[0]);
        }
        else
        {
            var exception = Assert.Throws<CorruptCompressedDataException>(() => CcittFaxCompactDecoder.Decode(input, output, 8, 1, CcittFaxCompressionType.ModifiedHuffman, false, true, lenient));
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
        var exception = Assert.Throws<CorruptCompressedDataException>(() => CcittFaxCompactDecoder.Decode(input, new byte[1], 1, 1, CcittFaxCompressionType.ModifiedHuffman, false, true, lenient));
        Assert.IsType<IndexOutOfRangeException>(exception.InnerException);
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
        (0x7, 4, 2), (0x8, 4, 3), (0xB, 4, 4), (0xC, 4, 5), (0xE, 4, 6),
        (0xF, 4, 7), (0x12, 5, 128), (0x13, 5, 8), (0x14, 5, 9), (0x1B, 5, 64),
        (0x7, 5, 10), (0x8, 5, 11), (0x17, 6, 192), (0x18, 6, 1664), (0x2A, 6, 16),
        (0x2B, 6, 17), (0x3, 6, 13), (0x34, 6, 14), (0x35, 6, 15), (0x7, 6, 1),
        (0x8, 6, 12), (0x13, 7, 26), (0x17, 7, 21), (0x18, 7, 28), (0x24, 7, 27),
        (0x27, 7, 18), (0x28, 7, 24), (0x2B, 7, 25), (0x3, 7, 22), (0x37, 7, 256),
        (0x4, 7, 23), (0x8, 7, 20), (0xC, 7, 19), (0x12, 8, 33), (0x13, 8, 34),
        (0x14, 8, 35), (0x15, 8, 36), (0x16, 8, 37), (0x17, 8, 38), (0x1A, 8, 31),
        (0x1B, 8, 32), (0x2, 8, 29), (0x24, 8, 53), (0x25, 8, 54), (0x28, 8, 39),
        (0x29, 8, 40), (0x2A, 8, 41), (0x2B, 8, 42), (0x2C, 8, 43), (0x2D, 8, 44),
        (0x3, 8, 30), (0x32, 8, 61), (0x33, 8, 62), (0x34, 8, 63), (0x35, 8, 0),
        (0x36, 8, 320), (0x37, 8, 384), (0x4, 8, 45), (0x4A, 8, 59), (0x4B, 8, 60),
        (0x5, 8, 46), (0x52, 8, 49), (0x53, 8, 50), (0x54, 8, 51), (0x55, 8, 52),
        (0x58, 8, 55), (0x59, 8, 56), (0x5A, 8, 57), (0x5B, 8, 58), (0x64, 8, 448),
        (0x65, 8, 512), (0x67, 8, 640), (0x68, 8, 576), (0xA, 8, 47), (0xB, 8, 48),
        (0x98, 9, 1472), (0x99, 9, 1536), (0x9A, 9, 1600), (0x9B, 9, 1728), (0xCC, 9, 704),
        (0xCD, 9, 768), (0xD2, 9, 832), (0xD3, 9, 896), (0xD4, 9, 960), (0xD5, 9, 1024),
        (0xD6, 9, 1088), (0xD7, 9, 1152), (0xD8, 9, 1216), (0xD9, 9, 1280), (0xDA, 9, 1344),
        (0xDB, 9, 1408), (0x8, 11, 1792), (0xC, 11, 1856), (0xD, 11, 1920), (0x12, 12, 1984),
        (0x13, 12, 2048), (0x14, 12, 2112), (0x15, 12, 2176), (0x16, 12, 2240), (0x17, 12, 2304),
        (0x1C, 12, 2368), (0x1D, 12, 2432), (0x1E, 12, 2496), (0x1F, 12, 2560),
    };
    private static readonly (int Bits, int Length, int Run)[] BlackRunCodes =
    {
        (0x2, 2, 3), (0x3, 2, 2), (0x2, 3, 1), (0x3, 3, 4), (0x2, 4, 6),
        (0x3, 4, 5), (0x3, 5, 7), (0x4, 6, 9), (0x5, 6, 8), (0x4, 7, 10),
        (0x5, 7, 11), (0x7, 7, 12), (0x4, 8, 13), (0x7, 8, 14), (0x18, 9, 15),
        (0x17, 10, 16), (0x18, 10, 17), (0x37, 10, 0), (0x8, 10, 18), (0xF, 10, 64),
        (0x17, 11, 24), (0x18, 11, 25), (0x28, 11, 23), (0x37, 11, 22), (0x67, 11, 19),
        (0x68, 11, 20), (0x6C, 11, 21), (0x8, 11, 1792), (0xC, 11, 1856), (0xD, 11, 1920),
        (0x12, 12, 1984), (0x13, 12, 2048), (0x14, 12, 2112), (0x15, 12, 2176), (0x16, 12, 2240),
        (0x17, 12, 2304), (0x1C, 12, 2368), (0x1D, 12, 2432), (0x1E, 12, 2496), (0x1F, 12, 2560),
        (0x24, 12, 52), (0x27, 12, 55), (0x28, 12, 56), (0x2B, 12, 59), (0x2C, 12, 60),
        (0x33, 12, 320), (0x34, 12, 384), (0x35, 12, 448), (0x37, 12, 53), (0x38, 12, 54),
        (0x52, 12, 50), (0x53, 12, 51), (0x54, 12, 44), (0x55, 12, 45), (0x56, 12, 46),
        (0x57, 12, 47), (0x58, 12, 57), (0x59, 12, 58), (0x5A, 12, 61), (0x5B, 12, 256),
        (0x64, 12, 48), (0x65, 12, 49), (0x66, 12, 62), (0x67, 12, 63), (0x68, 12, 30),
        (0x69, 12, 31), (0x6A, 12, 32), (0x6B, 12, 33), (0x6C, 12, 40), (0x6D, 12, 41),
        (0xC8, 12, 128), (0xC9, 12, 192), (0xCA, 12, 26), (0xCB, 12, 27), (0xCC, 12, 28),
        (0xCD, 12, 29), (0xD2, 12, 34), (0xD3, 12, 35), (0xD4, 12, 36), (0xD5, 12, 37),
        (0xD6, 12, 38), (0xD7, 12, 39), (0xDA, 12, 42), (0xDB, 12, 43), (0x4A, 13, 640),
        (0x4B, 13, 704), (0x4C, 13, 768), (0x4D, 13, 832), (0x52, 13, 1280), (0x53, 13, 1344),
        (0x54, 13, 1408), (0x55, 13, 1472), (0x5A, 13, 1536), (0x5B, 13, 1600), (0x64, 13, 1664),
        (0x65, 13, 1728), (0x6C, 13, 512), (0x6D, 13, 576), (0x72, 13, 896), (0x73, 13, 960),
        (0x74, 13, 1024), (0x75, 13, 1088), (0x76, 13, 1152), (0x77, 13, 1216),
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
