namespace UglyToad.PdfPig.Tests.Filters
{
    using System;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using PdfPig.Core;
    using PdfPig.Filters;
    using PdfPig.Tokens;
    using static FilterTestHelpers;

    public class DecompressionLimitTests
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void RejectsAnOversizedIntermediateStageBeforeStartingTheNext(bool lzw)
        {
            var data = Enumerable.Repeat((byte)42, 2_000_000).ToArray();
            var encoded = lzw ? LzwFilterTests.LzwEncoder.Encode(data, true, out _) : Compress(data);
            Assert.True(data.Length > encoded.Length * 100L * (lzw ? 2 : 10));
            IFilter decoder = lzw ? new LzwFilter() : new FlateFilter();

            var ex = Assert.Throws<PdfDocumentFormatException>(() =>
                FilterChainDecoder.Decode(encoded, Dictionary(), TestFilterProvider.Instance,
                    new IFilter[] { decoder, new MustNotDecode() }));
            Assert.IsType<DecodeBuffer.LimitExceededException>(ex.InnerException);
            Assert.StartsWith("Decoded stream size exceeds the estimated maximum size.", ex.Message);

            // The existing ratio policy exempts a single/final stage.
            Assert.Equal(data, FilterChainDecoder.Decode(encoded, Dictionary(), TestFilterProvider.Instance,
                new[] { decoder }).ToArray());
        }

        [Fact]
        public void ChecksPredictorPaddingBeforeAllocatingTheResult()
        {
            var dictionary = StreamDictionary(NameToken.FlateDecode,
                new[] { (NameToken.Predictor, 2), (NameToken.Columns, 26) });
            Assert.Throws<DecodeBuffer.LimitExceededException>(() =>
                new FlateFilter().Decode(Compress(new byte[] { 42 }), dictionary, TestFilterProvider.Instance, 0, 25));
        }


        private const int SmallLimit = 2048;

