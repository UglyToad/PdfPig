namespace UglyToad.PdfPig.Tests.Integration
{
    using PdfPig.Core;
    using PdfPig.Filters;
    using PdfPig.Fonts;
    using PdfPig.DocumentLayoutAnalysis.TextExtractor;
    using PdfPig.Tokens;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class FilterTests
    {
        private static readonly Lazy<string> DocumentFolder = new Lazy<string>(() => Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Integration", "Documents")));
        private static readonly HashSet<string> _documentsToIgnore =
        [
            "issue_671.pdf",
            "GHOSTSCRIPT-698363-0.pdf",
            "ErcotFacts.pdf"
        ];

#if NET || NETSTANDARD2_1_OR_GREATER
        [Fact]
        public void BrotliDecodeRoundTripsCompressedData()
        {
            var expected = System.Text.Encoding.ASCII.GetBytes(
                new string('A', 200) + "Hello Brotli compression coming to PDF!" + new string('B', 200));

            byte[] compressed;
            using (var ms = new System.IO.MemoryStream())
            {
                using (var brotli = new System.IO.Compression.BrotliStream(ms, System.IO.Compression.CompressionMode.Compress, leaveOpen: true))
                {
                    brotli.Write(expected, 0, expected.Length);
                }

                compressed = ms.ToArray();
            }

            var filter = new BrotliFilter();
            var parameters = new DictionaryToken(new Dictionary<NameToken, IToken>());

            var decoded = filter.Decode(compressed, parameters, DefaultFilterProvider.Instance, 0);

            Assert.Equal(expected, decoded.ToArray());
        }
#endif
        
        [Fact]
        public void BrotliDecodeFilterReportsSupported()
        {
#if NET || NETSTANDARD2_1_OR_GREATER
            Assert.True(new BrotliFilter().IsSupported);
#else
            Assert.False(new BrotliFilter().IsSupported);
#endif
        }

        [Fact]
        public void DefaultFilterProviderResolvesBrotliDecode()
        {
            var dictionary = new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                { NameToken.Filter, NameToken.BrotliDecode }
            });

            var filters = DefaultFilterProvider.Instance.GetFilters(dictionary);

            var filter = Assert.Single(filters);
            Assert.IsType<BrotliFilter>(filter);
        }

#if NET || NETSTANDARD2_1_OR_GREATER
        [Fact]
        public void BrotliDecodeRejectsLargeWindowStream()
        {
            // "Large window brotli" compressed by the reference implementation with
            // --large_window=30, which is the bitstream of IETF RFC 9841. The extension requires
            // support for it, but no decoder available to us provides it: BrotliStream is limited
            // to the RFC 7932 window of 2^24 bytes, as is the C# decoder in google/brotli. The
            // filter has to report such a stream rather than let the failure escape untyped.
            byte[] largeWindow =
            [
                0x11, 0x1e, 0x24, 0x00, 0x02, 0x4c, 0x61, 0x72, 0x67, 0x65, 0x20, 0x77, 0x69,
                0x6e, 0x64, 0x6f, 0x77, 0x20, 0x62, 0x72, 0x6f, 0x74, 0x6c, 0x69, 0x03
            ];

            var parameters = new DictionaryToken(new Dictionary<NameToken, IToken>());

            var exception = Assert.Throws<CorruptCompressedDataException>(
                () => new BrotliFilter().Decode(largeWindow, parameters, DefaultFilterProvider.Instance, 0));

            Assert.Contains("RFC 9841", exception.Message);
        }

        [Fact]
        public void BrotliDecodeRejectsTruncatedStream()
        {
            var compressed = CompressWithBrotli(SampleContent);

            Assert.Throws<CorruptCompressedDataException>(
                () => DecodeWithBrotli(compressed[..(compressed.Length / 2)]));
        }

        [Fact]
        public void BrotliDecodeRejectsDamagedStream()
        {
            var compressed = CompressWithBrotli(SampleContent);
            compressed[compressed.Length / 2] ^= 0xFF;

            Assert.Throws<CorruptCompressedDataException>(() => DecodeWithBrotli(compressed));
        }

        [Fact]
        public void BrotliDecodeAcceptsEmptyInput()
        {
            // Not a Brotli stream, but PDFs carry empty streams and they are not an error.
            Assert.Empty(DecodeWithBrotli([]).ToArray());
        }

        [Fact]
        public void BrotliDecodeAppliesPngPredictor()
        {
            // The extension extends the LZWDecode and FlateDecode predictor parameters of
            // ISO 32000, Clause 7.4.4.3 to BrotliDecode, so a predictor has to be honoured here
            // exactly as the Flate filter honours it.
            const int columns = 6;
            const int rows = 4;

            var expected = new byte[columns * rows];
            for (var i = 0; i < expected.Length; i++)
            {
                expected[i] = (byte)(i * 7);
            }

            var predicted = EncodeWithPngPredictor(expected, rows, columns, 1, PngUp);

            var streamDictionary = new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                { NameToken.Filter, NameToken.BrotliDecode },
                {
                    NameToken.DecodeParms, new DictionaryToken(new Dictionary<NameToken, IToken>
                    {
                        { NameToken.Predictor, new NumericToken(12) },
                        { NameToken.Columns, new NumericToken(columns) }
                    })
                }
            });

            var decoded = new BrotliFilter().Decode(CompressWithBrotli(predicted), streamDictionary,
                DefaultFilterProvider.Instance, 0);

            Assert.Equal(expected, decoded.ToArray());
        }

        [Fact]
        public void BrotliDecodeAppliesPngPredictorAcrossColours()
        {
            // Three colours at eight bits: the "Sub" row filter subtracts the pixel three bytes to
            // the left, so Colors and BitsPerComponent both have to reach the row length.
            const int columns = 4;
            const int colors = 3;
            const int rows = 3;
            const int rowLength = columns * colors;

            var expected = new byte[rows * rowLength];
            for (var i = 0; i < expected.Length; i++)
            {
                expected[i] = (byte)(i * 5);
            }

            var predicted = EncodeWithPngPredictor(expected, rows, rowLength, colors, PngSub);

            var streamDictionary = new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                { NameToken.Filter, NameToken.BrotliDecode },
                {
                    NameToken.DecodeParms, new DictionaryToken(new Dictionary<NameToken, IToken>
                    {
                        { NameToken.Predictor, new NumericToken(15) },
                        { NameToken.Colors, new NumericToken(colors) },
                        { NameToken.BitsPerComponent, new NumericToken(8) },
                        { NameToken.Columns, new NumericToken(columns) }
                    })
                }
            });

            var decoded = new BrotliFilter().Decode(CompressWithBrotli(predicted), streamDictionary,
                DefaultFilterProvider.Instance, 0);

            Assert.Equal(expected, decoded.ToArray());
        }

        [Fact]
        public void BrotliDecodeTakesTheDecodeParmsAtItsOwnIndex()
        {
            // Two filters mean DecodeParms is an array, and each filter has to read the entry at
            // its own position. Reading the first would miss the predictor here and hand back the
            // rows with their filter type bytes still in them.
            const int columns = 6;
            const int rows = 4;

            var expected = new byte[columns * rows];
            for (var i = 0; i < expected.Length; i++)
            {
                expected[i] = (byte)(i * 3);
            }

            var predicted = EncodeWithPngPredictor(expected, rows, columns, 1, PngUp);
            var hex = Convert.ToHexString(CompressWithBrotli(predicted)) + ">";

            var streamDictionary = new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                {
                    NameToken.Filter,
                    new ArrayToken([NameToken.AsciiHexDecode, NameToken.BrotliDecode])
                },
                {
                    NameToken.DecodeParms, new ArrayToken([
                        NullToken.Instance,
                        new DictionaryToken(new Dictionary<NameToken, IToken>
                        {
                            { NameToken.Predictor, new NumericToken(12) },
                            { NameToken.Columns, new NumericToken(columns) }
                        })
                    ])
                }
            });

            var filters = DefaultFilterProvider.Instance.GetFilters(streamDictionary);

            Assert.Equal(2, filters.Count);

            var data = (Memory<byte>)System.Text.Encoding.ASCII.GetBytes(hex);
            for (var i = 0; i < filters.Count; i++)
            {
                data = filters[i].Decode(data, streamDictionary, DefaultFilterProvider.Instance, i);
            }

            Assert.Equal(expected, data.ToArray());
        }

        private const byte PngSub = 1;
        private const byte PngUp = 2;

        /// <summary>
        /// Encodes rows the way a producer would: every row carries its PNG filter type, then the
        /// difference to the byte that filter names - the pixel to the left for "Sub", the row
        /// above for "Up".
        /// </summary>
        private static byte[] EncodeWithPngPredictor(byte[] raw, int rows, int rowLength, int bytesPerPixel, byte filterType)
        {
            var encoded = new byte[rows * (rowLength + 1)];

            for (var row = 0; row < rows; row++)
            {
                encoded[row * (rowLength + 1)] = filterType;

                for (var i = 0; i < rowLength; i++)
                {
                    var reference = filterType switch
                    {
                        PngSub => i < bytesPerPixel ? (byte)0 : raw[(row * rowLength) + i - bytesPerPixel],
                        PngUp => row == 0 ? (byte)0 : raw[((row - 1) * rowLength) + i],
                        _ => (byte)0
                    };

                    encoded[(row * (rowLength + 1)) + 1 + i] = (byte)(raw[(row * rowLength) + i] - reference);
                }
            }

            return encoded;
        }

        private static readonly byte[] SampleContent = System.Text.Encoding.ASCII.GetBytes(
            new string('A', 2000) + "Hello Brotli compression coming to PDF!" + new string('B', 2000));

        private static byte[] CompressWithBrotli(byte[] data)
        {
            using var ms = new System.IO.MemoryStream();

            using (var brotli = new System.IO.Compression.BrotliStream(ms,
                       System.IO.Compression.CompressionMode.Compress, leaveOpen: true))
            {
                brotli.Write(data, 0, data.Length);
            }

            return ms.ToArray();
        }

        private static Memory<byte> DecodeWithBrotli(byte[] input)
        {
            return new BrotliFilter().Decode(input,
                new DictionaryToken(new Dictionary<NameToken, IToken>()),
                DefaultFilterProvider.Instance,
                0);
        }
