namespace UglyToad.PdfPig.Tests.Filters
{
    using System.Globalization;
    using System.Text;
    using UglyToad.PdfPig.Fonts;
    using UglyToad.PdfPig.Filters;
    using UglyToad.PdfPig.Filters.CcittFax;
    using UglyToad.PdfPig.Tests.Images;
    using UglyToad.PdfPig.Tokens;

    /// <summary>Tests CCITT pixels, framing, buffer limits and legacy fallback.</summary>
    /// <remarks>
    /// <para>Known patterns and independently decoded PDFBox TIFF hashes check pixels. Direct decoder
    /// calls check fast-path acceptance; filter calls check parameters and legacy recovery. Agreement
    /// with legacy alone does not prove pixel correctness or fast-path success.</para>
    /// <para>Bit strings are MSB-first compressed input; PackBits zero-pads the final byte. Expected
    /// bitmap padding is white. Rows, Height and storage capacity deliberately differ in ending tests.</para>
    /// </remarks>
    public class CcittFaxDecodeFilterTests
    {
        /// <summary>Rejects unsafe dimensions before compressed input is examined or large buffers are allocated.</summary>
        [Theory]
        [InlineData(0, 1)]
        [InlineData(-1, 1)]
        [InlineData(1, 0)]
        [InlineData(1, -1)]
        [InlineData(1728, int.MaxValue)]
        [InlineData(int.MaxValue, 1)]
        [InlineData(int.MaxValue, int.MaxValue)]
        [InlineData(40_000_000, 1)] // Pixel storage fits; legacy transition arrays exceed the budget.
        public void RejectsUnsafeDimensionsBeforeReadingInput(int columns, int rows)
        {
            var parameters = new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                { NameToken.Columns, new NumericToken(columns) },
                { NameToken.Rows, new NumericToken(rows) }
            });
            var dictionary = new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                { NameToken.Filter, NameToken.CcittfaxDecode },
                { NameToken.DecodeParms, parameters }
            });

            // Empty input isolates dimension validation from row decoding and malformed-code recovery.
            var exception = Assert.Throws<CorruptCompressedDataException>(() =>
                new CcittFaxDecodeFilter().Decode(Memory<byte>.Empty, dictionary, TestFilterProvider.Instance, 0));
            if (columns > 0 && rows > 0)
            {
                Assert.StartsWith("CCITT decode buffers require ", exception.Message);
            }
        }

        /// <summary>Applies the allocation limit to both Height and its inline-image abbreviation H.</summary>
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ValidatesTheEffectiveHeight(bool inlineHeight)
        {
            var parameters = new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                { NameToken.Columns, new NumericToken(1728) },
                { NameToken.Rows, new NumericToken(1) }
            });
            var dictionary = new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                { NameToken.Filter, NameToken.CcittfaxDecode },
                { NameToken.DecodeParms, parameters },
                { inlineHeight ? NameToken.H : NameToken.Height, new NumericToken(int.MaxValue) }
            });

            var exception = Assert.Throws<CorruptCompressedDataException>(() =>
                new CcittFaxDecodeFilter().Decode(Memory<byte>.Empty, dictionary, TestFilterProvider.Instance, 0));
            Assert.StartsWith("CCITT decode buffers require ", exception.Message);
        }

        /// <summary>Includes reference/current transition arrays when checking the exact buffer-budget boundary.</summary>
        [Fact]
        public void IncludesChangeArraysAtTheLimit()
        {
            // The shared guard reserves legacy working storage too: two three-entry Int32 arrays
            // and one row byte cost 25 bytes. Exactly reaching the 256 MiB budget is allowed.
            const int lastAllowedRowCount = 256 * 1024 * 1024 - 25;
            Assert.Equal(lastAllowedRowCount, CcittFaxDecodeFilter.GetDecodedBufferSize(1, lastAllowedRowCount));
            Assert.Throws<CorruptCompressedDataException>(() =>
                CcittFaxDecodeFilter.GetDecodedBufferSize(1, lastAllowedRowCount + 1));
        }

        /// <summary>Rounds each row to whole bytes before multiplying by the row count.</summary>
        [Theory]
        [InlineData(1, 1, 1)]
        [InlineData(8, 3, 3)]
        [InlineData(9, 3, 6)]
        [InlineData(1800, 3113, 700425)]
        public void CalculatesRoundedBitmapSize(int columns, int rows, int expected)
        {
            Assert.Equal(expected, CcittFaxDecodeFilter.GetDecodedBufferSize(columns, rows));
        }

        /// <summary>Rejects attacker-controlled image dimensions through page access, including a preceding filter stage.</summary>
        [Theory]
        [InlineData(1728, 2_000_000, false, false)]
        [InlineData(1728, 2_000_000, false, true)]
        [InlineData(1728, 2_000_000, true, false)]
        [InlineData(1728, 2_000_000, true, true)]
        [InlineData(40_000_000, 1, false, false)]
        [InlineData(40_000_000, 1, false, true)]
        [InlineData(40_000_000, 1, true, false)]
        [InlineData(40_000_000, 1, true, true)]
        public void RejectsAllocationBombInPageImage(int columns, int rows, bool filterChain, bool lenient)
        {
            // A single compressed byte must not cause a 432 MB bitmap allocation, or two
            // 160 MB change arrays even though that second image's bitmap is only 5 MB.
            var pdf = CreateAllocationBombPdf(columns, rows, filterChain);
            Assert.InRange(pdf.Length, 1, 1024);

            using var document = PdfDocument.Open(pdf, new ParsingOptions { UseLenientParsing = lenient });
            var image = Assert.Single(document.GetPage(1).GetImages());
            Assert.Equal(filterChain ? 3 : 1, image.RawMemory.Length);

#if NET
            var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
#endif
            var exception = Assert.Throws<CorruptCompressedDataException>(() => image.TryGetBytesAsMemory(out _));
#if NET
            Assert.InRange(GC.GetAllocatedBytesForCurrentThread() - allocatedBefore, 0L, 1024L * 1024);
#endif
            Assert.StartsWith("CCITT decode buffers require ", exception.Message);
        }

        /// <summary>Ensures direct legacy use cannot bypass the bitmap allocation ceiling.</summary>
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void BothFiltersRejectAllocationBombs(bool legacy)
        {
            IFilter filter = legacy ? new LegacyCcittFaxDecodeFilter() : new CcittFaxDecodeFilter();
            Assert.Throws<CorruptCompressedDataException>(() => filter.Decode(new byte[20],
                CreateCcittDictionary(1728, 2_000_000, -1, false),
                TestFilterProvider.Instance,
                0));
        }

        /// <summary>Checks all-white rows in MH, mixed Group 3 and Group 4 against known pixels and legacy bytes.</summary>
        [Theory]
        [InlineData(-1, false)]
        [InlineData(-1, true)]
        [InlineData(0, false)]
        [InlineData(0, true)]
        [InlineData(1, false)]
        [InlineData(1, true)]
        [InlineData(2, false)]
        [InlineData(2, true)]
        public void BitmapCodingFamiliesMatchLegacy(int k, bool blackIsOne)
        {
            const int rows = 160;
            // A white 8-pixel row: G4 vertical-zero, or the T.4 white run code 10011.
            // Mixed G3 uses a 1D tag after its EOL; repeated rows exercise reusable row state.
            var rowBits = k < 0 ? "1" : k == 0 ? "10011" : "000000000001110011";
            var input = PackBits(string.Concat(Enumerable.Repeat(rowBits, rows)));
            var type = k < 0 ? CcittFaxCompressionType.Group4_2D
                : k == 0 ? CcittFaxCompressionType.ModifiedHuffman : CcittFaxCompressionType.Group3_2D;
            var bitmap = new byte[rows];
            Assert.True(CcittFaxBitmapDecoder.TryDecodeBitmap(input, bitmap, 8, rows, type, false, blackIsOne));
            Assert.Equal(Enumerable.Repeat(blackIsOne ? (byte)0 : (byte)255, rows).ToArray(), bitmap);
            var dictionary = CreateCcittDictionary(8, rows, k, blackIsOne);
            var expected = new LegacyCcittFaxDecodeFilter().Decode(input, dictionary, TestFilterProvider.Instance, 0)
                .ToArray();
            Assert.Equal(expected, bitmap);
            Assert.Equal(expected,
                new CcittFaxDecodeFilter().Decode(input, dictionary, TestFilterProvider.Instance, 0).ToArray());
        }

        /// <summary>Checks repeated EOL-framed Group 3 rows against LegacyCcittFaxDecodeFilter.</summary>
        [Fact]
        public void Group3OneDimensionalMatchesLegacy()
        {
            var input = PackBits(string.Concat(Enumerable.Repeat("00000000000110011", 16)));
            var bitmap = new byte[16];
            Assert.True(CcittFaxBitmapDecoder.TryDecodeBitmap(input,
                bitmap,
                8,
                16,
                CcittFaxCompressionType.Group3_1D,
                false,
                false));
            Assert.Equal(Enumerable.Repeat((byte)255, 16).ToArray(), bitmap);
            var dictionary = CreateCcittDictionary(8, 16, 0, false);
            Assert.Equal(new LegacyCcittFaxDecodeFilter().Decode(input, dictionary, TestFilterProvider.Instance, 0)
                    .ToArray(),
                new CcittFaxDecodeFilter().Decode(input, dictionary, TestFilterProvider.Instance, 0).ToArray());
        }

        /// <summary>Checks initial white-zero runs and repeated black reference rows in both output polarities.</summary>
        [Theory]
        [InlineData(-1, false)]
        [InlineData(-1, true)]
        [InlineData(0, false)]
        [InlineData(0, true)]
        public void BlackRunsMatchLegacy(int k, bool blackIsOne)
        {
            // T.4 white zero = 00110101; black eight = 000101.
            // The first G4 row is horizontal; following identical rows use two vertical-zero modes.
            var bits = k < 0
                ? "00100110101000101" + string.Concat(Enumerable.Repeat("11", 159))
                : string.Concat(Enumerable.Repeat("00110101000101", 160));
            var input = PackBits(bits);
            var bitmap = new byte[160];
            Assert.True(CcittFaxBitmapDecoder.TryDecodeBitmap(input,
                bitmap,
                8,
                160,
                k < 0 ? CcittFaxCompressionType.Group4_2D : CcittFaxCompressionType.ModifiedHuffman,
                false,
                blackIsOne));
            Assert.Equal(Enumerable.Repeat(blackIsOne ? (byte)255 : (byte)0, 160).ToArray(), bitmap);
            var dictionary = CreateCcittDictionary(8, 160, k, blackIsOne);
            Assert.Equal(new LegacyCcittFaxDecodeFilter().Decode(input, dictionary, TestFilterProvider.Instance, 0)
                    .ToArray(),
                bitmap);
            Assert.Equal(bitmap,
                new CcittFaxDecodeFilter().Decode(input, dictionary, TestFilterProvider.Instance, 0).ToArray());
        }

        /// <summary>Checks row-byte alignment and white padding for a nine-pixel bitmap row.</summary>
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ByteAlignedRowsAndUnusedPixelBitsMatchLegacy(bool blackIsOne)
        {
            // Nine pixels: eight white (10011), one black (010). This row is exactly one
            // encoded byte, but two decoded bytes; its seven unused pixel bits remain white.
            var input = PackBits(string.Concat(Enumerable.Repeat("10011010", 160)));
            var bitmap = new byte[320];
            Assert.True(CcittFaxBitmapDecoder.TryDecodeBitmap(input,
                bitmap,
                9,
                160,
                CcittFaxCompressionType.ModifiedHuffman,
                true,
                blackIsOne));
            var expectedRow = blackIsOne ? new byte[] { 0, 128 } : new byte[] { 255, 127 };
            Assert.Equal(Enumerable.Range(0, 160).SelectMany(_ => expectedRow).ToArray(), bitmap);
            var dictionary = CreateCcittDictionary(9, 160, 0, blackIsOne, true);
            Assert.Equal(new LegacyCcittFaxDecodeFilter().Decode(input, dictionary, TestFilterProvider.Instance, 0)
                    .ToArray(),
                bitmap);
            Assert.Equal(bitmap,
                new CcittFaxDecodeFilter().Decode(input, dictionary, TestFilterProvider.Instance, 0).ToArray());
        }

        /// <summary>Checks the complete real Group 4 fixture directly and through the filter against legacy pixels.</summary>
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void RealGroup4FixtureMatchesLegacy(bool blackIsOne)
        {
            var fixture = UglyToad.PdfPig.Tests.Writer.TestImages.CcittG4TestImage.Load();
            var bitmap = new byte[(fixture.Width + 7) / 8 * fixture.Height];
            Assert.True(CcittFaxBitmapDecoder.TryDecodeBitmap(fixture.RawCcittData,
                bitmap,
                fixture.Width,
                fixture.Height,
                CcittFaxCompressionType.Group4_2D,
                false,
                blackIsOne));
            var dictionary = CreateCcittDictionary(fixture.Width, fixture.Height, -1, blackIsOne);
            var expected = new LegacyCcittFaxDecodeFilter()
                .Decode(fixture.RawCcittData, dictionary, TestFilterProvider.Instance, 0).ToArray();
            Assert.Equal(expected, bitmap);
            Assert.Equal(expected,
                new CcittFaxDecodeFilter().Decode(fixture.RawCcittData, dictionary, TestFilterProvider.Instance, 0)
                    .ToArray());
        }

        /// <summary>Compares the stored compressed fixture with its expected decoded bitmap, including row padding.</summary>
        [Fact]
        public void CanDecodeCCittFaxCompressedImageData()
        {
            var encodedBytes = ImageHelpers.LoadFileBytes("ccittfax-encoded.bin");

            var filter = new CcittFaxDecodeFilter();
            var dictionary = new Dictionary<NameToken, IToken>
            {
                { NameToken.D, new ArrayToken(new[] { new NumericToken(1), new NumericToken(0) }) },
                { NameToken.W, new NumericToken(1800) },
                { NameToken.H, new NumericToken(3113) },
                { NameToken.Bpc, new NumericToken(1) },
                { NameToken.F, NameToken.CcittfaxDecode },
                {
                    NameToken.DecodeParms,
                    new DictionaryToken(new Dictionary<NameToken, IToken>
                    {
                        { NameToken.K, new NumericToken(-1) },
                        { NameToken.Columns, new NumericToken(1800) },
                        { NameToken.Rows, new NumericToken(3113) },
                        { NameToken.BlackIs1, BooleanToken.True }
                    })
                }
            };

            var expectedBytes = ImageHelpers.LoadFileBytes("ccittfax-decoded.bin");
            var decodedBytes = filter.Decode(encodedBytes,
                new DictionaryToken(dictionary),
                TestFilterProvider.Instance,
                0);
            Assert.Equal(expectedBytes, decodedBytes.ToArray());
        }

        /// <summary>Verifies fast rejection followed by a fresh legacy attempt without exposing partial fast pixels.</summary>
        [Fact]
        public void RejectedBitmapInputIsRetriedByLegacy()
        {
            var input = new byte[20];
            Assert.False(CcittFaxBitmapDecoder.TryDecodeBitmap(input,
                new byte[2],
                8,
                2,
                CcittFaxCompressionType.Group4_2D,
                false,
                false));
            var dictionary = CreateCcittDictionary(8, 2, -1, false);
            Assert.Equal(new LegacyCcittFaxDecodeFilter().Decode(input, dictionary, TestFilterProvider.Instance, 0)
                    .ToArray(),
                new CcittFaxDecodeFilter().Decode(input, dictionary, TestFilterProvider.Instance, 0).ToArray());
        }

        /// <summary>Preserves the legacy raster for rows wider than compact UInt16 transition storage permits.</summary>
        [Fact]
        public void WideRowsUseLegacy()
        {
            var input = Enumerable.Repeat((byte)255, 20).ToArray();
            var dictionary = CreateCcittDictionary(70_000, 1, -1, false);
            Assert.Equal(new LegacyCcittFaxDecodeFilter().Decode(input, dictionary, TestFilterProvider.Instance, 0)
                    .ToArray(),
                new CcittFaxDecodeFilter().Decode(input, dictionary, TestFilterProvider.Instance, 0).ToArray());
        }

        /// <summary>Rejects caller buffers that do not match the legacy raster dimensions before reuse.</summary>
        [Theory]
        [InlineData(0)]
        [InlineData(2)]
        public void LegacyReuseRejectsAnIncorrectBitmapSize(int bitmapByteCount)
        {
            var dictionary = CreateCcittDictionary(8, 1, -1, false);
            var exception = Assert.Throws<ArgumentException>(() =>
                new LegacyCcittFaxDecodeFilter().DecodeInto(new byte[20],
                    dictionary,
                    TestFilterProvider.Instance,
                    0,
                    new byte[bitmapByteCount]));
            Assert.Equal("bitmap", exception.ParamName);
        }

        /// <summary>Ensures an unsuccessful EOL probe leaves ordinary row data available to the code reader.</summary>
        [Fact]
        public void FailedEndOfLineProbePreservesTheNextCode()
        {
            var reader = new CcittFaxBitReader(new byte[] { 0b10101010 });
            Assert.False(reader.TryReadEndOfLine());
            Assert.Equal(0b10101010, reader.ReadBitsExact(8));
        }

        /// <summary>Consumes EOL zero fill and the marker without consuming the following image code.</summary>
        [Fact]
        public void EndOfLineProbeConsumesLeadingFillButPreservesTheFollowingCode()
        {
            // Fifteen zero bits and one set bit form an EOL with four leading fill bits.
            var reader = new CcittFaxBitReader(PackBits("000000000000000110101010"));
            Assert.True(reader.TryReadEndOfLine());
            Assert.Equal(0b10101010, reader.ReadBitsExact(8));
        }

        /// <summary>Retains legacy row-alignment policy while preserving the next encoded byte.</summary>
        [Fact]
        public void ByteAlignmentDiscardsNonzeroPaddingWithoutReadingTheNextByte()
        {
            // EncodedByteAlign preserves the legacy policy: padding is skipped, not checked.
            var reader = new CcittFaxBitReader(new byte[] { 0b10111111, 0x35 });
            Assert.Equal(0b101, reader.ReadBitsExact(3));
            reader.AlignToByteBoundary();
            Assert.Equal(0x35, reader.ReadBitsExact(8));
        }

        // Framing vectors use an eight-pixel white row: MH run code 10011, or G4 vertical-zero 1.
        // EOL is eleven zeros followed by one. A complete RTC is six EOLs; G4 EOFB is two.
        // Mixed Group 3 appends a 1D/2D tag to each row EOL; every RTC tag must be 1 (1D).
        // Short-marker tests validate a complete row first; their all-zero tails are a local tolerance.
        private const string Eol = "000000000001";
        private const string WhiteEightPixels = "10011";

        /// <summary>Accepts two-to-five-EOL Group 3 endings only after intact rows and an exclusively zero trailer.</summary>
        [Theory]
        [InlineData(1, 2)]
        [InlineData(1, 3)]
        [InlineData(1, 4)]
        [InlineData(1, 5)]
        [InlineData(0, 2)]
        [InlineData(0, 5)]
        public void ShortRtcRequiresCompleteRowsAndZeroTail(int declaredRows, int eolCount)
        {
            var bits = Eol + WhiteEightPixels + string.Concat(Enumerable.Repeat(Eol, eolCount)) + "00000000";
            Assert.True(TryDecodeFramed(bits,
                CcittFaxCompressionType.Group3_1D,
                declaredRows,
                out var bitmap,
                out var completedRows,
                out var consumed,
                out var status));
            Assert.Equal(1, completedRows);
            Assert.Equal(255, bitmap[0]);
            Assert.Equal(PackBits(bits).Length, consumed);
            Assert.Equal(CcittFaxDecodeStatus.IncompleteEndOfBlock, status);
        }

        /// <summary>Rejects mismatched Rows, unauthorized single EOL and nonzero short-marker trailers.</summary>
        [Theory]
        [InlineData(0, 1, false)]
        [InlineData(2, 2, false)]
        [InlineData(1, 5, true)]
        public void InvalidRtcDoesNotEstablishAnInputBoundary(int declaredRows, int eolCount, bool nonzeroTail)
        {
            var bits = Eol + WhiteEightPixels + string.Concat(Enumerable.Repeat(Eol, eolCount))
                       + (nonzeroTail ? "1" : "00000000");
            Assert.False(TryDecodeFramed(bits,
                CcittFaxCompressionType.Group3_1D,
                declaredRows,
                out var bitmap,
                out _,
                out var consumed,
                out var status));
            Assert.All(bitmap, value => Assert.Equal(0, value));
            Assert.Equal(0, consumed);
            Assert.Equal(CcittFaxDecodeStatus.NotCompleted, status);
        }

        /// <summary>Requires exact positive Rows before accepting one EOL or missing markers with whole-byte zero fill.</summary>
        [Theory]
        [InlineData(false, 0)]
        [InlineData(false, 1)]
        [InlineData(true, 0)]
        [InlineData(true, 1)]
        public void ExactPositiveRowsAuthorizeSingleEolOrMissingMarker(bool group4, int eolCount)
        {
            var bits = (group4 ? "1" : Eol + WhiteEightPixels)
                       + string.Concat(Enumerable.Repeat(Eol, eolCount)) + "0000000000000000";
            Assert.True(TryDecodeFramed(bits,
                group4 ? CcittFaxCompressionType.Group4_2D : CcittFaxCompressionType.Group3_1D,
                1,
                out _,
                out var completedRows,
                out var consumed,
                out var status));
            Assert.Equal(1, completedRows);
            Assert.Equal(PackBits(bits).Length, consumed);
            Assert.Equal(eolCount == 0 ? CcittFaxDecodeStatus.EndOfInput : CcittFaxDecodeStatus.IncompleteEndOfBlock,
                status);
        }

        /// <summary>Distinguishes final-byte zero padding from an unauthorized additional whole zero byte.</summary>
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void UnknownRowsAcceptOnlyFinalBytePadding(bool extraZeroByte)
        {
            var bits = "1" + (extraZeroByte ? new string('0', 15) : new string('0', 7));
            Assert.Equal(!extraZeroByte,
                TryDecodeFramed(bits,
                    CcittFaxCompressionType.Group4_2D,
                    0,
                    out _,
                    out _,
                    out _,
                    out var status));
            Assert.Equal(extraZeroByte ? CcittFaxDecodeStatus.NotCompleted : CcittFaxDecodeStatus.EndOfInput, status);
        }

        /// <summary>Requires every tag in a complete mixed Group 3 RTC to select one-dimensional coding.</summary>
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void MixedRtcRequiresOneDimensionalTags(bool wrongTag)
        {
            var rtc = string.Concat(Enumerable.Repeat(Eol + "1", 6));
            if (wrongTag)
                rtc = Eol + "0" + string.Concat(Enumerable.Repeat(Eol + "1", 5));
            Assert.Equal(!wrongTag,
                TryDecodeFramed(Eol + "1" + WhiteEightPixels + rtc,
                    CcittFaxCompressionType.Group3_2D,
                    1,
                    out _,
                    out _,
                    out _,
                    out var status));
            Assert.Equal(wrongTag ? CcittFaxDecodeStatus.NotCompleted : CcittFaxDecodeStatus.EndOfBlock, status);
        }

        /// <summary>Accepts optional row EOL without confusing it with a following complete block marker.</summary>
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void OptionalEolIsAcceptedForMhAndGroup4(bool group4)
        {
            var bits = Eol + (group4 ? "1" : WhiteEightPixels)
                           + string.Concat(Enumerable.Repeat(Eol, group4 ? 2 : 6));
            Assert.True(TryDecodeFramed(bits,
                group4 ? CcittFaxCompressionType.Group4_2D : CcittFaxCompressionType.ModifiedHuffman,
                1,
                out _,
                out var completedRows,
                out _,
                out var status));
            Assert.Equal(1, completedRows);
            Assert.Equal(CcittFaxDecodeStatus.EndOfBlock, status);
        }

        /// <summary>Stops at a complete block marker even when Rows and Height overstate the encoded image.</summary>
        [Fact]
        public void EarlyFullMarkerStopsBeforeRowsAndExcludesFollowingBytes()
        {
            var dictionary = CreateFramingDictionary(4, 4, -1, true, false);
            // Only one row belongs to the image. The following bytes represent enclosing syntax.
            var input = PackBits("1" + Eol + Eol).Concat(new byte[] { 0x45, 0x49, 0x20 }).ToArray();
            var output = new CcittFaxDecodeFilter().DecodeWithConsumption(input,
                dictionary,
                TestFilterProvider.Instance,
                0,
                out var consumed,
                out var status);
            Assert.Equal(new byte[] { 255 }, output.ToArray());
            Assert.Equal(4, consumed);
            Assert.Equal(CcittFaxDecodeStatus.EndOfBlock, status);
        }

        /// <summary>Checks that image cropping does not change encoded row counting or the verified stopping condition.</summary>
        [Theory]
        [InlineData(true, 1, 1, 2, "EndOfBlock")]
        [InlineData(false, 2, 1, 2, "RowLimit")]
        [InlineData(false, 0, 1, 2, "EndOfInput")]
        [InlineData(true, 0, 0, 2, "EndOfBlock")]
        public void FilterSeparatesStoredHeightFromEncodedRows(bool endOfBlock,
            int rows,
            int height,
            int encodedRows,
            string expectedStatus)
        {
            var bits = new string('1', encodedRows) + (endOfBlock ? Eol + Eol : "");
            var output = new CcittFaxDecodeFilter().DecodeWithConsumption(PackBits(bits),
                CreateFramingDictionary(rows, height, -1, endOfBlock, false),
                TestFilterProvider.Instance,
                0,
                out var consumed,
                out var status);
            Assert.Equal(height > 0 ? Math.Min(height, encodedRows) : encodedRows, output.Length);
            Assert.All(output.ToArray(), value => Assert.Equal(255, value));
            Assert.Equal(PackBits(bits).Length, consumed);
            Assert.Equal(expectedStatus, status.ToString());
        }

        /// <summary>Updates reference transitions for discarded rows so later 2D rows remain decodable.</summary>
        [Fact]
        public void ExtraTwoDimensionalRowsRetainTheirReferenceState()
        {
            // White first row; horizontal white-0/black-8 second row; two vertical-zero
            // transitions repeat the black row. Only the first row is retained for Height=1.
            var bits = "1" + "001" + "00110101" + "000101" + "11" + Eol + Eol;
            Assert.True(TryDecodeFramed(bits,
                CcittFaxCompressionType.Group4_2D,
                1,
                out _,
                out var completedRows,
                out _,
                out var status));
            Assert.Equal(3, completedRows);
            Assert.Equal(CcittFaxDecodeStatus.EndOfBlock, status);
            var output = new CcittFaxDecodeFilter().DecodeWithConsumption(PackBits(bits),
                CreateFramingDictionary(1, 1, -1, true, false),
                TestFilterProvider.Instance,
                0,
                out _,
                out status);
            Assert.Equal(new byte[] { 255 }, output.ToArray());
            Assert.Equal(CcittFaxDecodeStatus.EndOfBlock, status);
        }

        /// <summary>Honors explicit PDF row framing when the stream is shorter than the header-detection window.</summary>
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ExplicitEndOfLineControlsShortHeaderDetection(bool required)
        {
            var bits = Eol + WhiteEightPixels + string.Concat(Enumerable.Repeat(Eol, 6));
            var output = new CcittFaxDecodeFilter().DecodeWithConsumption(PackBits(bits),
                CreateFramingDictionary(1, 1, 0, true, required),
                TestFilterProvider.Instance,
                0,
                out _,
                out var status);
            Assert.Equal(new byte[] { 255 }, output.ToArray());
            Assert.Equal(CcittFaxDecodeStatus.EndOfBlock, status);
        }

        /// <summary>Rejects absent row EOL when explicitly required even though ordinary Group 4 needs none.</summary>
        [Fact]
        public void RequiredGroup4EolCannotBeOmitted()
        {
            var bitmap = new byte[1];
            var options = new CcittFaxDecodeOptions(8,
                1,
                CcittFaxCompressionType.Group4_2D,
                false,
                false,
                acceptOptionalEndOfLine: true,
                endOfBlock: true,
                requireEndOfLine: true,
                maximumAllocatedBufferBytes: 1024);
            Assert.False(CcittFaxBitmapDecoder.TryDecodeBitmap(PackBits("1" + Eol + Eol),
                ref bitmap,
                in options,
                out var result));
            Assert.Equal(0, result.BytesConsumed);
            Assert.Equal(CcittFaxDecodeStatus.NotCompleted, result.Status);
        }

        /// <summary>Keeps boundary status unverified when legacy returns pixels after a rejected fast attempt.</summary>
        [Fact]
        public void FailedFastAttemptDoesNotInventLegacyConsumption()
        {
            var output = new CcittFaxDecodeFilter().DecodeWithConsumption(new byte[20],
                CreateCcittDictionary(8, 2, -1, false),
                TestFilterProvider.Instance,
                0,
                out var consumed,
                out var status);
            Assert.Equal(2, output.Length);
            Assert.Equal(0, consumed);
            Assert.Equal(CcittFaxDecodeStatus.NotCompleted, status);
        }

        /// <summary>Bounds both cumulative resize allocations and logical decoded bytes discarded beyond image Height.</summary>
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void GrowthAndDiscardedRowsRemainBudgeted(bool discardRows)
        {
            // All-white G4 rows. A deliberately tiny test budget admits initial state,
            // but cannot admit all cumulative growth allocations or all logical scratch rows.
            var bitmap = new byte[1];
            var options = new CcittFaxDecodeOptions(8,
                1,
                CcittFaxCompressionType.Group4_2D,
                false,
                false,
                acceptOptionalEndOfLine: true,
                endOfBlock: true,
                requireEndOfLine: false,
                maximumAllocatedBufferBytes: discardRows ? 42 : 44,
                maximumStoredRows: discardRows ? 1 : 0);
            Assert.Throws<CorruptCompressedDataException>(() => CcittFaxBitmapDecoder.TryDecodeBitmap(
                PackBits(new string('1', discardRows ? 43 : 5) + Eol + Eol),
                ref bitmap,
                in options,
                out _));
        }

        /// <summary>Checks PDFBox's CCITT TIFF strips against independently decoded pixel hashes.</summary>
        /// <remarks>Adapted from Apache PDFBox 3.0.8 CCITTFactoryTest's single/multi-image cases.
        /// Unchanged compressed strips are embedded below. SHA-256 references were generated
        /// with Pillow 12.3.0/libtiff, mode 1 (white=1, MSB-first, padded rows), independently
        /// of PdfPig. Group 3 strips stop at Rows without RTC; Group 4 uses its block end.</remarks>
        [Theory]
        [InlineData(0, PdfBoxGroup3, "3703A34E33947B4426A41AEAB8CD405420D1C5BA4911FA83B1CC41AF40A9ADED")]
        [InlineData(-1, PdfBoxGroup4, "3703A34E33947B4426A41AEAB8CD405420D1C5BA4911FA83B1CC41AF40A9ADED")]
        [InlineData(-1, PdfBoxMultiPage0, "08E2ABD03263C1076BE0EFE454937F5035AA8C1DB2442E21ABD57623D5D8F856")]
        [InlineData(-1, PdfBoxMultiPage1, "2861E68D2CF43CDF11DB188C9E0315FABFEF88D1BE58BAE00F2D96DFE17BBE7C")]
        [InlineData(-1, PdfBoxMultiPage2, "5D6C75C856CDCAD8204E1A4530EC14316AA932B0D3C7DDBE73638D80596A45E0")]
        public void PdfBoxTiffStripsMatchIndependentPixels(int k, string encoded, string expectedHash)
        {
            foreach (bool blackIsOne in new[] { false, true })
            {
                var input = Convert.FromBase64String(encoded);
                var actual = new CcittFaxDecodeFilter().Decode(input,
                    CreateTestDictionary(344, 287, 287, k, k < 0, k == 0, blackIsOne),
                    TestFilterProvider.Instance,
                    0).ToArray();
                Assert.Equal(43 * 287, actual.Length);
                if (blackIsOne)
                {
                    for (int i = 0; i < actual.Length; i++)
                    {
                        actual[i] = (byte)~actual[i];
                    }
                }

                using var hash = System.Security.Cryptography.SHA256.Create();
                Assert.Equal(expectedHash, BitConverter.ToString(hash.ComputeHash(actual)).Replace("-", ""));
            }
        }

        /// <summary>Checks PDFBox's 343x287 alternating-pixel pattern, including white row padding.</summary>
        /// <remarks>Adapted from CCITTFactoryTest.testCreateFromBufferedChessImage. Black pixels
        /// have even x+y parity. A small MH encoder uses only fixed zero/one-pixel codewords;
        /// expected pixels are painted independently. Both polarities include the unused last bit.</remarks>
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void PdfBoxChessboardPreservesPixelsAndWhitePadding(bool blackIsOne)
        {
            const int columns = 343;
            const int rows = 287;
            var bits = new StringBuilder();
            var expected = Enumerable.Repeat(blackIsOne ? (byte)0 : (byte)255, 43 * rows).ToArray();
            for (int y = 0; y < rows; y++)
            {
                if ((y & 1) == 0)
                {
                    bits.Append("00110101"); // White zero before the first black pixel.
                }

                for (int x = 0; x < columns; x++)
                {
                    bool black = ((x + y) & 1) == 0;
                    bits.Append(black ? "010" : "000111");
                    if (black)
                    {
                        int index = y * 43 + x / 8;
                        byte mask = (byte)(128 >> (x & 7));
                        expected[index] = blackIsOne ? (byte)(expected[index] | mask) : (byte)(expected[index] & ~mask);
                    }
                }
            }

            var input = PackBits(bits.ToString());
            var bitmap = new byte[expected.Length];
            Assert.True(CcittFaxBitmapDecoder.TryDecodeBitmap(input,
                bitmap,
                columns,
                rows,
                CcittFaxCompressionType.ModifiedHuffman,
                false,
                blackIsOne));
            Assert.Equal(expected, bitmap);
            Assert.Equal(expected,
                new CcittFaxDecodeFilter().Decode(input,
                    CreateTestDictionary(columns, rows, rows, 0, false, false, blackIsOne),
                    TestFilterProvider.Instance,
                    0).ToArray());
        }

        /// <summary>Exercises long makeup sequences and the black table's thirteen-bit codes.</summary>
        /// <remarks>Expected pixels use a known white prefix followed by black pixels, independently
        /// of either decoder. Cases include 2048-pixel runs and white-zero/black-512.</remarks>
        [Theory]
        [InlineData("00000010000011010011", 64, 29)]
        [InlineData("000000010011001101010000000100110000110111", 4096, 2048)]
        [InlineData("0011010100000011011000000110111", 512, 0)]
        public void LongHuffmanCodesMatchIndependentPixels(string bits, int columns, int whitePixels)
        {
            foreach (bool blackIsOne in new[] { false, true })
            {
                var expected = CreateWhiteThenBlackPixels(columns, whitePixels, blackIsOne);
                var actual = new byte[expected.Length];
                Assert.True(CcittFaxBitmapDecoder.TryDecodeBitmap(PackBits(bits),
                    actual,
                    columns,
                    1,
                    CcittFaxCompressionType.ModifiedHuffman,
                    false,
                    blackIsOne));
                Assert.Equal(expected, actual);
            }
        }

        /// <summary>Checks all small white/black run combinations against independently painted pixels.</summary>
        /// <remarks>Fixed standard codes avoid production lookup builders. Zero white runs, final
        /// single runs, short pairs, partial bytes and adjacent rows exercise MH and Group 3,
        /// with and without byte alignment. Each run length ranges from zero to eight.</remarks>
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ShortHuffmanRunsMatchIndependentPixels(bool blackIsOne)
        {
            string[] white = { "00110101", "000111", "0111", "1000", "1011", "1100", "1110", "1111", "10011" };
            string[] black = { "0000110111", "010", "11", "10", "011", "0011", "0010", "00011", "000101" };
            foreach (bool aligned in new[] { false, true })
            foreach (bool group3 in new[] { false, true })
                for (int w = 0; w <= 8; w++)
                for (int b = 0; b <= 8; b++)
                {
                    if (w + b == 0)
                    {
                        continue;
                    }

                    string row = (group3 ? "0000" + Eol : "") + white[w] + (b > 0 ? black[b] : "");
                    if (aligned)
                    {
                        row = row.PadRight((row.Length + 7) / 8 * 8, '0');
                    }

                    var input = PackBits(row + row);
                    var expected = CreateWhiteThenBlackPixels(w + b, w, blackIsOne);
                    expected = expected.Concat(expected).ToArray();
                    var actual = new byte[expected.Length];
                    Assert.True(CcittFaxBitmapDecoder.TryDecodeBitmap(input,
                        actual,
                        w + b,
                        2,
                        group3 ? CcittFaxCompressionType.Group3_1D : CcittFaxCompressionType.ModifiedHuffman,
                        aligned,
                        blackIsOne));
                    Assert.Equal(expected, actual);
                    Assert.Equal(expected,
                        new CcittFaxDecodeFilter().Decode(input,
                            CreateTestDictionary(w + b, 2, 2, 0, false, group3, blackIsOne, aligned),
                            TestFilterProvider.Instance,
                            0).ToArray());
                }
        }

        /// <summary>Checks horizontal, vertical-zero and pass in Group 4 and mixed Group 3.</summary>
        /// <remarks>Rows are WWWBBBBB, WWWBBBBB, WWWWWWWW, WWWWWWWW. Group 4 uses horizontal
        /// white-3/black-5, repeats its edges with vertical-zero, then passes the black interval.
        /// Mixed Group 3 alternates tagged 1D/2D rows. Expected bytes are fixed pixels.</remarks>
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void TwoDimensionalOperationsMatchIndependentPixels(bool blackIsOne)
        {
            foreach (bool mixed in new[] { false, true })
            {
                string bits = mixed
                    ? Eol + "1" + "10000011" + Eol + "0" + "11" + Eol + "1" + WhiteEightPixels + Eol + "0" + "1"
                    : "00110000011" + "11" + "0001" + "1";
                var actual = new byte[4];
                Assert.True(CcittFaxBitmapDecoder.TryDecodeBitmap(PackBits(bits),
                    actual,
                    8,
                    4,
                    mixed ? CcittFaxCompressionType.Group3_2D : CcittFaxCompressionType.Group4_2D,
                    false,
                    blackIsOne));
                Assert.Equal(blackIsOne ? new byte[] { 31, 31, 0, 0 } : new byte[] { 224, 224, 255, 255 }, actual);
            }
        }

        /// <summary>Checks every signed vertical offset against a known reference edge.</summary>
        /// <remarks>First row: white-4/black-4. Second row shifts the first edge by -3..+3;
        /// vertical-zero ends black at width 8. This verifies sign, reference parity and color.</remarks>
        [Theory]
        [InlineData(-3, "0000010")]
        [InlineData(-2, "000010")]
        [InlineData(-1, "010")]
        [InlineData(0, "1")]
        [InlineData(1, "011")]
        [InlineData(2, "000011")]
        [InlineData(3, "0000011")]
        public void VerticalOffsetsMatchIndependentPixels(int offset, string mode)
        {
            foreach (bool blackIsOne in new[] { false, true })
            {
                var actual = new byte[2];
                Assert.True(CcittFaxBitmapDecoder.TryDecodeBitmap(PackBits("0011011011" + mode + "1"),
                    actual,
                    8,
                    2,
                    CcittFaxCompressionType.Group4_2D,
                    false,
                    blackIsOne));
                Assert.Equal(CreateWhiteThenBlackPixels(8, 4, blackIsOne)
                        .Concat(CreateWhiteThenBlackPixels(8, 4 + offset, blackIsOne)).ToArray(),
                    actual);
            }
        }

        /// <summary>Accepts 4096 zero-fill bits before Group 3 EOL without losing the next row code.</summary>
        [Fact]
        public void LongEndOfLineFillPreservesTheRowCode()
        {
            var actual = new byte[] { 0xAA };
            Assert.True(CcittFaxBitmapDecoder.TryDecodeBitmap(PackBits(new string('0', 4096) + Eol + WhiteEightPixels),
                actual,
                8,
                1,
                CcittFaxCompressionType.Group3_1D,
                false,
                true));
            Assert.Equal(new byte[] { 0 }, actual);
        }

        /// <summary>Preserves prefetched next-row bits when alignment skips the current row's padding.</summary>
        /// <remarks>White-1/black-1/white-6 takes thirteen bits; the next row is white-eight.
        /// Its code can already be buffered before the first row's padding is discarded.</remarks>
        [Fact]
        public void ByteAlignmentRetainsPrefetchedNextRow()
        {
            var actual = new byte[2];
            Assert.True(CcittFaxBitmapDecoder.TryDecodeBitmap(new byte[] { 0x1D, 0x70, 0x98 },
                actual,
                8,
                2,
                CcittFaxCompressionType.ModifiedHuffman,
                true,
                true));
            Assert.Equal(new byte[] { 0x40, 0 }, actual);
        }

        /// <summary>Checks slice offsets, input immutability and white padding at varied widths.</summary>
        /// <remarks>Sentinel bytes surround four vertical-zero codes for all-white Group 4 rows.
        /// Bytes outside the supplied slice must neither be interpreted nor modified.</remarks>
        [Theory]
        [InlineData(1)]
        [InlineData(7)]
        [InlineData(8)]
        [InlineData(9)]
        [InlineData(31)]
        [InlineData(32)]
        [InlineData(33)]
        [InlineData(1800)]
        public void SlicedInputPreservesSentinelsAndWhitePadding(int columns)
        {
            var input = new byte[] { 0xAA, 0xF0, 0x55 };
            foreach (bool blackIsOne in new[] { false, true })
            {
                var actual = new byte[(columns + 7) / 8 * 4];
                Assert.True(CcittFaxBitmapDecoder.TryDecodeBitmap(input.AsSpan(1, 1),
                    actual,
                    columns,
                    4,
                    CcittFaxCompressionType.Group4_2D,
                    false,
                    blackIsOne));
                Assert.Equal(Enumerable.Repeat(blackIsOne ? (byte)0 : (byte)255, actual.Length).ToArray(), actual);
                Assert.Equal(actual,
                    new CcittFaxDecodeFilter().Decode(input.AsMemory(1, 1),
                        CreateTestDictionary(columns, 4, 4, -1, false, false, blackIsOne),
                        TestFilterProvider.Instance,
                        0).ToArray());
                Assert.Equal(new byte[] { 0xAA, 0xF0, 0x55 }, input);
            }
        }

        /// <summary>Checks identical success, rejection and pixels for whole and sliced short inputs.</summary>
        /// <remarks>Seeded samples exercise reservoirs and invalid prefixes in every coding family.
        /// This establishes offset invariance, not independent pixel correctness. Rejected attempts
        /// must clear their entire bitmap, including earlier rows, before any legacy retry.</remarks>
        [Theory]
        [InlineData("ModifiedHuffman")]
        [InlineData("Group3_1D")]
        [InlineData("Group3_2D")]
        [InlineData("Group4_2D")]
        public void WholeAndSlicedShortInputsHaveIdenticalResults(string compression)
        {
            var type = (CcittFaxCompressionType)Enum.Parse(typeof(CcittFaxCompressionType), compression);
            var random = new Random(1435);
            foreach (int columns in new[] { 1, 7, 8, 9, 16, 31, 64 })
            foreach (bool aligned in new[] { false, true })
            foreach (bool blackIsOne in new[] { false, true })
                for (int sample = 0; sample < 32; sample++)
                {
                    var input = new byte[random.Next(0, 49)];
                    random.NextBytes(input);
                    var surrounded = Enumerable.Repeat((byte)0xAA, input.Length + 10).ToArray();
                    input.CopyTo(surrounded, 5);
                    var before = surrounded.ToArray();
                    var whole = Enumerable.Repeat((byte)0xAA, (columns + 7) / 8 * 4).ToArray();
                    var sliced = whole.ToArray();
                    bool success =
                        CcittFaxBitmapDecoder.TryDecodeBitmap(input, whole, columns, 4, type, aligned, blackIsOne);
                    Assert.Equal(success,
                        CcittFaxBitmapDecoder.TryDecodeBitmap(surrounded.AsSpan(5, input.Length),
                            sliced,
                            columns,
                            4,
                            type,
                            aligned,
                            blackIsOne));
                    Assert.Equal(whole, sliced);
                    Assert.Equal(before, surrounded);
                    if (!success)
                    {
                        Assert.All(whole, value => Assert.Equal(0, value));
                    }
                }
        }

        /// <summary>Rejects malformed rows and preserves the retained legacy recovery contract.</summary>
        /// <remarks>Cases cover oversized runs, unsupported 2D modes, negative vertical positions,
        /// invalid Huffman prefixes and excessive zero-length runs. The fast decoder clears
        /// partial output on rejection, then the filter restarts from the original input. Legacy may
        /// return pixels or throw; this test preserves its bytes and exception types.</remarks>
        [Theory]
        [InlineData("10101000", 8, 0)]
        [InlineData("0000000010000000", 8, 0)]
        [InlineData("0000000010000000", 8, -1)]
        [InlineData("00000101", 1, -1)]
        [InlineData("001101010000110111001101010000110111001101010000110111001101010000110111", 1, 0)]
        public void MalformedRowsRejectFastAndMatchLegacyRetry(string bits, int columns, int k)
        {
            var input = PackBits(bits);
            var bitmap = new byte[] { 0xAA };
            Assert.False(CcittFaxBitmapDecoder.TryDecodeBitmap(input,
                bitmap,
                columns,
                1,
                k < 0 ? CcittFaxCompressionType.Group4_2D : CcittFaxCompressionType.ModifiedHuffman,
                false,
                true));
            Assert.Equal(new byte[] { 0 }, bitmap);
            AssertFilterMatchesLegacy(input, CreateTestDictionary(columns, 1, 1, k, false, false, true), true);
        }

        /// <summary>Distinguishes valid row-boundary EOF from malformed truncation in a real Group 4 image.</summary>
        /// <remarks>Clean EOF returns the original pixel prefix; truncation inside a code retries legacy.
        /// Legacy pads to the requested height, so only rejected fast attempts are compared with it.</remarks>
        [Fact]
        public void TruncatedFixtureDistinguishesCleanEofFromLegacyRetry()
        {
            var fixture = UglyToad.PdfPig.Tests.Writer.TestImages.CcittG4TestImage.Load();
            int stride = (fixture.Width + 7) / 8;
            var full = new byte[stride * fixture.Height];
            Assert.True(CcittFaxBitmapDecoder.TryDecodeBitmap(fixture.RawCcittData,
                full,
                fixture.Width,
                fixture.Height,
                CcittFaxCompressionType.Group4_2D,
                false,
                true));
            var dictionary =
                CreateTestDictionary(fixture.Width, fixture.Height, fixture.Height, -1, false, false, true);
            var options = new CcittFaxDecodeOptions(fixture.Width,
                fixture.Height,
                CcittFaxCompressionType.Group4_2D,
                false,
                true,
                acceptOptionalEndOfLine: true,
                requireEndOfLine: false,
                declaredRows: fixture.Height,
                allowEndOfInput: true,
                maximumAllocatedBufferBytes: 1048576,
                maximumStoredRows: fixture.Height,
                maximumDecodedRows: fixture.Height);
            int rejected = 0;
            int cleanEof = 0;
            // Sample early and late cuts; testing every prefix would repeat decoding quadratically.
            var lengths = Enumerable.Range(1, Math.Min(96, fixture.RawCcittData.Length - 1))
                .Concat(Enumerable.Range(1, 12).Select(i => fixture.RawCcittData.Length * i / 13))
                .Concat(new[] { fixture.RawCcittData.Length - 1 }).Distinct();
            foreach (int length in lengths)
            {
                var input = fixture.RawCcittData.Take(length).ToArray();
                var bitmap = new byte[full.Length];
                if (CcittFaxBitmapDecoder.TryDecodeBitmap(input, ref bitmap, in options, out var result))
                {
                    int pixelLength = result.DecodedRows * stride;
                    var output = new CcittFaxDecodeFilter().DecodeWithConsumption(input,
                        dictionary,
                        TestFilterProvider.Instance,
                        0,
                        out int consumed,
                        out var status);
                    Assert.Equal(full.Take(pixelLength).ToArray(), output.ToArray());
                    Assert.Equal(bitmap.Take(pixelLength).ToArray(), output.ToArray());
                    Assert.Equal(result.BytesConsumed, consumed);
                    Assert.Equal(result.Status, status);
                    cleanEof++;
                }
                else
                {
                    AssertFilterMatchesLegacy(input, dictionary, true);
                    rejected++;
                }
            }

            Assert.True(cleanEof > 0);
            Assert.True(rejected > 0);
            AssertFilterMatchesLegacy(Array.Empty<byte>(), dictionary, true);
        }

        /// <summary>Checks the exact UInt16 width limit and the first wider legacy-dispatched row.</summary>
        /// <remarks>Two vertical-zero codes encode two white rows. Width 65,535 succeeds directly;
        /// width 65,536 rejects without changing caller output. The filter returns the same known
        /// white raster at both widths, with white padding in either polarity.</remarks>
        [Theory]
        [InlineData(65535)]
        [InlineData(65536)]
        public void CompactWidthBoundaryPreservesPixels(int columns)
        {
            foreach (bool blackIsOne in new[] { false, true })
            {
                var bitmap = Enumerable.Repeat((byte)0xAA, (columns + 7) / 8 * 2).ToArray();
                bool success = CcittFaxBitmapDecoder.TryDecodeBitmap(new byte[] { 0xC0 },
                    bitmap,
                    columns,
                    2,
                    CcittFaxCompressionType.Group4_2D,
                    false,
                    blackIsOne);
                Assert.Equal(columns == 65535, success);
                if (!success)
                {
                    Assert.All(bitmap, value => Assert.Equal(0xAA, value));
                }

                var expected = Enumerable.Repeat(blackIsOne ? (byte)0 : (byte)255, bitmap.Length).ToArray();
                if (success)
                {
                    Assert.Equal(expected, bitmap);
                }

                Assert.Equal(expected,
                    new CcittFaxDecodeFilter().Decode(new byte[] { 0xC0 },
                        CreateTestDictionary(columns, 2, 2, -1, false, false, blackIsOne),
                        TestFilterProvider.Instance,
                        0).ToArray());
            }
        }

        /// <summary>Checks bounded header detection on inputs shorter than its twenty-byte window.</summary>
        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(19)]
        [InlineData(20)]
        public void HeaderSearchStaysWithinAvailableInput(int length)
        {
            var input = new byte[length];
            input[0] = 0x98; // MH white-eight; later bytes are outside the requested row.
            Assert.Equal(new byte[] { 0 },
                new CcittFaxDecodeFilter().Decode(input,
                    CreateTestDictionary(8, 1, 1, 0, false, false, true),
                    TestFilterProvider.Instance,
                    0).ToArray());
        }

        /// <summary>Verifies that an explicit EndOfLine requirement preserves the first actual row.</summary>
        /// <remarks>Optional/absent EOL preserves the first MH white row despite a later EOL/black row.
        /// The required case supplies EOL/black directly instead of scanning past image data.</remarks>
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        [InlineData(null)]
        public void RequiredEndOfLineControlsInitialRow(bool? endOfLine)
        {
            var input = endOfLine == true
                ? PackBits(Eol + "00110101000101")
                : new byte[] { 0x98, 0x00, 0x13, 0x51, 0x40 };
            Assert.Equal(new byte[] { endOfLine == true ? (byte)255 : (byte)0 },
                new CcittFaxDecodeFilter().Decode(input,
                    CreateTestDictionary(8, 1, 1, 0, false, endOfLine, true),
                    TestFilterProvider.Instance,
                    0).ToArray());
        }

        /// <summary>Stops at Rows without including a prefetched following row or trailer in BytesConsumed.</summary>
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ConsumptionAtRowLimitExcludesTheNextRow(bool aligned)
        {
            var input = PackBits(WhiteEightPixels + (aligned ? "000" : "") + "00110101000101" + "11111111");
            var output = new CcittFaxDecodeFilter().DecodeWithConsumption(input,
                CreateTestDictionary(8, 1, 1, 0, false, false, true, aligned),
                TestFilterProvider.Instance,
                0,
                out int consumed,
                out var status);
            Assert.Equal(new byte[] { 0 }, output.ToArray());
            Assert.Equal(1, consumed);
            Assert.Equal(CcittFaxDecodeStatus.RowLimit, status);
        }

        /// <summary>Checks marker consumption in every coding family while excluding following syntax.</summary>
        [Theory]
        [InlineData(0, false)]
        [InlineData(0, true)]
        [InlineData(2, true)]
        [InlineData(-1, false)]
        public void CompleteMarkersExcludePrefetchedFollowingBytes(int k, bool endOfLine)
        {
            string row = k < 0 ? "1" : (endOfLine ? Eol : "") + (k > 0 ? "1" : "") + WhiteEightPixels;
            string marker = string.Concat(Enumerable.Repeat(Eol + (k > 0 ? "1" : ""), k < 0 ? 2 : 6));
            var encoded = PackBits(row + marker);
            var input = encoded.Concat(new byte[] { 0x45, 0x49, 0x20 }).ToArray();
            var output = new CcittFaxDecodeFilter().DecodeWithConsumption(input,
                CreateTestDictionary(8, 7, 7, k, true, endOfLine, true),
                TestFilterProvider.Instance,
                0,
                out int consumed,
                out var status);
            Assert.Equal(new byte[] { 0 }, output.ToArray());
            Assert.Equal(encoded.Length, consumed);
            Assert.Equal(CcittFaxDecodeStatus.EndOfBlock, status);
            Assert.Equal(0x45, input[consumed]);
        }

        /// <summary>Requires 1D tags on single and shortened mixed Group 3 RTC endings.</summary>
        /// <remarks>Missing/zero tags cannot authorize an incomplete marker even when Rows matches.
        /// Call the fast API directly so legacy recovery cannot mask the rejection.</remarks>
        [Theory]
        [InlineData(1, "")]
        [InlineData(1, "0")]
        [InlineData(2, "")]
        [InlineData(2, "0")]
        [InlineData(5, "0")]
        public void IncompleteMixedRtcRejectsInvalidTags(int eolCount, string lastTag)
        {
            string trailer = string.Concat(Enumerable.Repeat(Eol + "1", eolCount - 1)) + Eol + lastTag;
            Assert.False(TryDecodeFramed(Eol + "1" + WhiteEightPixels + trailer,
                CcittFaxCompressionType.Group3_2D,
                1,
                out _,
                out _,
                out _,
                out _));
        }

        /// <summary>Checks exact-Rows zero-fill endings across coding, polarity, alignment and Height hints.</summary>
        /// <remarks>Whole zero bytes are a local EOF tolerance, not RTC/EOFB. Height cannot authorize it.</remarks>
        [Theory]
        [InlineData(0, false)]
        [InlineData(0, true)]
        [InlineData(2, true)]
        [InlineData(-1, false)]
        public void ExactRowsAcceptZeroFillAcrossCodingFamilies(int k, bool endOfLine)
        {
            foreach (bool aligned in new[] { false, true })
            foreach (bool blackIsOne in new[] { false, true })
            foreach (bool withEol in new[] { false, true })
            foreach (int fillBits in new[] { 0, 8, 65, 4096 })
            {
                string row = k < 0 ? "1" : (endOfLine ? Eol : "") + (k > 0 ? "1" : "") + WhiteEightPixels;
                if (aligned)
                {
                    row = row.PadRight((row.Length + 7) / 8 * 8, '0');
                }

                var input = PackBits(row + row + (withEol ? Eol + (k > 0 ? "1" : "") : "") + new string('0', fillBits));
                foreach (int height in new[] { 0, 1, 2, 10 })
                {
                    var output = new CcittFaxDecodeFilter().DecodeWithConsumption(input,
                        CreateTestDictionary(8, 2, height, k, true, endOfLine, blackIsOne, aligned),
                        TestFilterProvider.Instance,
                        0,
                        out int consumed,
                        out var status);
                    Assert.Equal(Enumerable.Repeat(blackIsOne ? (byte)0 : (byte)255, height == 1 ? 1 : 2).ToArray(),
                        output.ToArray());
                    Assert.Equal(input.Length, consumed);
                    Assert.Equal(withEol ? CcittFaxDecodeStatus.IncompleteEndOfBlock : CcittFaxDecodeStatus.EndOfInput,
                        status);
                }
            }
        }

        /// <summary>A failed long zero-fill probe preserves the input position and next bit.</summary>
        [Theory]
        [InlineData(0)]
        [InlineData(511)]
        [InlineData(1023)]
        public void ZeroFillProbePreservesPositionOnNonzeroInput(int setBit)
        {
            string tail = new string('0', setBit) + "1" + new string('0', 1023 - setBit);
            var reader = new CcittFaxBitReader(PackBits("101" + tail));
            reader.ReadBitsExact(3);
            int consumed = reader.BytesConsumed;
            Assert.False(reader.TryReadZeroFillToEnd());
            Assert.Equal(consumed, reader.BytesConsumed);
            Assert.Equal(setBit == 0 ? 1 : 0, reader.ReadBitsExact(1));
        }

        /// <summary>Rejects a shortened Group 4 ending if alignment would hide nonzero bits.</summary>
        /// <remarks>Check zero padding before alignment can discard a set bit; storage capacity must not affect acceptance.</remarks>
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void SingleEolChecksPaddingBeforeAlignment(bool zeroPadding)
        {
            foreach (int capacity in new[] { 1, 10 })
            {
                var bitmap = new byte[capacity];
                var options = CreateBoundaryOptions(CcittFaxCompressionType.Group4_2D, capacity, true, 1);
                var input = PackBits("1" + (zeroPadding ? "0000000" : "1000000") + Eol);
                Assert.Equal(zeroPadding,
                    CcittFaxBitmapDecoder.TryDecodeBitmap(input, ref bitmap, in options, out var result));
                Assert.Equal(
                    zeroPadding ? CcittFaxDecodeStatus.IncompleteEndOfBlock : CcittFaxDecodeStatus.NotCompleted,
                    result.Status);
            }
        }

        /// <summary>Checks EOF padding before byte alignment could discard a set bit.</summary>
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void EndOfInputChecksPaddingBeforeAlignment(bool group4)
        {
            var bitmap = new byte[1];
            var type = group4 ? CcittFaxCompressionType.Group4_2D : CcittFaxCompressionType.ModifiedHuffman;
            var options = CreateBoundaryOptions(type, 1, true, 1);
            Assert.False(CcittFaxBitmapDecoder.TryDecodeBitmap(PackBits((group4 ? "1" : WhiteEightPixels) + "1"),
                ref bitmap,
                in options,
                out var result));
            Assert.Equal(0, result.BytesConsumed);
        }

        /// <summary>A Height hint cannot authorize whole-byte zero fill or a single Group 4 EOL when Rows is unknown.</summary>
        [Theory]
        [InlineData(0, false)]
        [InlineData(2, true)]
        [InlineData(-1, false)]
        public void UnknownRowsRejectUnverifiedTrailers(int k, bool endOfLine)
        {
            string row = k < 0 ? "1" : (endOfLine ? Eol : "") + (k > 0 ? "1" : "") + WhiteEightPixels;
            var type = k < 0 ? CcittFaxCompressionType.Group4_2D :
                k > 0 ? CcittFaxCompressionType.Group3_2D : CcittFaxCompressionType.ModifiedHuffman;
            foreach (int capacity in new[] { 1, 10 })
            foreach (string trailer in k < 0 ? new[] { Eol, new string('0', 64) } : new[] { new string('0', 64) })
            {
                var bitmap = new byte[capacity];
                var options = CreateBoundaryOptions(type, capacity, false, 0);
                Assert.False(CcittFaxBitmapDecoder.TryDecodeBitmap(PackBits(row + trailer),
                    ref bitmap,
                    in options,
                    out _));
            }
        }

        /// <summary>Unknown Rows without EndOfBlock checks every complete row while limiting stored pixels.</summary>
        /// <remarks>A verified consumed boundary distinguishes fast-path success from a legacy retry.</remarks>
        [Theory]
        [InlineData(0, false)]
        [InlineData(0, true)]
        [InlineData(2, true)]
        [InlineData(-1, false)]
        public void UnknownRowsWithoutBlockMarkerChecksAllRows(int k, bool endOfLine)
        {
            foreach (bool aligned in new[] { false, true })
            foreach (bool blackIsOne in new[] { false, true })
            {
                string first = k < 0
                    ? "00100110101000101"
                    : (endOfLine ? Eol : "") + (k > 0 ? "1" : "") + "00110101000101";
                string next = k < 0 ? "11" : k > 0 ? Eol + "0" + "11" : first;
                if (aligned)
                {
                    first = first.PadRight((first.Length + 7) / 8 * 8, '0');
                    next = next.PadRight((next.Length + 7) / 8 * 8, '0');
                }

                var input = PackBits(first + string.Concat(Enumerable.Repeat(next, 16)));
                foreach (int height in new[] { 0, 1, 17, 25 })
                {
                    var dictionary = CreateTestDictionary(8, 0, height, k, false, endOfLine, blackIsOne, aligned);
                    foreach (bool omitRows in new[] { false, true })
                    {
                        if (omitRows)
                        {
                            var parameters = (DictionaryToken)dictionary.Data[NameToken.DecodeParms.Data];
                            dictionary = dictionary.With(NameToken.DecodeParms,
                                new DictionaryToken(parameters.Data
                                    .Where(x => x.Key != NameToken.Rows.Data)
                                    .ToDictionary(x => NameToken.Create(x.Key), x => x.Value)));
                        }

                        var output = new CcittFaxDecodeFilter().DecodeWithConsumption(input,
                            dictionary,
                            TestFilterProvider.Instance,
                            0,
                            out int consumed,
                            out var status);
                        Assert.Equal(
                            Enumerable.Repeat(blackIsOne ? (byte)255 : (byte)0, height > 0 ? Math.Min(height, 17) : 17)
                                .ToArray(),
                            output.ToArray());
                        Assert.Equal(input.Length, consumed);
                        Assert.Equal(CcittFaxDecodeStatus.EndOfInput, status);
                    }
                }
            }
        }

        /// <summary>Reaches positive Rows after discarded image rows without consuming a following marker.</summary>
        [Fact]
        public void RowLimitAfterDiscardedRowsExcludesFollowingMarker()
        {
            var output = new CcittFaxDecodeFilter().DecodeWithConsumption(PackBits("111" + Eol + Eol),
                CreateTestDictionary(8, 3, 1, -1, false, false, true),
                TestFilterProvider.Instance,
                0,
                out int consumed,
                out var status);
            Assert.Equal(new byte[] { 0 }, output.ToArray());
            Assert.Equal(1, consumed);
            Assert.Equal(CcittFaxDecodeStatus.RowLimit, status);
        }

        /// <summary>Rejects malformed extra rows even after Height has filled the returned image.</summary>
        /// <remarks>Complete first row then unsupported extension: cropping must not manufacture
        /// a verified input boundary. Fast rejection is checked independently of legacy recovery.</remarks>
        [Fact]
        public void StoredHeightDoesNotHideAnInvalidExtraRow()
        {
            var bitmap = new byte[1];
            var options = CreateBoundaryOptions(CcittFaxCompressionType.Group4_2D, 1, false, 1, maximumStoredRows: 1);
            Assert.False(CcittFaxBitmapDecoder.TryDecodeBitmap(PackBits("1" + "0000001"),
                ref bitmap,
                in options,
                out var result));
            Assert.Equal(0, result.BytesConsumed);
            Assert.Equal(CcittFaxDecodeStatus.NotCompleted, result.Status);
        }

        /// <summary>Recognizes EOF or a final marker before attempting a budgeted resize.</summary>
        /// <remarks>Width 8 requires 40 bytes of transition arrays plus a one-byte bitmap. A budget
        /// of 41 admits exactly that state. An ending succeeds; another row throws before replacing
        /// the array. Exercise both unknown-Rows EOF and marker-based termination.</remarks>
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void FinalBoundaryDoesNotAllocateAnotherRow(bool withMarker)
        {
            var bitmap = new byte[1];
            var options = new CcittFaxDecodeOptions(8,
                1,
                CcittFaxCompressionType.ModifiedHuffman,
                false,
                true,
                acceptOptionalEndOfLine: true,
                endOfBlock: withMarker,
                requireEndOfLine: false,
                verifyEndMarker: withMarker,
                allowEndOfInput: !withMarker,
                maximumAllocatedBufferBytes: 41);
            string marker = withMarker ? string.Concat(Enumerable.Repeat(Eol, 6)) : "";
            Assert.True(CcittFaxBitmapDecoder.TryDecodeBitmap(PackBits(WhiteEightPixels + marker),
                ref bitmap,
                in options,
                out var result));
            Assert.Equal(41L, result.AllocatedBufferBytes);
            Assert.Single(bitmap);
            Assert.Throws<CorruptCompressedDataException>(() => CcittFaxBitmapDecoder.TryDecodeBitmap(
                PackBits(WhiteEightPixels + WhiteEightPixels + marker),
                ref bitmap,
                in options,
                out _));
            Assert.Single(bitmap);
        }

        /// <summary>Checks black makeup runs across compact and legacy widths in every coding family.</summary>
        /// <remarks>Fixed T.4 codes encode black using 2560/64-pixel makeup lengths and a final
        /// zero/63-pixel terminating code. This tests wide arithmetic, the white-zero first run
        /// and partial-byte padding; expected pixels are painted without decoding code tables.</remarks>
        [Theory]
        [InlineData(65535)]
        [InlineData(65536)]
        public void WideBlackRowsMatchIndependentPixels(int columns)
        {
            string runs = "00110101" + string.Concat(Enumerable.Repeat("000000011111", columns / 2560))
                                     + string.Concat(Enumerable.Repeat("0000001111", columns % 2560 / 64))
                                     + (columns % 64 == 0 ? "0000110111" : "000001100111");
            foreach (int k in new[] { 0, 2, -1 })
            foreach (bool group3 in k == 0 ? new[] { false, true } : new[] { true })
            foreach (bool aligned in new[] { false, true })
            foreach (bool blackIsOne in new[] { false, true })
            {
                string row = k < 0 ? "001" + runs : (group3 ? Eol : "") + (k > 0 ? "1" : "") + runs;
                if (aligned)
                {
                    row = row.PadRight((row.Length + 7) / 8 * 8, '0');
                }

                var input = PackBits(row + row);
                var expected = CreateWhiteThenBlackPixels(columns, 0, blackIsOne);
                expected = expected.Concat(expected).ToArray();
                Assert.Equal(expected,
                    new CcittFaxDecodeFilter().Decode(input,
                        CreateTestDictionary(columns, 2, 2, k, false, k < 0 ? false : group3, blackIsOne, aligned),
                        TestFilterProvider.Instance,
                        0).ToArray());
            }
        }

        /// <summary>Checks short RTC acceptance across MH, Group 3 1D and tagged mixed Group 3.</summary>
        /// <remarks>Unknown or exact Rows permit 2–5 EOLs with zero fill; mismatched positive Rows reject.
        /// Call the fast decoder directly so fallback cannot hide a framing error.</remarks>
        [Theory]
        [InlineData(0, false)]
        [InlineData(0, true)]
        [InlineData(2, true)]
        public void ShortRtcAcrossCodingFamiliesRequiresMatchingRows(int k, bool endOfLine)
        {
            string row = (endOfLine ? Eol : "") + (k > 0 ? "1" : "") + WhiteEightPixels;
            var type = k > 0 ? CcittFaxCompressionType.Group3_2D
                : endOfLine ? CcittFaxCompressionType.Group3_1D : CcittFaxCompressionType.ModifiedHuffman;
            foreach (int eolCount in new[] { 2, 3, 4, 5 })
            foreach (int declaredRows in new[] { 0, 1, 2, 3 })
            {
                var input = PackBits(row + row + string.Concat(Enumerable.Repeat(Eol + (k > 0 ? "1" : ""), eolCount)) +
                                     "00000000");
                var bitmap = new byte[2];
                var options = CreateBoundaryOptions(type, 2, false, declaredRows);
                bool expected = declaredRows == 0 || declaredRows == 2;
                Assert.Equal(expected,
                    CcittFaxBitmapDecoder.TryDecodeBitmap(input, ref bitmap, in options, out var result));
                Assert.Equal(expected ? CcittFaxDecodeStatus.IncompleteEndOfBlock : CcittFaxDecodeStatus.NotCompleted,
                    result.Status);
                if (expected)
                {
                    Assert.Equal(2, result.DecodedRows);
                    Assert.Equal(input.Length, result.BytesConsumed);
                }
            }
        }

        /// <summary>An invalid late row cannot authorize a short RTC merely because Rows matches output capacity.</summary>
        [Fact]
        public void DamagedRowCannotAuthorizeIncompleteTermination()
        {
            var bitmap = new byte[2];
            var options = CreateBoundaryOptions(CcittFaxCompressionType.Group3_1D, 2, false, 2);
            Assert.False(CcittFaxBitmapDecoder.TryDecodeBitmap(
                PackBits(Eol + WhiteEightPixels + Eol + "0000000001" + Eol + Eol),
                ref bitmap,
                in options,
                out var result));
            Assert.All(bitmap, value => Assert.Equal(0, value));
            Assert.Equal(0, result.DecodedRows);
        }

        /// <summary>Rejects a makeup run at the row bound before an attacker can accumulate a huge length.</summary>
        /// <remarks>A 2560-pixel makeup code already exceeds width 8; rejection must precede accumulation.</remarks>
        [Fact]
        public void MakeupAccumulationRejectsAtRowBound()
        {
            var bitmap = new byte[1];
            Assert.False(CcittFaxBitmapDecoder.TryDecodeBitmap(
                PackBits(string.Concat(Enumerable.Repeat("000000011111", 32))),
                bitmap,
                8,
                1,
                CcittFaxCompressionType.ModifiedHuffman,
                false,
                true));
            Assert.Equal(new byte[] { 0 }, bitmap);
        }

        /// <summary>Accepts unknown-Rows clean EOF without using image capacity as the stopping condition.</summary>
        /// <remarks>Vary initial capacity around the actual two-row count to ensure it cannot select the ending.</remarks>
        [Theory]
        [InlineData(0, false)]
        [InlineData(0, true)]
        [InlineData(2, true)]
        [InlineData(-1, false)]
        public void UnknownRowsCleanEofIsIndependentOfCapacity(int k, bool endOfLine)
        {
            string row = k < 0 ? "1" : (endOfLine ? Eol : "") + (k > 0 ? "1" : "") + WhiteEightPixels;
            var type = k < 0 ? CcittFaxCompressionType.Group4_2D
                : k > 0 ? CcittFaxCompressionType.Group3_2D
                : endOfLine ? CcittFaxCompressionType.Group3_1D : CcittFaxCompressionType.ModifiedHuffman;
            var input = PackBits(row + row);
            foreach (int capacity in new[] { 1, 2, 10 })
            {
                var bitmap = new byte[capacity];
                var options = CreateBoundaryOptions(type, capacity, false, 0);
                Assert.True(CcittFaxBitmapDecoder.TryDecodeBitmap(input, ref bitmap, in options, out var result));
                Assert.Equal(2, result.DecodedRows);
                Assert.Equal(input.Length, result.BytesConsumed);
                Assert.Equal(CcittFaxDecodeStatus.EndOfInput, result.Status);
                Assert.Equal(new byte[2], bitmap.Take(2).ToArray());
            }
        }

        /// <summary>Checks optional EOL on black MH/Group 4 rows and optional tagged mixed Group 3 framing.</summary>
        /// <remarks>Mixed Group 3 encodes the first black row in 1D and repeats it in 2D; optional EOL
        /// probes must preserve the mode tag and the first run.</remarks>
        [Theory]
        [InlineData(0)]
        [InlineData(2)]
        [InlineData(-1)]
        public void OptionalRowFramingPreservesBlackPixels(int k)
        {
            foreach (bool includeEol in new[] { false, true })
            foreach (bool blackIsOne in new[] { false, true })
            {
                string prefix = includeEol ? Eol : "";
                string runs = "00110101000101";
                string bits = k < 0 ? prefix + "001" + runs + prefix + "11"
                    : k > 0 ? prefix + "1" + runs + prefix + "0" + "11" : prefix + runs + prefix + runs;
                var type = k < 0 ? CcittFaxCompressionType.Group4_2D
                    : k > 0 ? CcittFaxCompressionType.Group3_2D
                    : CcittFaxCompressionType.ModifiedHuffman;
                var bitmap = new byte[2];
                var options = new CcittFaxDecodeOptions(8,
                    2,
                    type,
                    false,
                    blackIsOne,
                    acceptOptionalEndOfLine: true,
                    requireEndOfLine: false);
                Assert.True(CcittFaxBitmapDecoder.TryDecodeBitmap(PackBits(bits), ref bitmap, in options, out _));
                Assert.Equal(Enumerable.Repeat(blackIsOne ? (byte)255 : (byte)0, 2).ToArray(), bitmap);
            }
        }

        /// <summary>A complete block marker before the first row yields an empty raster, not synthesized white rows.</summary>
        [Theory]
        [InlineData(0)]
        [InlineData(2)]
        [InlineData(-1)]
        public void CompleteMarkerBeforeFirstRowDoesNotInventPixels(int k)
        {
            string marker = string.Concat(Enumerable.Repeat(Eol + (k > 0 ? "1" : ""), k < 0 ? 2 : 6));
            var input = PackBits(marker);
            var output = new CcittFaxDecodeFilter().DecodeWithConsumption(input,
                CreateTestDictionary(8, 4, 4, k, true, k == 0, true),
                TestFilterProvider.Instance,
                0,
                out int consumed,
                out var status);
            Assert.Empty(output.ToArray());
            Assert.Equal(input.Length, consumed);
            Assert.Equal(CcittFaxDecodeStatus.EndOfBlock, status);
        }

        /// <summary>Reports consumed code bytes without counting bytes loaded only for lookahead.</summary>
        [Fact]
        public void ConsumptionExcludesUnreadReservoirBytes()
        {
            var reader = new CcittFaxBitReader(new byte[] { 0xAA, 0x55, 0xAA, 0x55, 0xAA });
            reader.ReadBitsExact(3);
            Assert.True(reader.EnsureBits(32));
            Assert.Equal(1, reader.BytesConsumed);
            reader.ReadBitsExact(6);
            Assert.Equal(2, reader.BytesConsumed);
        }

        /// <summary>Builds explicit parameters without conflating Rows and Height.</summary>
        private static DictionaryToken CreateTestDictionary(int columns,
            int rows,
            int height,
            int k,
            bool endOfBlock,
            bool? endOfLine,
            bool blackIsOne,
            bool aligned = false)
        {
            var parameters = new Dictionary<NameToken, IToken>
            {
                { NameToken.Columns, new NumericToken(columns) }, { NameToken.Rows, new NumericToken(rows) },
                { NameToken.K, new NumericToken(k) },
                { NameToken.BlackIs1, blackIsOne ? BooleanToken.True : BooleanToken.False },
                { NameToken.EncodedByteAlign, aligned ? BooleanToken.True : BooleanToken.False },
                { NameToken.Create("EndOfBlock"), endOfBlock ? BooleanToken.True : BooleanToken.False }
            };
            if (endOfLine.HasValue)
            {
                parameters.Add(NameToken.EndOfLine, endOfLine.Value ? BooleanToken.True : BooleanToken.False);
            }

            return new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                { NameToken.Filter, NameToken.CcittfaxDecode }, { NameToken.Height, new NumericToken(height) },
                { NameToken.DecodeParms, new DictionaryToken(parameters) }
            });
        }

        /// <summary>Configures an eight-pixel fast attempt with bounded growth and complete-row ending checks.</summary>
        private static CcittFaxDecodeOptions CreateBoundaryOptions(CcittFaxCompressionType type,
            int capacity,
            bool aligned,
            int declaredRows,
            int maximumStoredRows = 0)
            => new CcittFaxDecodeOptions(8,
                capacity,
                type,
                aligned,
                true,
                acceptOptionalEndOfLine: true,
                endOfBlock: true,
                requireEndOfLine: false,
                verifyEndMarker: true,
                allowIncompleteTermination: true,
                declaredRows: declaredRows,
                allowEndOfInput: declaredRows == 0,
                maximumAllocatedBufferBytes: 4096,
                maximumStoredRows: maximumStoredRows);

        /// <summary>Paints a white prefix then black suffix, including white padding.</summary>
        /// <remarks>Expected values use pixels only, not production code tables or another decoder.</remarks>
        private static byte[] CreateWhiteThenBlackPixels(int columns, int whitePixels, bool blackIsOne)
        {
            var pixels = Enumerable.Repeat(blackIsOne ? (byte)0 : (byte)255, (columns + 7) / 8).ToArray();
            for (int x = whitePixels; x < columns; x++)
            {
                byte mask = (byte)(128 >> (x & 7));
                pixels[x / 8] = blackIsOne ? (byte)(pixels[x / 8] | mask) : (byte)(pixels[x / 8] & ~mask);
            }

            return pixels;
        }

        /// <summary>Compares bytes or exception types with the retained master filter.</summary>
        /// <remarks>This checks historical recovery, not independent pixel correctness. requireLegacyBoundary
        /// asserts zero consumption and NotCompleted because legacy cannot verify the encoded end.</remarks>
        private static void AssertFilterMatchesLegacy(byte[] input,
            DictionaryToken dictionary,
            bool requireLegacyBoundary)
        {
            byte[] expected = null;
            byte[] actual = null;
            int consumed = 0;
            CcittFaxDecodeStatus status = CcittFaxDecodeStatus.NotCompleted;
            var legacyError = Record.Exception(() => expected = new LegacyCcittFaxDecodeFilter().Decode(input,
                dictionary,
                TestFilterProvider.Instance,
                0).ToArray());
            var currentError = Record.Exception(() => actual = new CcittFaxDecodeFilter().DecodeWithConsumption(input,
                dictionary,
                TestFilterProvider.Instance,
                0,
                out consumed,
                out status).ToArray());
            Assert.Equal(legacyError?.GetType(), currentError?.GetType());
            if (legacyError == null)
            {
                Assert.Equal(expected, actual);
                if (requireLegacyBoundary)
                {
                    Assert.Equal(0, consumed);
                    Assert.Equal(CcittFaxDecodeStatus.NotCompleted, status);
                }
            }
        }

        /*
         * TIFF fixtures adapted from Apache PDFBox 3.0.8 CCITTFactoryTest.
         * Copyright 2014 The Apache Software Foundation.
         * Licensed under the Apache License, Version 2.0 (the "License");
         * you may not use this file except in compliance with the License.
         * You may obtain a copy at https://www.apache.org/licenses/LICENSE-2.0
         * Unless required by applicable law or agreed to in writing, software distributed
         * under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR
         * CONDITIONS OF ANY KIND, either express or implied. See the License for the
         * specific language governing permissions and limitations under the License.
         * Source: https://github.com/apache/pdfbox/blob/3.0.8/pdfbox/src/test/java/org/apache/pdfbox/pdmodel/graphics/image/CCITTFactoryTest.java
         * Adaptation: embed unchanged CCITT strips; test decoded pixels without TIFF import.
         */
        // ccittg3.tif, image 0; original TIFF SHA-256: B8BC24334D4B0D55A5DBB678FFD30A329C86A48C45F0416FEE224EDF3C0566CE.
        private const string PdfBoxGroup3 =
            "ABNlAAJsoABcx4fqDaHHxDrE+/c8Ooh0OY84EDmOhzH7DuMeHh6EOnUwo8PDhnQh1QF5ygAF5WClxF0ewVw07DuYa+8JWHQ5hibU" +
            "AAj0LUCQjJhsoExujheBl7fcQdDqF+OnG4QdXjqYeYAJq3tAUg5WGw4TvQJ4HjdDqPG5DpxnQ6mHmACatwMwYHABNaAMwYHABNW4" +
            "ZAZY8PDw/Ag4NI4HgsjgAmrcMgC6g7WBsgGocDwMDgAmrcCKAejg0aswfmnCrSwGUcAE1oDQOG3nATJAaisG2YxAEcD0BpmFHABN" +
            "W4Ko+ctcf5UQ4bB4cGYdjkrQ8QB/U3McAE1bgqhkKAXmCxnBqDIA/IFcxwATVuB4aicMwcG4/fYHgUQbhnHMcAE1oA8GgZO4Q4f4" +
            "cG44Hgp2FgC7mOACatwMDodDodDocNUcGrhoCuCXjgpjw4UeH6Bm1AwPDw6m3McAE1oAwgNTAzeGQBjwZJhawzKgr4huY4AJrQHl" +
            "QFkhP7FzHMcAE1oCbWrFMCzRbKG5jgAmtAXaqMduhwPK3sqbmOACa0BdqpxQB4tvZU3McAE1oJ/am6AeGfZW3McAE1oJ/YW5g2+K" +
            "OGbsrbmOACPwT2ywSA1B14tqDHQ4bIQ/sUbmOACOrJ7ZYx/A0zizOA7l3sUbmOADxP7Kh2GWKfG4bNi7cxwAeJ/ZRP2GWFTho2UN" +
            "zHAB4LsVcKAy8nhecCNlDcxwAeGPbEh4Degcx03BjZQxyHABdQT+xI+DAYJYNdMcMbKAo4ALqBNjQ+DAYfhdBjs4MbFwo4ALqBNg" +
            "Y7AhAINBaBn2KhRwAXUBNgRwGg4ILMgNGxQKOADAJtWMcCKbggIfBs2JqBjgAwGNgxRwXm3DqR2GzYnuMcAGAxsQMcCJzgio4Avs" +
            "bUDHABgMbGE4Me4DrE5OC+xtQMcAFwI3qcARbsKZiuDGxqgxwAXBjeh2BEE4TdgY2B0GOAC4Mb0dgQ5OE3wGNgdBjgAuDG0OwYcY" +
            "+OW0AX2BuDHABcEHtneAvDs9hocC+wNwY4ALgg9uh2C7/FvTHAvsmeGOAC4JC35YDxWW9QzNkzwxwAXBIW+OwxCgTdYxw0bMTpzH" +
            "ABgPC3jwEuHHsGTho2TJyHABgPC3d4e5y2EJwb7UEOMcAFwSFuHYPk57JmODfasxxjgAwCOtjnYe4cXasUchwxtQ3FHABcHdGhWJ" +
            "NOWzCduhzcE2ocDHABcEhGjwTrYsLBuD7UUDHABcEhGjsJlsXFtuD7CNxRwAXB9mpwCO3x1sSEghwTYMhxRwAXBzM7DvZbFjsNGw" +
            "ZuKOAC4OZuwg6hY9ixPuBDrYwhxRwAXDBk7Cwsti433Q4LutiAccAFwwlHAXFfspFcNEexgOOAC4LrJuCUtjZQK5jg9rYwHHABcF" +
            "1l2HnbF8cGm1sYDjgAuC6y7CU9lAFx8cd+04OOAC4LrDdDioW0g3DQ4QcW1tODjgAuC5LQEo9pHCsGgcWPdYOOAC4LkjgQ9tIfA2" +
            "u91A44ALguquBPTvas+Bl3UDjgAuC6rQL+PbVkOBIPbqBxwAXBdV4xz/e2DG4G49uoHHABcF1XhO57+wYo4HA9uoHHABcF1ScXmP" +
            "7Bk4aMBpHt1A44ALhx2pUC5bJuwYHDXf3UDjgAmu4R2FUCXbJ1gQPzBof3UDjgAmu4R4LoFy2rcBnQ3Au/uoHHABNYcJaCm4mPYR" +
            "D9hipDpwLjveoOOACa0C3grHF49QaiicMDod4vxwyx7eoOOACa0C3goJxkM89e8PTyYZ3PD4M1/eoOOACa0GP2gpji4c1BzcL159" +
            "Aw2PusHaHDLd7DBxwAXUGP3gsOLznvuatuExag3fpQcBlQxGSg44AL7i3gsOOfwPQnFnYLnzcF0BLxwzHT5eUwwOAC+4t4JOPUHm" +
            "GxxY7IfvHVg5Bh46cDDwiUwwOAC+5j94JOMMGHBUcSFgLo68cMx2nILDA4AL7mP5qQ0AednCgEQxAZsNOYWGBwAX3FvUyPjgSOGg" +
            "cM5EEBpHdZKYIOACatxb1IdJwaeB7DV5OSgNg4PWamHHQ4AJqY5j94IoHNuGXC4Fzhm4IkDOOEHqzUwrocAFxB+4tUD52GXCwM+g" +
            "KByHYEDQ/W1QK6HABcPeCKB44Djg7gjlAWc4N4QkY4ZurVArocAFw94IoHZjhr+CgdDuAyxsGoeDNUBxwAXEH7wRQOw2h/waIDYL" +
            "g1PhmqA44ALiD9wWMcNEBlhgrC4Nb6CVARwAXEH71IOOGbzOx+CfHDbeUGPjgX1MkCjocAFxB+8FThXTgXm63/AbY6HwZWCi14pK" +
            "FcAFxB+8FThXThnhUYhfOCqOghDw4N7rEJQrgAuIP3AigV4cGK2NQ4KoRjtwbFgOOAC4e8FhxIDEfI7AqAZBjsG1aCDocAFw94LD" +
            "iQGIOFNPGHYMtbsG1aCDocAFw94KBDgg4z47SVhpjhh8h+GolCOhwATUHuBAbUErOA1B5wwc+DWWxHQ4AJrjhbysM+B3Q8PjqhKG" +
            "OCo8OHmfA0UiUIOhwATXHCuyoRxMDunMfmSgCHBk8+BopKoOOACa44V2UBxxB8cHHTz0yCMcEHDXHD4cwzNmocdDgAmuOFcxUHcI" +
            "ODA7tTIG3BpcNA6HDwfg0GgWwATXcI7Ewd8cRw47jDIvOHHEgThuIOFg/BoNA46HABNdwjsTB3bhoHcbo5CG4UcJCgh1Ao4g4WD8" +
            "Gg0DjocAE13COxoF4cBnHcRBCAzJlw3EcRz8M3ZqFsAE1B3MCBe3AZx3EQMYBggWxj44xwX8M3ZqFsAE1D9mAIHnoDOO3wx1zhs1" +
            "LjhOTh5mOBfZqFdDgAmqHD3pAUO3DOO3ZBVzhoqLjhOTh5mOBfZqFdDgAvjjdRkwLwxwzjt2QWrgIOJWXG6t2Em3AwaBbABfcY6i" +
            "CAYyHAw8GWjHDYXb45D9kOF43EHDGzULAAL7iRhAMc4DjhB4MtIcNCgx9xcrCYbiODmgcdDgAvuMdRhAy8bhxwg8GWm4aFAri5WE" +
            "w3Bxw5kHHQ4AI85OowYZnNwo+OMdakHOGdITiZYLhuDuFMg8AAj8E6jBhmfDhx8cWoiKAzmDcTHYuG4O4UyDwACa9k6jEIcDAwKO" +
            "OoZ5wIAxRyH/4EtwI7HCmQOOACa9k6jGY4ZSMfcKOOoZ5wIGgcf+HCw3CDmOFMgccAE17J1HrjqAyx0dOhwz1DoBcaG5oB4Bd2OH" +
            "S25DocAE17J1HpunDMHR04Zx1UOjHDONjHI4YcF3BhLbm6HABNaCdR6Jwa9FgEHC+xwziY3OYo4IOFcCIiWHJ0OACaw4rqMNDhpj" +
            "8IMM9AvzgQKDc5jcDDgRDJIcnQ4AJrDiDqMjHDWfqAjg/zgQKlDoY6HAw4EciXcx8cAE1hxB1GRjhrP0rAjg/UAwLk7oY4ZbhoHe" +
            "IL3N0OACaw4rq0eHDWeDeAwPuLlAUcykhzHUCOB4blYSAATWHFdWj7hquwywM44uUBRzKSHG4UcDw3KwkAAuhxXUZ9w1HPU+Ahyt" +
            "wEfHRUHg3A9OrRQFcAF0OK6jODY51PwYcrbhIToqBHOAPDe0+LhIABcI6jmDaOMQzgRdAtmWGOE4DxRcJAALhHUf7hrDjED8CLoF" +
            "giwxwnAeFFAkAAuEdR/jhrONQ5g3cI5FpjhOA8KKCOhwAXQ4rqP04asErZw2DiQQIGOA8KKCOhwAXDjqPNwVjqfN0DMgy0MYhwPB" +
            "caEdDgAuDv9uCsde50w1SkGB1wHgQ6SBB0OAC+OJfycFdOf0w1xUM46cDwbvKwrgAuoFv5OCunPnMGwCQ+OB4K0asK4AJq3Fz8GR" +
            "OWHMDwaxwVnEsC9suY7sAE1bi34rBWOvG4vhhwPCvjg0QhwrheBRBjAlgAmrcW++wVUfDHE8MODM4VwkAw6cHiHFwFEMQJYAJrQL" +
            "ffYKqLBuL4YcCsdQFcJAMOnDDw4g6cC8QgSwATWgW6OwVUEOMfHEsF3BWTg3cG44LQ7GBXABNaBb77BVQDjh7Bdww7K4cGHDWiQI" +
            "ODd5OFcAE1oFvvsFVAOOCQDVDKkPjgvAa49qDRFUEgAE1oFvisFU4YcPANbspxwzwGs71DNh6hIABNaBbnrBXmC7gg4JYMOcIa7Y" +
            "4IOGvcnIcG+LWHGOACa0C3xWDLwLjhmWBA7tAfbcDeIYTg3uDhzHQ4AJrQK7OCngYHDMgMob8oAktBDcFDwcOMcAE1oFfDBTsMDh" +
            "mQC745AVzcCS0ENwUDsOHGOAC+4r4YKeAjhmuDHET5zjHBoHBHahuGYGoGOAC4ceDAkwO4ZrgRhK6cOBJETBuOHRDjHABcOPaAp4" +
            "CDhmuGexhKgG7qJIGxwqIcY4ALocI/qCnYI4Zo4aLGDqASQnjFAYgQiHGOAC4WPUCQcCsccCCKgbbhR4a7BgcVDHGOAC4S+oFJwP" +
            "bhU6QBOOxpuMcM9DHGOAC4S7QFNwVxw2DglSQBw+JOCcCKGOMcAFwjnqCk4FY4RwpgFgTcFAEUY4xwAXCvVApjgqOhwrhHnC8gwZ" +
            "R2LnAsARxjjHABcI56gpjgqOnCOFdjmOOEQCvBlBuLAEcY4xwAXCjnqB4NTpwjh3Y5Ob6AV4Q6KjcWAz4Y4xwAXAisDwzX4FiHCP" +
            "47LwZQpjhYNwg9cOTgAuHHOsDwzX4FuHEhc3BnCjHCW3Cj9w5OAC4EVgeGa9AsBi1wNLg1hRjhLOBHVw5OAC6cMJwPDMjgWDHCQu" +
            "BpgGoKBezgPcOTgAuoDFYHhmROI4Z5grOCDpSBeww+Ooh3YALqAwnA8MuGOEHDZwxwaRzgEHSkC+DHKEOohzgAF1Ao5jAPDLhuIO" +
            "Be0PwKOBA5+GFMMeY43ohycAF1Ao5iAPAxbcC7i9jhBwL+GFMMeblaiHJwAXUCjjEAeCjpww7H7CDhmXnY4RTBebg4nh3YALqBRx" +
            "iAPBR84ju95w0ukAiqC83BjkPDnAALqBRxiAPBROHCDncOCppDikgYO3G8OEcAF1Ao4y4Hg3nDhRzHQ4K+gISA6G43B3ABNdwg4w" +
            "gHhnpjocNEA21AiqC5G4sGOcAAmu4QcagDwYRjpwo4kA0jmOtAwJ+oPpOKOQ5OACa46HEclAHgwHuGfgzIULUK7G1WDhg45Dk4AJ" +
            "q3FclAHgvpuGiwzRCHDuxtVg4YI5Dk4AJq3FcjAHh9ZwZAhjivmEO6wfU4dscY4AL7iuRgDwpAQcFYGY4sIahgQe0Q9Bw47ABhOK" +
            "5AQHhEAjhtlweENQwK61x+DhxQADCcVyGgPFODuG0XDEIwScO6qQ85w47ABNexXIbA85s4xwbRhDixqA4eOFHqk4g5R2ACa9i2xI" +
            "D3/5QCqMIcbpQHqDPUnCO47ABNex4xQD1P4Y6HBVrGOKP1rg9QYHqlAQdx2ACa9lzFQZ9QWAxHFf1IcPWC4+XEdvWACa9lzKAKxw" +
            "o4Fxw1PQ4kCR9DhsrBceGJw48O7ABNey5lIFdhqjgpDiQoAwOhAEF04cfvsAE17KE7KQK7A8NMeHAwEDOIQ4Y5WACa9lCdlgMg4o" +
            "4bUAeBCQMDwhDguOoTocAE17G+OwQDw00AeG4mGdWhwQdQnQ4AJr2KPjsLA8GBw8dIdOCo5ixDhHSicHhOhwATXsUfHamB4Ljh36" +
            "Q6gNt2LAxCCNwo/CHxwATXsUcaqB4YHC6Y84nBh90FAxCUB334Q+OACa0CjjWwPCOHIx1BcDBIGcdExjv0+6HABHnFHh40VgfjhQ" +
            "8dOBcfWHAo7A2O334Y4AL7ijj3YK48OGWQ3Au6U1BuDgRjt9+GOAC+43HsdDgSQBgiHDR6Cwbj423Y7H4Y4AJq3G4zICuAUa7g58" +
            "LHAV4COBYVw4AJq3G8PJYMo4Xg0DAhEClgWMDOBX7hwATVuUDSwZRzcrDQIFDy1DjsO+NHAr9w4AJq3KBrYKT3PAziBVoXxR50Nn" +
            "A47hwATVuNxrIZbm4YfPurBgQJpCQWwR8SHAo+OrhwATVuN4eWAyoIcF0/AMCAmmr8F4PaExYFHx1cOACatyiHiUMrG4Lp+wXLhA" +
            "4G8Q9MEeE2PQKPjq4cAE1blAyQMvtwjw6GLAwDCBsYHoGcUx0PQKP9DgAmvY3GqBmYGIy6HVmOEBhNDAscCClD47cr+hwAR1Yo8P" +
            "KYVwfwxGX8NxAicejMBgdOitDk7dX/jgA8KONSCwCTDEIH4bhAagV0erWJwXCxXY/fNDgA8KOMLDuEQD8gawhwhMOOql6HY4EKcO" +
            "4n78TocAE1oFHGChYxxaY4UdegiwFxHYFCwcJzH8rTocAE1bijw8CBbEQnCD/oJ2DBfgDcdEpQnOnadDgAmrcUcZaHsVCcI69F04" +
            "KC/gKS0cJxz7x0OACatxRxloWAiE4j/YwMwCUAqLRPuOffAAJq3FHx2WiDhdccLkGGmoyAzzFSwg7jn3OACatxR94tG5Dofqxxj/" +
            "RAaqvr9lwjt7ScAE1bijp2Wjch3UnJ18gF3DQOgnsXQ5uoN7TgAE1bijp2Wm5F1BOuQ6QEHQ6m3BfaULCcUf04ABNW4o+OyshxiG" +
            "4l4/MJq3McLtaFwWEIABfcUfHZUGBhuJBzFHWpjlAmyhOx+FBQrFfEAAvuKOnZWHLzhLOZDJygLsXJwxCgPWoABfcW2VEOYOcJBT" +
            "YgPDHF2Kw6cFx4cETqAAX3FtlCHbmEhw8OkXY477HsQCk+IQ6HABeHFtlBDkE3D3ny5jjvwezqE4Eg8IhwAX3FtlBuahQKPRnwZj" +
            "jdDrx7HY4HhuXY4AL7iuh2UE7UJxB+0fEJzch6g1kDxFbHABfcUfHYsK8ohwt88YxzHFHjkUgeGUoY4AL7ij47FhX1YYvmhE7cUe" +
            "QRQB4ZYRjgAvuKOnhccJWHdXmDHx1A3iCK0OCwOhwygbHABfcUdPlA4wk454QQeHUDeIGCIfHA8Fy7HABeHFvKBxhIcTsGkJ+DiD" +
            "FWUOC0OFGMcAF4cW0LoccYNjidird3aDMgWRw6djgAvuLaKRXQRji+hvhu76ixGgMnCJ2OAC+4rw+LCD4OHE9DVASJOUex0OGYOG" +
            "MCCEOAC+4r46Khj4PcTkNp0OEvGDj2QH9j/EOFGIcAF9xXZUEB7icgNDofHCRiBx7qF90IcOIABfcVyKAgNQJmBY8OFeLjhbJoZO" +
            "HEAAvuK++KKApdQLkYh4cJEDY6hbCocOIABfcV98U3DiE4lxgGIahjp1sEAgQAC+4r74puHEIcJckAiJQN1sFBgQAC+4r74qnCAw" +
            "rw6SARerHtTBjTgAF+Bvviu4QuFjAgYzJlC2FAvpOAC+4r48KgwP6hHavHDOPGDxbqwMFyOAAX3HGPCoaNArsmhwzjxg9g8DB9Jw" +
            "AX4O8eFYc3CBhIYZD744sGPD4EY62XQ0CaTgAvwd48Kw5OEQHdnQ4N3Q5j7oaLZdDQOHOAC+44x4WhxuC7w4QcMYCQFHhsh1sugM" +
            "OPh7gAvuOMeF8cbgwPDhBwYgPYOJmOtl0Bhx1puAC/A33yjcnAr8HFTHWy8BhddwAX4G++U443BVcJyHEHYqY62XgMLruAC+4r75" +
            "YFsGo9QJaKR7CIwGA9wAX3FffLAuCOGgdD7494O6KR7VQfTcAF+BR98tDjpxXCOCPzQ4LlY9qoJruAC/A33y3HBINwg4O6oDBYPa" +
            "oD67gAuoF7ClAeYRxzBeGPlhbCNWHH9NwAX3FyHDi4ZsBqPlp7VnirDjrXcAF+B9jghShwMDgYgQdAp7JvOsE1UAAvwP0Ohw5UCl" +
            "c+pFsm8tQfVQAC6gWwXKg27bt0SFsC/Wgg8IABfcUzK0OGaOPslLY15kNzHqtwAX4Hs1x7gyxxmZbGkP2iG5va7gAvwPd2bgQO7j" +
            "QtiXaIY5P1bgAuoF3MGBwxtxdgV8OuMccKrcAF9wg6pDw+O4v3pqMeHIdDscV2PDjHT77sNwoUKFGOC46HnVuE+++OnQ+8Ih4ffc" +
            "CHCjhlUTgAvwHHx0ODA5OhxB8fHhwccnG4aT77geGUOn3x4dDgfDoYAC/AHgoHDJDULgAvuC2DMIABeHDIDbHCjgeEHQ8PDw4UcM" +
            "DgigAF9wyA1x5wbwoUJ998cOPvvjw8Oy6fuHFHQ4j6gAF+AzjocMgQcnUBsSBFlQGceUAAvpwZPBqdD0ayJtSAhIAC8dCaHh0Oh4" +
            "DrEg4FUhdqgLmAAXiRDoS4CWh+xAoXaoGAIAFwyvCnBtHAvBOMTCDse2WKBRsAFw27BMAomIO0gSQmAC8PDw8PDodDodDodDoeHh" +
            "xlpjgl3hxoJbkkAB2NscHDCq/AluSQAHYkxwwPiyHDRdIDAB2JBocxINl0hoAHYoY4bB0Dbhsg5DQAOxMhwUFwzIchsAHYscAz45" +
            "CgAOxYbgWYchUAHZQVgrjt08PSKAAdlJ4Cq7sWRQADsqM4a3uFIsAB2WEgOOGfjfkCAA7BBahI3Tp8cLIKAB2CjqF0CyUwAdqQPq" +
            "duRkADtVDRY3ORmADtYQ4Zi8AAmygA==";

        // ccittg4.tif, image 0; original TIFF SHA-256: 57254CEB1AB84172C5D346B90570971C40079444BBBC7F5BFE11F66CD675ACE7.
        private const string PdfBoxGroup4 =
            "y5keI+ag2iOM8RHRxF8vm4vEdGIjojmR4uBBHMjojmR82HLjI8R4jxhEdF0ZhSPEeI4ZyhEdGgLmdhCMeIiLQikLHERHwhEOIp0I" +
            "mYeLpaRhHUCRghI4bAZCYW5RyF4REM9l9EQcIjowoRHRcZcIR0b0VMh5qnCVLaGoiK+00ECwQv/iIiIiIiIiIiJDOPXuQyAyxzDm" +
            "HMOeCIOQaR/ghERBoMrApBqH+QIoQPRyDRqgYMZNOQq0VnUMjhtnMwEOTIaisSDzIxBAjiD0INM+4kC45zxM8j7QIochsHMOJDKH" +
            "LHBCfRHiQB/Lf8cRFDEGEHDceSBfxEpOJDA5BuOez2IiJBZiXDP6kDwaBhnLgoch/MOMEJA8Nq2xXuQMDlDlDlDlDlDkNUcg1cII" +
            "QYYISVnHEg2jmHIUcw56EM2oTjmHMOVP6xERGgfYxghxF5CuEIj/kPKgkFQgpz7F3+EdrUcSY1X7O1UZHZdEcDyNsf/CqP/yToEX" +
            "QRX/wggRBt8ij/v2dlghgNQjo+KbUmOUPMIj/6+WOFQ44yHdD/HoFEjhliCPxNxIXYv/hGcIIIQ9fiwlQQRyLwYZcCPyMeCiEQb0" +
            "Fkxht9ZdVSBBEMBkEsGKOPj8UCSSCkXQjHYb/pAoSEMXD/cIEggiLO+52rGRwINMECEjncqOGsIEkEGH38IECFIIJOg6+gkECCQQ" +
            "bkn/QYQIJG2CIUz3DD6CCSChAgYv/SQQQQX/BAgRBhxBEfRLaEvkEHpVBRPsMof/1SCPysjgXBBfDhKEoQSH/0ECCCKgQpGPDgin" +
            "30EgQUIj2DtoNfoKkq4etdQS6De/bwkgRuiECyQ9BLXCSIkwkClOpQ7Lgm+uEkCQxY9v/1S31XYhIJhIvkdXDI4In8IJBBArBqOv" +
            "0gRHUIJKEd5dX77SQS8ofsfCQQWgrYir9hBAkISOxsuTHv+nSOxfQj/8JBISGgc4+/pAihwSCO0gy4aC4S3+gkKBYIIqxZHFv61S" +
            "iENP6SR9KEEEP9IIJBAgQRHAkI9+kcdJYIJf/VdKv9UMKkgQQkCM/kOO0tBCgSIMDjfhHe2kqSCCSPP/661UZb/3h1W0UOpDFQQL" +
            "p/D3oJTqDUmXDBHRHZxWEyPf+PBFnM5rO4RHjPQhIQgg2N/wRH6lj0jNKEoTiwbDCwRdQ5Q+Hw/3SVKjuDERBFuKOIJJDNBOLZEf" +
            "uP6aBCIRoRcUZsGcwi3CE4EN6cEXwzKf+likEIMEHPZIeEccKEiYZxynHf+CI/+Ey4KjF0IVHrx2g/+2OEInsMSOoIEg7b/jSoI4" +
            "5AkcQRHDOECh6h4X1VkGnoYo8i5SGwcdIILuCI/kEWshlxhk4ZqFB5CD+uGPvwgvQZ0BQRyI7jevx16QRDjhAhLclBCzM4hQTJji" +
            "H//1CESL4gyOglFxBEeWPBEf/iCI/EchsF7/+xwRDRAhkMFa21r9RLHEheLOWOeCTnHIbbmUExzjxpoJfwRTqU4WIZfwwyOiP25t" +
            "Arf/+QzwsHuOsEYfDX/ewgqBC2Iow4tpWIq8dQ7DEgkM8m0mQZBkGIUIL/+Dwinqw//8RO4cMbukQ0x2X8GFXC3EbGFWZxap/nH0" +
            "SsM5wOXRHiPkdN0CFsw4fINFDEr/hURH+x3xII4i/wh/wnkfT4S6LHQRDXHDw4wgv7CsUhqH0okGHyhw9Oh70CBAnUhx4cdBBEOO" +
            "RIZ4ZcQjhfsJf+JTj8utaWVDI6v//+CW/Y8Ri2Gy4hcTqhx2l/3BMG3OO4/wg0FBGdtkd1hGdBw4ZTsuHRMfhLKHD0O/hv1///Gc" +
            "cIutW/8JSEHtt4cPV49vsJPFu3hCgbBHHdsHayIPb8dJWYDkcJ9IJhhMuKGg37qEvCI64bf/XH/xMOH9a9W3nHwtaD22H98df7r3" +
            "pb9//S/CKHBCeCkcdpBAkGRyI+Dd1Ijvh/9bKMj//sfaeq//zroKyOgwgihx6HI3ixxvctwgv/S1+IRHXLHt3fH17/pCxlYIME0k" +
            "my5oijiWPsxOvwvpBSPmEN9V9R9/+/WNQYa+7tBFD8Hd1//5pelTfxwwU4vf1uEklEEXhBgc70kQo6bsjoEy4HhvFj/6pYYYv/40" +
            "/h/XpQ2Gy5LUER1EJE3EgVuEbV/+Pd/SrvcQQK6x+EgpEMnAioSGGTHEJiP/o7+D/X4P/6WVDDFBUv6CWUP+qrDdbH/iGCKhrGTo" +
            "zzNgx0EECKHIHguNfGl+m6GSkGCOmy6vzjhdKN8MLlwPBvDvT/62QJEfQjfhPbFbtiIkC45BWciXYNExwvpAirk6sJyGHIHhXSIN" +
            "EFDpMGGw4/61evIMz4kcAi6Q2YcNt31/3vwRHX/fYIp2/+/g0gjjhrIKycUENCIiHcP9fHHyGHLKzDhBCQSDIIg7L3//uK0CBatI" +
            "GLDDr+ur3rHVvf++HbDkEtsMwgQIZY6BBUGSHDOJmHD/XD0O60ko0tuHwgvwow30IYpVUINsP4/wm/kF3OOwlf+vw3+1diGGgqRG" +
            "OQaB0CvHC8a2/euhFUxIMDhw/733aUQRdbv+UPdvbtvrSmi1+K7CESHHppIhtuDCCdgyOKvrai4cKKWHIxxa/tZBXHcPqFu/8K8S" +
            "FHZcKMV7b+2rkFRyh7bM5C8UGQZRyxfDDDX0n+9sMjmRx6BK3/74/tsjkXO+UOF9/iwew7MOIIJghKsMKI0E2TdEeD1kOP/bI4jt" +
            "odkcI9kf/H23EMS6BiQzH/Yd/ynu/Jjv3j4/7fdxBsSCOQQcLiRuiOuF97uHIxyDSOv2Rj3+/Io694NlDhSKOQIHErxuHuPv+1xI" +
            "O8cLH+4f/ww4bZDDhAmDIQcRIPZzljpXdPhf9tkR2DZeYkC/KIX4T7/7d2wyOCpMOqbiU+L/v2RzCKHFIdfHCQXDVIocREqAtMPY" +
            "ZMdf2m0U5CjgzANJHMjpMifCttb6KHvh4hMKVEUQrtBaH+N+u1EQRQ/+v/kwB4dhlwZImHwiYRHaTJxD3D/RICEcFZJKhpgiPSh9" +
            "4YK/uhKehSSStWR/7/h1pcFXe/qCCH2EZsnGRwbUSHSlQHBHHQXYwv8O6BHyMAqpIIEUoIeoR9l3+sNahFDwkqmtchgft/62hCEZ" +
            "DESwgYQS0Na/waI4UEQLjs+gQIJZ6I4bAQIEyMSDI8v9s2GqRwUkkEYAwR1EL6D/BFP4i00hvQp/+DIMg7CIbUCJDYkIYH9kdYS9" +
            "3hiLKAPDcIQhZQ//34MgeDA4RjZhynIKjkxYkOQjoKx//w2Rw/70hWsER/X8YbI4UXhsicQw53KCklEk6/35WwPCFw7ZHUECQiFC" +
            "CGRjxdXt8w8miNgfkcK2CKcSHHClwRR2D5fVjvxjIK45hxce5U0qGv/6wRQ4w9BENHoEIkUcFWn8L5MgK2g2XB1kcKwlFO/5h6IM" +
            "o7PBoFglCBJV6/qPk3D2qBFD1S//4ShB3xCEFIo+uv94RDLcm6QQcEVezpESCmwShBJQgiOv8w9BKojfblXoi8CQS//r0mvduwso" +
            "cEoWWP//xq0iEcIjp3bYaFhCoQX8fCvCQQqhSI6kx22GDEj5HAhBBNUC+9zD0Qrhfe7KduhIUcpwuKQIut48aVdfewy6c1o4gkIx" +
            "67/VEIgJlA94xZHQnkCBIMIQSMOq8EC5NbwgQRx0COOCI62VgyI4iIiQqxDQVK/3zD0ohVS92GeAiOl8X/8a0klSQIpx4aGkk+//" +
            "QXpVCGLKhKv4/nHwiOFZxxQSIYaZUYmYZzMVCgi49/3hIkOUOuRjrTDPWfZdaj/pfjqq0QXeR1iEUOTcJev/CCCEIIUkgihwgiEH" +
            "KHKmW4hiEK0GR//eEgkE4wkoIEI8mO0mFQw/6GuhSBEdKoc7KEXZHzCQIwtf1sKEEuEMJJOKiPQo1r46JDr1hIcscOCI6UKEv+EU" +
            "OElptVLHtBs7EApF/CW/jWtql9s7OjCCrHr6qpFHpJeECps7HYIREJfhFD7fWqSBFPdNsrIHif1wtJxShBAqQXclIHhlf/6j6qvb" +
            "v/VVSId0kIVAiOq2yVojgsEdEcMr/12jOhyEEX+xDKHOOIv3j/ppBFICWElYiZojgtEcL/oIofr4inW4g9+vG66ojf9OcRNAZAf+" +
            "FI/d0usahh2DKHIZg7sH/SCD9aUIEUOqtsSgPLHPxId/44/wgtdLzuoXHsfw+tVilVtxLoQ1/neRRX0q9bCI6xb//d6pIRCCTYIF" +
            "hD//660h7/94UUCBEdJVi2dPv9t7jW5hPfXWIMj9BSr0wQVMrdRwMOvXxrv+PCvtfMOTd1kMiPhJEWCY+sjp3//22ngihxEUEUPq" +
            "mPv6ftkcZcF0YdxnAQwFsUiOmUBhEff/v38Oop/6+3u2XJiIiR+F8MN//btkFV0EUORB//9fGJ7INRzUEnZRSMOEH//ZEeR0R8Ec" +
            "eEH419v2GEU+QjiPEEUOx6/XzjiGYPWorX0o4yoBtlxsgvAIj9HYRwiP+9SOHGKiISVMM4uvu9gyh2iBiKC3tjr7yh2MVM4Qr2vp" +
            "QQiCBENuwi7BRwbBlaEQdDvFkrKHRHGDYpNqEW/NX3DCI8IfHoj7+3/FudyBA8Gsf1r0okcGCOGBCnYFDDI6CSv7hkdGkR4j5HZi" +
            "P5eM0YyPEciOiOyOKyxzDhkdF8vl2Q2YRhGEYRjI4LkdEeLo2zCL5fL5HRdEfLxERHiPl8uBAQkxxEiU9iyPkdBCIwmXQQyPkfTQ" +
            "jjeIiIiGXy+hEREREREREREWR0Xy+R4joIREREgwOUM4iIiIiIjQiIiIiIx6F7kMgNschRyB4Qcocw5hzDkKOQwOQRRUhkBrj2JC" +
            "wVBUFOdzucchxzudzjmHMOEIiJxF9mHCCyI/shnHKHIZAg9qhEREWhERERFpiNeU4Mh8GouiPGMRCIm1J7J0RNEeI6I6I8QONiNs" +
            "iqSa4iI8J1YIj7D6yGV5CnEWhESoZcYJEdkelQCxRsEOAUGYhHYQQiI5hzDmHMOYcocococococococw4RHGS0yOCG4FFI24iIiI" +
            "iIiIiL2NFX/bI4YI+JTojhoCBY2TEhSUMmOIZHRBtILWNDqDEqBIKMBYbCCUNgyCuOW4ReQK7WKHbJw1j7MKGDKA5HDOCCBBDDIt" +
            "Qk4O5Tgi+Rwt6ERiCgyD6k7LkZAyGi4SgyhxDiMAEAE=";

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

        /// <summary>Decodes readable framing vectors with the same ending policy as the PDF filter.</summary>
        /// <remarks>No legacy retry: false means fast-path rejection. Output uses black=0.</remarks>
        private static bool TryDecodeFramed(string bits,
            CcittFaxCompressionType compression,
            int rows,
            out byte[] bitmap,
            out int completedRows,
            out int consumed,
            out CcittFaxDecodeStatus status)
        {
            bitmap = new byte[1];
            var options = new CcittFaxDecodeOptions(8,
                1,
                compression,
                false,
                false,
                acceptOptionalEndOfLine: true,
                endOfBlock: true,
                requireEndOfLine: false,
                verifyEndMarker: true,
                allowIncompleteTermination: true,
                declaredRows: rows,
                allowEndOfInput: rows == 0,
                maximumAllocatedBufferBytes: 1024);
            bool success =
                CcittFaxBitmapDecoder.TryDecodeBitmap(PackBits(bits), ref bitmap, in options, out var result);
            completedRows = result.DecodedRows;
            consumed = result.BytesConsumed;
            status = result.Status;
            return success;
        }

        /// <summary>Creates eight-pixel PDF framing parameters with independent Rows and Height.</summary>
        private static DictionaryToken CreateFramingDictionary(int rows,
            int height,
            int k,
            bool endOfBlock,
            bool endOfLine)
            => new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                { NameToken.Filter, NameToken.CcittfaxDecode },
                { NameToken.Height, new NumericToken(height) },
                {
                    NameToken.DecodeParms, new DictionaryToken(new Dictionary<NameToken, IToken>
                    {
                        { NameToken.Columns, new NumericToken(8) },
                        { NameToken.Rows, new NumericToken(rows) },
                        { NameToken.K, new NumericToken(k) },
                        { NameToken.Create("EndOfBlock"), endOfBlock ? BooleanToken.True : BooleanToken.False },
                        { NameToken.EndOfLine, endOfLine ? BooleanToken.True : BooleanToken.False }
                    })
                }
            });

        /// <summary>Creates a fixed-size legacy-comparison image with Rows equal to its decoded height.</summary>
        /// <remarks>EndOfBlock is absent and therefore defaults to true; ending-specific tests use explicit dictionaries.</remarks>
        private static DictionaryToken CreateCcittDictionary(int columns,
            int rows,
            int k,
            bool blackIsOne,
            bool byteAligned = false)
            => new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                { NameToken.Filter, NameToken.CcittfaxDecode },
                {
                    NameToken.DecodeParms, new DictionaryToken(new Dictionary<NameToken, IToken>
                    {
                        { NameToken.Columns, new NumericToken(columns) },
                        { NameToken.Rows, new NumericToken(rows) },
                        { NameToken.K, new NumericToken(k) },
                        { NameToken.EncodedByteAlign, byteAligned ? BooleanToken.True : BooleanToken.False },
                        { NameToken.BlackIs1, blackIsOne ? BooleanToken.True : BooleanToken.False }
                    })
                }
            });

        /// <summary>Packs readable MSB-first bits, filling the final partial byte with zeros.</summary>
        /// <remarks>Implicit final-byte fill matters in EOF tests; eight explicit zero bits add a whole byte.</remarks>
        private static byte[] PackBits(string bits)
        {
            var result = new byte[(bits.Length + 7) / 8];
            for (int i = 0; i < bits.Length; i++)
                if (bits[i] == '1')
                    result[i / 8] |= (byte)(1 << (7 - i % 8));
            return result;
        }

        /// <summary>Builds a tiny PDF whose image declares unsafe dimensions with almost no compressed data.</summary>
        /// <remarks>The optional ASCIIHex stage checks that a filter chain reaches the same CCITT allocation guard.</remarks>
        private static byte[] CreateAllocationBombPdf(int columns, int rows, bool filterChain)
        {
            const string content = "q 1 0 0 1 0 0 cm /Bomb Do Q\n";
            var data = filterChain ? "00>" : "\0";
            var filters = filterChain ? "[/ASCIIHexDecode /CCITTFaxDecode]" : "/CCITTFaxDecode";
            var parameters = $"<< /K -1 /Columns {columns} /Rows {rows} >>";
            if (filterChain) parameters = $"[null {parameters}]";
            var objects = new[]
            {
                "<< /Type /Catalog /Pages 2 0 R >>",
                "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
                "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 10 10] /Resources << /XObject << /Bomb 5 0 R >> >> /Contents 4 0 R >>",
                $"<< /Length {content.Length} >>\nstream\n{content}endstream",
                $"<< /Type /XObject /Subtype /Image /Width {columns} /Height {rows} /ColorSpace /DeviceGray /BitsPerComponent 1 /Filter {filters} /DecodeParms {parameters} /Length {data.Length} >>\nstream\n{data}\nendstream"
            };
            var builder = new StringBuilder("%PDF-1.7\n");
            var offsets = new List<int>();
            for (var i = 0; i < objects.Length; i++)
            {
                offsets.Add(builder.Length);
                builder.Append($"{i + 1} 0 obj\n{objects[i]}\nendobj\n");
            }

            var xrefOffset = builder.Length;
            builder.Append($"xref\n0 {objects.Length + 1}\n0000000000 65535 f \n");
            foreach (var offset in offsets)
            {
                builder.Append($"{offset.ToString("D10", CultureInfo.InvariantCulture)} 00000 n \n");
            }

            builder.Append($"trailer\n<< /Size {objects.Length + 1} /Root 1 0 R >>\nstartxref\n{xrefOffset}\n%%EOF\n");
            return Encoding.ASCII.GetBytes(builder.ToString());
        }
    }
}