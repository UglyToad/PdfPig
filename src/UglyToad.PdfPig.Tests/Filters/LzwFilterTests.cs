namespace UglyToad.PdfPig.Tests.Filters
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;
    using Integration;
    using PdfPig.Core;
    using PdfPig.Filters;
    using PdfPig.Tokens;

    public class LzwFilterTests
    {
        private readonly LzwFilter filter = new LzwFilter();

        private static readonly DictionaryToken NoParameters = new DictionaryToken(new Dictionary<NameToken, IToken>());

        // The worked example of ISO 32000-1 section 7.4.4.2: the codes 256 45 258 258 65 259 66 257 packed
        // into 9 bits each, standing for the ten bytes below. It contains a code that refers to the entry
        // the decoder is about to add (the second 258), which is the awkward case of the algorithm.
        private static readonly byte[] SpecificationExampleEncoded = { 0x80, 0x0B, 0x60, 0x50, 0x22, 0x0C, 0x0C, 0x85, 0x01 };
        private static readonly byte[] SpecificationExampleDecoded = { 45, 45, 45, 45, 45, 65, 45, 45, 45, 66 };

        [Fact]
        public void DecodesTheSpecificationExample()
        {
            var decoded = filter.Decode(SpecificationExampleEncoded, NoParameters, TestFilterProvider.Instance, 0);

            Assert.Equal(SpecificationExampleDecoded, decoded.ToArray());
        }

        [Fact]
        public void DataEndingWithoutEodMarkerKeepsWhatDecoded()
        {
            // Without its last byte the example has all seven content codes and a single bit of the
            // EOD marker. Producers do drop the marker; the content still has to come out.
            var truncated = SpecificationExampleEncoded.Take(SpecificationExampleEncoded.Length - 1).ToArray();

            var decoded = filter.Decode(truncated, NoParameters, TestFilterProvider.Instance, 0);

            Assert.Equal(SpecificationExampleDecoded, decoded.ToArray());
        }

        [Fact]
        public void CodeOutsideTheTableWithNoPrecedingSequenceThrows()
        {
            // 256 (clear) then 300, which nothing before it could have defined.
            var input = new byte[] { 0x80, 0x4B, 0x00 };

            Assert.Throws<InvalidOperationException>(() => filter.Decode(input, NoParameters, TestFilterProvider.Instance, 0));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void RoundTripsText(bool earlyChange)
        {
            var data = Encoding.ASCII.GetBytes(string.Concat(Enumerable.Repeat("Hello LZW, hello PDF. ", 200)));

            var encoded = LzwEncoder.Encode(data, earlyChange, out _);

            var decoded = filter.Decode(encoded, WithEarlyChange(earlyChange), TestFilterProvider.Instance, 0);

            Assert.Equal(data, decoded.ToArray());
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void RoundTripsThroughTwelveBitCodesAndATableReset(bool earlyChange)
        {
            // Enough distinct sequences to fill the table and force a clear code, so every code width
            // and the reset are exercised.
            var data = PseudoRandom(60_000, 40, seed: 17);

            var encoded = LzwEncoder.Encode(data, earlyChange, out var clearCount);

            Assert.True(clearCount > 0, "The sample did not fill the code table, so the test does not cover a reset.");

            var decoded = filter.Decode(encoded, WithEarlyChange(earlyChange), TestFilterProvider.Instance, 0);

            Assert.Equal(data, decoded.ToArray());
        }

        [Fact]
        public void AppliesPngPredictor()
        {
            const int columns = 6;
            const int rows = 4;

            var expected = new byte[columns * rows];
            for (var i = 0; i < expected.Length; i++)
            {
                expected[i] = (byte)(i * 7);
            }

            // Every row carries the PNG "Up" filter type then the difference to the row above.
            var predicted = new byte[rows * (columns + 1)];
            for (var row = 0; row < rows; row++)
            {
                predicted[row * (columns + 1)] = 2;

                for (var i = 0; i < columns; i++)
                {
                    var above = row == 0 ? (byte)0 : expected[((row - 1) * columns) + i];
                    predicted[(row * (columns + 1)) + 1 + i] = (byte)(expected[(row * columns) + i] - above);
                }
            }

            var streamDictionary = new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                { NameToken.Filter, NameToken.LzwDecode },
                {
                    NameToken.DecodeParms, new DictionaryToken(new Dictionary<NameToken, IToken>
                    {
                        { NameToken.Predictor, new NumericToken(12) },
                        { NameToken.Columns, new NumericToken(columns) }
                    })
                }
            });

            var decoded = filter.Decode(LzwEncoder.Encode(predicted, true, out _), streamDictionary, TestFilterProvider.Instance, 0);

            Assert.Equal(expected, decoded.ToArray());
        }

        /// <summary>
        /// Streams from real producers with the content they decoded to before the table was
        /// rewritten. byz.pdf packs its stream in ASCII85 as well, so the whole chain is run.
        /// </summary>
        public static IEnumerable<object[]> RealDocumentStreams => new[]
        {
            new object[] { "ssm2163.pdf", 3, 19587, "AEA3FE889445AA23DFEAE7ECFEE97FEBE84F5DC01E323F15EC66EC040E8B712F" },
            new object[] { "ssm2163.pdf", 16, 6945, "E440EDCF98BA2D322121E4668C40E52DA88D90BD52D2619C48E9267F95310854" },
            new object[] { "ssm2163.pdf", 19, 12579, "DC39D8B8274FBA9F475C10DB936BFED4D829FE3360510AC9FE6DC2D63A898F92" },
            new object[] { "ssm2163.pdf", 24, 9347, "AE0C6E8D914249A9DC98726566B92C26AC4337797F764DC405F807B4BBA1847D" },
            new object[] { "ssm2163.pdf", 27, 27570, "C2963EC81E5ED1DE93C65CB99EA8953F77DB984C0835CDE4DE1ADA8FC0EFF6D7" },
            new object[] { "ssm2163.pdf", 33, 13173, "A89BE7C55695BA9DEB7E02D71DF64856FF7DCB2BFCCAAFE1F7B7CA1771CC3D05" },
            new object[] { "ssm2163.pdf", 36, 22718, "9B4DC2B7C954AB121E94D4385E2860990D7ADFC329D91547C37988D7B245219C" },
            new object[] { "ssm2163.pdf", 41, 82546, "7383FA7FE851A13878A820CBCC634FEF8762423F783A605A58D89DF45124F616" },
            new object[] { "ssm2163.pdf", 44, 153027, "5D20A2C951F66C49EC133551B05DEFA9607D71A4E559B3EBD7DEA3FD98E1E370" },
            new object[] { "ssm2163.pdf", 47, 29876, "5C6914A9AB4C5482535BAD60A768F26BF5FA57D2363EC2E738D6173E929F9830" },
            new object[] { "ssm2163.pdf", 50, 39354, "7CBF00DA1467FD33A614705F03D6705478FB7EC3591F55C78D2F28DAF8E31DEB" },
            new object[] { "ssm2163.pdf", 56, 34548, "2939102A19268C4D35B8E7F49870D1E629348EE2654941BBDF4B677ADF4025FC" },
            new object[] { "ssm2163.pdf", 59, 51554, "EF444AD7EB3C413A1C0A6F22AC06C0C4CC6A7F38669BF69A3E4719E07E3229C7" },
            new object[] { "ssm2163.pdf", 63, 19293, "3287543A2AB3D3C6395445EC893A0535898767FCD97982A9202623D34717CF2B" },
            new object[] { "ssm2163.pdf", 66, 6180, "9317956768D0C8EA229E228225C24232B727F75B0F882991308EDFEAA1E77E9F" },
            new object[] { "ssm2163.pdf", 70, 37135, "426F494630A63D8373C8DF74879E042900E0C1F4D457457A75A38A1C13C2594F" },
            new object[] { "byz.pdf", 17, 768, "79CE1E7C3CAE4365E4697012A92A408C0679BA4E6C83BC28EA47AD0A9501AC92" }
        };

        [Theory]
        [MemberData(nameof(RealDocumentStreams))]
        public void DecodesRealDocumentStreamsToTheKnownContent(string documentName, int objectNumber, int expectedLength, string expectedSha256)
        {
            FilterTestHelpers.AssertStreamDecodesTo(documentName, objectNumber, expectedLength, expectedSha256);
        }

        private static DictionaryToken WithEarlyChange(bool earlyChange)
        {
            return FilterTestHelpers.StreamDictionary(NameToken.LzwDecode, [(NameToken.EarlyChange, earlyChange ? 1 : 0)]);
        }

        private static byte[] PseudoRandom(int length, int alphabet, int seed)
        {
            var random = new Random(seed);
            var data = new byte[length];

            for (var i = 0; i < data.Length; i++)
            {
                data[i] = (byte)random.Next(alphabet);
            }

            return data;
        }

        /// <summary>
        /// A plain LZW encoder for the tests. It tracks the size of the table the decoder will have built
        /// after each code, because that, not the encoder's own table, decides the width of the next code.
        /// </summary>
        private static class LzwEncoder
        {
            private const int ClearTable = 256;
            private const int EodMarker = 257;
            private const int FirstFreeCode = 258;
            private const int MaxCodes = 4096;

            public static byte[] Encode(byte[] data, bool earlyChange, out int clearCount)
            {
                var codeOffset = earlyChange ? 0 : 1;
                var writer = new BitWriter();
                var table = new Dictionary<(int Prefix, byte Next), int>();

                var encoderNext = FirstFreeCode;
                var decoderNext = FirstFreeCode;
                var isFirstAfterClear = true;
                var clears = 0;

                writer.Write(ClearTable, 9);

                var prefix = -1;

                foreach (var b in data)
                {
                    if (prefix < 0)
                    {
                        prefix = b;
                        continue;
                    }

                    if (table.TryGetValue((prefix, b), out var code))
                    {
                        prefix = code;
                        continue;
                    }

                    Emit(prefix);

                    table[(prefix, b)] = encoderNext++;
                    prefix = b;

                    if (encoderNext == MaxCodes)
                    {
                        writer.Write(ClearTable, Width(decoderNext, codeOffset));
                        clears++;

                        table.Clear();
                        encoderNext = FirstFreeCode;
                        decoderNext = FirstFreeCode;
                        isFirstAfterClear = true;
                    }
                }

                if (prefix >= 0)
                {
                    Emit(prefix);
                }

                writer.Write(EodMarker, Width(decoderNext, codeOffset));

                clearCount = clears;

                return writer.ToArray();

                void Emit(int code)
                {
                    writer.Write(code, Width(decoderNext, codeOffset));

                    // The decoder adds an entry for every code except the first after a clear.
                    if (isFirstAfterClear)
                    {
                        isFirstAfterClear = false;
                    }
                    else if (decoderNext < MaxCodes)
                    {
                        decoderNext++;
                    }
                }
            }

            /// <summary>The width the decoder reads the next code with, given the size its table has reached.</summary>
            private static int Width(int decoderNext, int codeOffset)
            {
                if (decoderNext >= 2047 + codeOffset)
                {
                    return 12;
                }

                if (decoderNext >= 1023 + codeOffset)
                {
                    return 11;
                }

                if (decoderNext >= 511 + codeOffset)
                {
                    return 10;
                }

                return 9;
            }

            private sealed class BitWriter
            {
                private readonly List<byte> bytes = new List<byte>();
                private ulong accumulator;
                private int bitCount;

                public void Write(int value, int bits)
                {
                    accumulator = (accumulator << bits) | (uint)value;
                    bitCount += bits;

                    while (bitCount >= 8)
                    {
                        bitCount -= 8;
                        bytes.Add((byte)(accumulator >> bitCount));
                    }
                }

                public byte[] ToArray()
                {
                    if (bitCount > 0)
                    {
                        bytes.Add((byte)(accumulator << (8 - bitCount)));
                        bitCount = 0;
                    }

                    return bytes.ToArray();
                }
            }
        }
    }
}