#endif

        [Fact]
        public void BrotliDecodeIsRefusedForInlineImagesWhenNotLenient()
        {
            // ISO 32000, Clause 8.9.7 as amended by the extension: BrotliDecode SHALL NOT be
            // used for inline images.
            var path = IntegrationHelpers.GetSpecificTestDocumentPath("Brotli-InlineImage.pdf");

            using var document = PdfDocument.Open(path, new ParsingOptions() { UseLenientParsing = false });

            // BrotliDecode is a 2.0 extension, so the document declares 2.0. A strict read of that
            // header has to succeed before anything in the document can be reached.
            Assert.Equal(2.0, document.Version);

            var exception = Assert.Throws<PdfDocumentFormatException>(() => document.GetPage(1));

            Assert.Contains("inline images", exception.Message);
        }

        [Fact]
        public void BrotliDecodeInlineImageIsStillReadWhenLenient()
        {
            // Forbidden, but decodable, so a lenient read keeps the content rather than losing it.
            var path = IntegrationHelpers.GetSpecificTestDocumentPath("Brotli-InlineImage.pdf");

            using var document = PdfDocument.Open(path, new ParsingOptions() { UseLenientParsing = true });

            Assert.Equal(2.0, document.Version);

            var image = Assert.Single(document.GetPage(1).GetImages());

            Assert.True(image.IsInlineImage);

#if NET || NETSTANDARD2_1_OR_GREATER
            Assert.True(image.TryGetBytesAsMemory(out var bytes));
            Assert.Equal(16, bytes.Length);
#endif
        }
        