        [Theory]
        [InlineData("Flate", 2048)]
        [InlineData("Lzw", 2048)]
        [InlineData("RunLength", 2048)]
        [InlineData("Flate", 2049)]
        [InlineData("Lzw", 2049)]
        [InlineData("RunLength", 2049)]
#if NET
        [InlineData("Brotli", 2048)]
        [InlineData("Brotli", 2049)]
#endif
        public void DirectDecodersAcceptTheLimitAndRejectOneByteMore(string filter, int length)
        {
            var data = Enumerable.Repeat((byte)42, length).ToArray();
            var encoded = Encode(filter, data);
            if (length > SmallLimit)
            {
                Assert.Throws<DecodeBuffer.LimitExceededException>(() => Decode(filter, encoded));
            }
            else
            {
                Assert.Equal(data, Decode(filter, encoded).ToArray());
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void CapsSingleAndLastStagesIndependentlyOfTheRatio(bool chained)
        {
            var data = Enumerable.Repeat((byte)42, SmallLimit + 1).ToArray();
            var encoded = Encode("RunLength", data);
            var filters = chained
                ? new IFilter[] { new FlateFilter(), new RunLengthFilter() }
                : new IFilter[] { new RunLengthFilter() };
            if (chained)
            {
                encoded = Compress(encoded);
            }
            Assert.Throws<DecodeBuffer.LimitExceededException>(() =>
                FilterChainDecoder.Decode(encoded, Dictionary(), TestFilterProvider.Instance, filters, SmallLimit));
        }

        [Fact]
        public void InlineImagesPropagateTheDecoderLimit()
        {
            var data = Enumerable.Repeat((byte)42, SmallLimit + 1).ToArray();
            var image = new PdfPig.Content.InlineImage(
                new PdfRectangle(0, 0, 1, 1), 1, 1, 8, false,
                PdfPig.Graphics.Core.RenderingIntent.RelativeColorimetric, false,
                Array.Empty<double>(), Encode("RunLength", data), new LimitedProvider(),
                new[] { NameToken.RunLengthDecode }, Dictionary(),
                PdfPig.Graphics.Colors.DeviceGrayColorSpaceDetails.Instance, null);
            Assert.Throws<DecodeBuffer.LimitExceededException>(() => image.TryGetBytesAsMemory(out _));
        }

        [Fact]
        public void Ascii85BoundsZeroBlocksAndPartialBlocks()
        {
            var filter = new Ascii85Filter();
            byte[] Bytes(string text) => System.Text.Encoding.ASCII.GetBytes(text);
            Assert.Equal(8, filter.Decode(Bytes("zz"), Dictionary(), TestFilterProvider.Instance, 0, 8).Length);
            Assert.Throws<DecodeBuffer.LimitExceededException>(() =>
                filter.Decode(Bytes("zzz"), Dictionary(), TestFilterProvider.Instance, 0, 8));
            Assert.Equal(3, filter.Decode(Bytes("!!!!"), Dictionary(), TestFilterProvider.Instance, 0, 3).Length);
            Assert.Throws<DecodeBuffer.LimitExceededException>(() =>
                filter.Decode(Bytes("!!!!!"), Dictionary(), TestFilterProvider.Instance, 0, 3));
        }

        private static Memory<byte> Decode(string filter, byte[] input) => filter switch
        {
            "Flate" => new FlateFilter().Decode(input, Dictionary(), TestFilterProvider.Instance, 0, SmallLimit),
            "Lzw" => new LzwFilter().Decode(input, Dictionary(), TestFilterProvider.Instance, 0, SmallLimit),
            "RunLength" => new RunLengthFilter().Decode(input, Dictionary(), TestFilterProvider.Instance, 0, SmallLimit),
#if NET
            "Brotli" => new BrotliFilter().Decode(input, Dictionary(), TestFilterProvider.Instance, 0, SmallLimit),
#endif
            _ => throw new ArgumentOutOfRangeException(nameof(filter))
        };

        private static byte[] Encode(string filter, byte[] data)
        {
            if (filter == "Flate")
            {
                return Compress(data);
            }
            if (filter == "Lzw")
            {
                return LzwFilterTests.LzwEncoder.Encode(data, true, out _);
            }
            using var output = new MemoryStream();
#if NET
            if (filter == "Brotli")
            {
                using (var brotli = new BrotliStream(output, CompressionLevel.Optimal, true))
                {
                    brotli.Write(data, 0, data.Length);
                }
                return output.ToArray();
            }
#endif
            for (var remaining = data.Length; remaining > 0;)
            {
                var run = Math.Min(128, remaining);
                output.WriteByte(run == 1 ? (byte)0 : (byte)(257 - run));
                output.WriteByte(42);
                remaining -= run;
            }
            output.WriteByte(128);
            return output.ToArray();
        }

        private sealed class LimitedProvider : BaseFilterProvider, ILookupFilterProvider
        {
            public LimitedProvider() : base(new System.Collections.Generic.Dictionary<string, IFilter>
            {
                [NameToken.RunLengthDecode.Data] = new LimitedRunLength()
            }) { }

            public System.Collections.Generic.IReadOnlyList<IFilter> GetFilters(
                DictionaryToken dictionary, PdfPig.Tokenization.Scanner.IPdfTokenScanner scanner)
                => GetFilters(dictionary);
        }

        private sealed class LimitedRunLength : IFilter
        {
            public bool IsSupported => true;

            public Memory<byte> Decode(Memory<byte> input, DictionaryToken dictionary, IFilterProvider provider, int index)
                => new RunLengthFilter().Decode(input, dictionary, provider, index, SmallLimit);
        }


        [Theory]
        [InlineData(2)]
        [InlineData(15)]
        public void UsesPdfBoxPredictorRowLimitBeforeAllocatingOrPadding(int predictor)
        {
            var decoder = new PngPredictor.Decoder(predictor, 1, 8, 10_000_000);
            Assert.Equal(10_000_000, decoder.RowLength);
            Assert.Throws<DecodeBuffer.LimitExceededException>(() =>
                new PngPredictor.Decoder(predictor, 1, 8, 10_000_001));
            var dictionary = StreamDictionary(NameToken.FlateDecode,
                new[] { (NameToken.Predictor, predictor), (NameToken.Columns, 10_000_001) });
            Assert.Throws<DecodeBuffer.LimitExceededException>(() =>
                new FlateFilter().Decode(Compress(new byte[] { 42 }), dictionary, TestFilterProvider.Instance, 0));
        }

        [Fact]
        public void KeepsLegitimateRepeatedFiltersWhileApplyingTheirLimits()
        {
            var data = Enumerable.Repeat((byte)42, 4096).ToArray();
            var encoded = Compress(Compress(data));
            var decoded = FilterChainDecoder.Decode(encoded, Dictionary(), TestFilterProvider.Instance,
                new IFilter[] { new FlateFilter(), new FlateFilter() });
            Assert.Equal(data, decoded.ToArray());
        }

        [Fact]
        public void GrowsFlateImagesPastTheInitialChunkAsRowsArrive()
        {
            const int columns = 1024;
            const int rows = 2048;
            var data = Enumerable.Repeat((byte)42, columns * rows).ToArray();
            var predicted = new byte[data.Length];
            for (var row = 0; row < rows; row++)
            {
                predicted[row * columns] = 42;
            }
            var dictionary = StreamDictionary(NameToken.FlateDecode,
                new[] { (NameToken.Predictor, 2), (NameToken.Columns, columns) },
                (NameToken.Height, rows));
            Assert.Equal(data, new FlateFilter().Decode(Compress(predicted), dictionary,
                TestFilterProvider.Instance, 0).ToArray());
        }


        [Theory]
        [InlineData("Lzw")]
#if NET
        [InlineData("Brotli")]
#endif
        public void ChecksDeclaredPredictorRowsBeforeDecodingTheCompressedPayload(string filter)
        {
            var dictionary = StreamDictionary(NameToken.FlateDecode,
                new[] { (NameToken.Predictor, 15), (NameToken.Columns, 10_000_001) });
            Assert.Throws<DecodeBuffer.LimitExceededException>(() =>
            {
                if (filter == "Lzw")
                {
                    new LzwFilter().Decode(new byte[1], dictionary, TestFilterProvider.Instance, 0);
                }
                else
                {
                    new BrotliFilter().Decode(new byte[1], dictionary, TestFilterProvider.Instance, 0);
                }
            });
        }

        private static byte[] Compress(byte[] data)
        {
            using var output = new MemoryStream();
            output.WriteByte(120);
            output.WriteByte(156);
            using (var deflate = new DeflateStream(output, CompressionLevel.Optimal, true))
            {
                deflate.Write(data, 0, data.Length);
            }
            // FlateFilter skips the zlib header and ignores its checksum.
            output.Write(new byte[4], 0, 4);
            return output.ToArray();
        }

        private sealed class MustNotDecode : IFilter
        {
            public bool IsSupported => true;

            public Memory<byte> Decode(Memory<byte> input, DictionaryToken dictionary, IFilterProvider provider, int index)
                => throw new InvalidOperationException("The rejected stage must stop the chain.");
        }
    }
}
