namespace UglyToad.PdfPig.Tests.Integration
{
    using PdfPig.Core;
    using PdfPig.Filters;
    using PdfPig.Fonts;
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

            // PNG "Up" prediction: each row carries its filter type, then the difference to the
            // row above it.
            var predicted = new byte[rows * (columns + 1)];
            for (var row = 0; row < rows; row++)
            {
                predicted[row * (columns + 1)] = 2;

                for (var column = 0; column < columns; column++)
                {
                    var current = expected[(row * columns) + column];
                    var above = row == 0 ? 0 : expected[((row - 1) * columns) + column];

                    predicted[(row * (columns + 1)) + 1 + column] = (byte)(current - above);
                }
            }

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
        

#if NET || NETSTANDARD2_1_OR_GREATER
        // The cross-reference stream of this document is itself Brotli compressed, so on a target
        // without a Brotli decoder it cannot be opened at all.
        [Fact]
        public void BrotliDecode()
        {
            var path = IntegrationHelpers.GetSpecificTestDocumentPath("Brotli-Prototype-FileA.pdf");
            using (var document = PdfDocument.Open(path, new ParsingOptions() { UseLenientParsing = true }))
            {
                foreach (var page in document.GetPages())
                {
                    Assert.NotNull(page);
                }
            }
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