#if NET || NETSTANDARD2_1_OR_GREATER
        // The cross-reference streams of these documents are themselves Brotli compressed, so on a
        // target without a Brotli decoder they cannot be opened at all.
        [Theory]
        [InlineData("Brotli-Prototype-FileA.pdf", 25, "SHEET INDEX")]
        [InlineData("Brotli-Prototype-FileB.pdf", 52, "Deriving HTML from PDF")]
        [InlineData("Brotli-Prototype-FileC.pdf", 57, "Well-Tagged PDF")]
        public void BrotliDecode(string documentName, int expectedPages, string expectedOnFirstPage)
        {
            var path = IntegrationHelpers.GetSpecificTestDocumentPath(documentName);

            using var document = PdfDocument.Open(path, new ParsingOptions() { UseLenientParsing = true });

            // All three carry a 2.0 header
            Assert.Equal(2.0, document.Version);

            Assert.Equal(expectedPages, document.NumberOfPages);

            // Every page is read, so every content stream in the document is decoded.
            Assert.Equal(expectedPages, document.GetPages().Count());

            // Reading the text is what proves the streams decoded; a page object exists either way.
            Assert.Contains(expectedOnFirstPage, ContentOrderTextExtractor.GetText(document.GetPage(1)));
        }
#else
        [Fact]
        public void BrotliDecodeReportsTheMissingDecoder()
        {
            // The reason has to reach the caller instead of arriving as "could not find an xref
            // trailer", which is what a swallowed filter failure used to look like from outside.
            var path = IntegrationHelpers.GetSpecificTestDocumentPath("Brotli-Prototype-FileA.pdf");

            var exception = Assert.Throws<NotSupportedException>(
                () => PdfDocument.Open(path, new ParsingOptions() { UseLenientParsing = true }));

            Assert.Contains("BrotliDecode", exception.Message);
        }
#endif

        [Theory]
        [MemberData(nameof(GetAllDocuments))]
        public void NoImageDecoding(string documentName)
        {
            // Add the full path back on, we removed it so we could see it in the test explorer.
            documentName = Path.Combine(DocumentFolder.Value, documentName);

            var parsingOptions = new ParsingOptions
            {
                UseLenientParsing = true,
                FilterProvider = MyFilterProvider.Instance
            };

            using (var document = PdfDocument.Open(documentName, parsingOptions))
            {
                for (var i = 0; i < document.NumberOfPages; i++)
                {
                    var page = document.GetPage(i + 1);

                    foreach (var pdfImage in page.GetImages())
                    {
                        if (pdfImage.ImageDictionary.TryGet(NameToken.Filter, out NameToken filter))
                        {
                            if (filter.Data.Equals(NameToken.FlateDecode.Data) ||
                                filter.Data.Equals(NameToken.FlateDecodeAbbreviation.Data) || 
                                filter.Data.Equals(NameToken.LzwDecode.Data) ||
                                filter.Data.Equals(NameToken.LzwDecodeAbbreviation.Data))
                            {
                                continue;
                            }
                        }
                        else
                        {
                            continue;
                        }

                        Assert.False(pdfImage.TryGetPng(out _));
                    }
                }
            }
        }

        public sealed class NoFilter : IFilter
        {
            public bool IsSupported => false;

            public Memory<byte> Decode(Memory<byte> input, DictionaryToken streamDictionary, IFilterProvider filterProvider, int filterIndex)
            {
                throw new NotImplementedException();
            }
        }

        public class MyFilterProvider : BaseFilterProvider
        {
            /// <summary>
            /// The single instance of this provider.
            /// </summary>
            public static readonly IFilterProvider Instance = new MyFilterProvider();

            /// <inheritdoc/>
            protected MyFilterProvider() : base(GetDictionary())
            {
            }

            private static Dictionary<string, IFilter> GetDictionary()
            {
                var ascii85 = new Ascii85Filter();
                var asciiHex = new AsciiHexDecodeFilter();
                var flate = new FlateFilter();
                var runLength = new RunLengthFilter();
                var lzw = new LzwFilter();

                var noFilter = new NoFilter();

                return new Dictionary<string, IFilter>
                {
                    { NameToken.Ascii85Decode.Data, ascii85 },
                    { NameToken.Ascii85DecodeAbbreviation.Data, ascii85 },
                    { NameToken.AsciiHexDecode.Data, asciiHex },
                    { NameToken.AsciiHexDecodeAbbreviation.Data, asciiHex },
                    { NameToken.CcittfaxDecode.Data, noFilter },
                    { NameToken.CcittfaxDecodeAbbreviation.Data, noFilter },
                    { NameToken.DctDecode.Data, noFilter },
                    { NameToken.DctDecodeAbbreviation.Data, noFilter },
                    { NameToken.FlateDecode.Data, flate },
                    { NameToken.FlateDecodeAbbreviation.Data, flate },
                    { NameToken.Jbig2Decode.Data, noFilter },
                    { NameToken.JpxDecode.Data, noFilter },
                    { NameToken.RunLengthDecode.Data, runLength },
                    { NameToken.RunLengthDecodeAbbreviation.Data, runLength },
                    {NameToken.LzwDecode, lzw },
                    {NameToken.LzwDecodeAbbreviation, lzw }
                };
            }
        }

        public static IEnumerable<object[]> GetAllDocuments
        {
            get
            {
                var files = Directory.GetFiles(DocumentFolder.Value, "*.pdf");

                // Return the shortname so we can see it in the test explorer.
                return files.Where(x => !_documentsToIgnore.Any(i => x.EndsWith(i))).Select(x => new object[] { Path.GetFileName(x) });
            }
        }
    }
}
