namespace UglyToad.PdfPig.Tests.Filters
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Security.Cryptography;
    using Integration;
    using PdfPig.Core;
    using PdfPig.Filters;
    using PdfPig.Tokens;

    public class PngPredictorTests
    {
        [Fact]
        public void PredictorBelowTwoReturnsTheDataUntouched()
        {
            var data = new byte[] { 1, 2, 3, 4, 5, 6 };

            Assert.Equal(data, PngPredictor.Decode(data, 1, 1, 8, 3).ToArray());
            Assert.Equal(data, PngPredictor.Decode(data, -1, 1, 8, 3).ToArray());
        }

        [Fact]
        public void EmptyDataDecodesToNothing()
        {
            Assert.Empty(PngPredictor.Decode(Array.Empty<byte>(), 15, 3, 8, 10).ToArray());
        }

        [Fact]
        public void UpAddsTheRowAbove()
        {
            // Two rows of three bytes, both with the Up filter type; the first row has nothing above it.
            var data = new byte[] { 2, 10, 20, 30, 2, 1, 2, 3 };

            var decoded = PngPredictor.Decode(data, 12, 1, 8, 3);

            Assert.Equal(new byte[] { 10, 20, 30, 11, 22, 33 }, decoded.ToArray());
        }

        [Fact]
        public void SubAddsThePixelToTheLeft()
        {
            // One row of two RGB pixels: the second pixel is stored relative to the first.
            var data = new byte[] { 1, 10, 20, 30, 1, 2, 3 };

            var decoded = PngPredictor.Decode(data, 15, 3, 8, 2);

            Assert.Equal(new byte[] { 10, 20, 30, 11, 22, 33 }, decoded.ToArray());
        }

        [Fact]
        public void AverageAndPaethMatchTheDefinitions()
        {
            // Row 1 is a plain row, row 2 uses Average, row 3 uses Paeth, single component, 3 columns.
            // Average: byte + floor((left + up) / 2); Paeth: byte + nearest of left, up, upper-left.
            var data = new byte[]
            {
                0, 10, 20, 30,
                3, 5, 5, 5,
                4, 1, 1, 1
            };

            var decoded = PngPredictor.Decode(data, 15, 1, 8, 3);

            // Average row: 5 + 10/2 = 10; 5 + (10 + 20)/2 = 20; 5 + (20 + 30)/2 = 30.
            // Paeth row: first byte takes up (10) -> 11; then a=11, b=20, c=10: estimate 21, nearest is b -> 21;
            // then a=21, b=30, c=20: estimate 31, nearest is b -> 31.
            Assert.Equal(new byte[] { 10, 20, 30, 10, 20, 30, 11, 21, 31 }, decoded.ToArray());
        }

        [Fact]
        public void IncompleteLastRowIsPaddedWithZerosBeforeDecoding()
        {
            // Second row has only one of its three bytes; the rest count as zero differences.
            var data = new byte[] { 0, 10, 20, 30, 2, 5 };

            var decoded = PngPredictor.Decode(data, 12, 1, 8, 3);

            Assert.Equal(new byte[] { 10, 20, 30, 15, 20, 30 }, decoded.ToArray());
        }

        [Fact]
        public void TrailingFilterTypeByteAloneIsNotARow()
        {
            var data = new byte[] { 0, 10, 20, 30, 2 };

            var decoded = PngPredictor.Decode(data, 12, 1, 8, 3);

            Assert.Equal(new byte[] { 10, 20, 30 }, decoded.ToArray());
        }

        [Fact]
        public void TiffPredictorAddsTheSampleToTheLeft()
        {
            var data = new byte[] { 10, 1, 1, 100, 2, 2 };

            var decoded = PngPredictor.Decode(data, 2, 1, 8, 3);

            Assert.Equal(new byte[] { 10, 11, 12, 100, 102, 104 }, decoded.ToArray());
        }

        public static IEnumerable<object[]> ParameterSets => new[]
        {
            // predictor, colors, bitsPerComponent, columns
            new object[] { 15, 1, 8, 1 },
            new object[] { 15, 1, 8, 37 },
            new object[] { 15, 3, 8, 64 },
            new object[] { 15, 4, 8, 23 },
            new object[] { 15, 1, 1, 100 },
            new object[] { 15, 3, 16, 9 },
            new object[] { 12, 1, 8, 5 },
            new object[] { 11, 1, 8, 5 },
            new object[] { 2, 1, 8, 40 },
            new object[] { 2, 3, 8, 17 },
            new object[] { 2, 1, 16, 12 },
            new object[] { 2, 3, 16, 7 },
            new object[] { 2, 1, 1, 50 },
            new object[] { 2, 1, 2, 33 },
            new object[] { 2, 1, 4, 21 },
            new object[] { 2, 3, 4, 10 },
            new object[] { 2, 3, 2, 10 },
            new object[] { 5, 1, 8, 10 }
        };

        /// <summary>
        /// The rewrite has to give exactly what the row-buffer implementation it replaced gave, for
        /// every predictor and sample width, including the padded last row.
        /// </summary>
        [Theory]
        [MemberData(nameof(ParameterSets))]
        public void MatchesThePreviousImplementation(int predictor, int colors, int bitsPerComponent, int columns)
        {
            var random = new Random((predictor * 1000) + (colors * 100) + bitsPerComponent + columns);
            var rowLength = PngPredictor.CalculateRowLength(colors, bitsPerComponent, columns);
            var stride = predictor >= 10 ? rowLength + 1 : rowLength;

            foreach (var rows in new[] { 1, 2, 7, 50 })
            {
                foreach (var cut in new[] { 0, 1, rowLength / 2, rowLength })
                {
                    var length = (rows * stride) + (cut < stride ? cut : 0);

                    var data = new byte[length];
                    random.NextBytes(data);

                    if (predictor >= 10)
                    {
                        // Filter types 0 to 4 in a mix, with an undefined one thrown in.
                        for (var row = 0; row * stride < data.Length; row++)
                        {
                            data[row * stride] = (byte)(row % 6 == 5 ? 9 : row % 5);
                        }
                    }

                    var expected = LegacyPredictor.Decode((byte[])data.Clone(), predictor, colors, bitsPerComponent, columns);
                    var actual = PngPredictor.Decode(data, predictor, colors, bitsPerComponent, columns);

                    Assert.Equal(expected, actual.ToArray());
                }
            }
        }

        /// <summary>
        /// Fed a piece at a time, in whatever sizes the inflater happens to produce, the incremental
        /// decoder has to end up with exactly what decoding the complete data gives.
        /// </summary>
        [Theory]
        [MemberData(nameof(ParameterSets))]
        public void IncrementalDecodingMatchesDecodingTheWholeBuffer(int predictor, int colors, int bitsPerComponent, int columns)
        {
            var random = new Random((predictor * 7919) + (colors * 131) + bitsPerComponent + columns);
            var rowLength = PngPredictor.CalculateRowLength(colors, bitsPerComponent, columns);
            var stride = predictor >= 10 ? rowLength + 1 : rowLength;

            foreach (var length in new[] { 0, 1, stride, (stride * 13) + (stride / 2), stride * 40 })
            {
                var data = new byte[length];
                random.NextBytes(data);

                if (predictor >= 10)
                {
                    for (var row = 0; row * stride < data.Length; row++)
                    {
                        data[row * stride] = (byte)(row % 5);
                    }
                }

                var expected = PngPredictor.Decode((byte[])data.Clone(), predictor, colors, bitsPerComponent, columns).ToArray();

                // Room for the padded last row, as the filter guarantees before finishing.
                var buffer = new byte[data.Length + rowLength];
                var decoder = new PngPredictor.Decoder(predictor, colors, bitsPerComponent, columns);
                var appended = 0;

                while (appended < data.Length)
                {
                    var chunk = Math.Min(data.Length - appended, random.Next(1, (2 * stride) + 3));
                    Array.Copy(data, appended, buffer, appended, chunk);
                    appended += chunk;

                    decoder.Advance(buffer, appended);
                }

                Assert.True(decoder.FinalLength(appended) <= buffer.Length);

                var actualLength = decoder.Finish(buffer, appended);

                Assert.Equal(expected, buffer.AsSpan(0, actualLength).ToArray());
            }
        }

        /// <summary>
        /// Decoding rows where they lie and gathering them afterwards has to give the same result as
        /// moving them up as they are decoded.
        /// </summary>
        [Theory]
        [MemberData(nameof(ParameterSets))]
        public void DecodingInPlaceAndGatheringMatchesDecodingTheWholeBuffer(int predictor, int colors, int bitsPerComponent, int columns)
        {
            var random = new Random((predictor * 104729) + (colors * 977) + bitsPerComponent + columns);
            var rowLength = PngPredictor.CalculateRowLength(colors, bitsPerComponent, columns);
            var stride = predictor >= 10 ? rowLength + 1 : rowLength;

            foreach (var length in new[] { 0, 1, stride, (stride * 13) + (stride / 2), stride * 40 })
            {
                var data = new byte[length];
                random.NextBytes(data);

                if (predictor >= 10)
                {
                    for (var row = 0; row * stride < data.Length; row++)
                    {
                        data[row * stride] = (byte)(row % 5);
                    }
                }

                var expected = PngPredictor.Decode((byte[])data.Clone(), predictor, colors, bitsPerComponent, columns).ToArray();

                // Room for the padded last row where its input lies, as the filter guarantees before finishing.
                var buffer = new byte[data.Length + stride];
                var decoder = new PngPredictor.Decoder(predictor, colors, bitsPerComponent, columns, compact: false);
                var appended = 0;

                while (appended < data.Length)
                {
                    var chunk = Math.Min(data.Length - appended, random.Next(1, (2 * stride) + 3));
                    Array.Copy(data, appended, buffer, appended, chunk);
                    appended += chunk;

                    decoder.Advance(buffer, appended);
                }

                Assert.True(decoder.RequiredCapacity(appended) <= buffer.Length);

                var actualLength = decoder.Finish(buffer, appended);
                var actual = new byte[actualLength];
                decoder.CopyTo(buffer, actual);

                Assert.Equal(expected, actual);
            }
        }

        /// <summary>
        /// Decoding from a small, recycled input buffer straight into a separate output, as the Flate
        /// filter does for images, has to give the same result as decoding the complete data.
        /// </summary>
        [Theory]
        [MemberData(nameof(ParameterSets))]
        public void DecodingIntoASeparateOutputMatchesDecodingTheWholeBuffer(int predictor, int colors, int bitsPerComponent, int columns)
        {
            var random = new Random((predictor * 7907) + (colors * 613) + bitsPerComponent + columns);
            var rowLength = PngPredictor.CalculateRowLength(colors, bitsPerComponent, columns);
            var stride = predictor >= 10 ? rowLength + 1 : rowLength;

            foreach (var length in new[] { 0, 1, stride, (stride * 13) + (stride / 2), stride * 40 })
            {
                var data = new byte[length];
                random.NextBytes(data);

                if (predictor >= 10)
                {
                    for (var row = 0; row * stride < data.Length; row++)
                    {
                        data[row * stride] = (byte)(row % 5);
                    }
                }

                var expected = PngPredictor.Decode((byte[])data.Clone(), predictor, colors, bitsPerComponent, columns).ToArray();

                var decoder = new PngPredictor.Decoder(predictor, colors, bitsPerComponent, columns);
                var input = new byte[stride + 7];
                var output = new byte[((data.Length / stride) + 1) * rowLength];
                var offset = 0;
                var buffered = 0;

                while (offset < data.Length)
                {
                    var chunk = Math.Min(Math.Min(data.Length - offset, input.Length - buffered), random.Next(1, stride + 4));
                    Array.Copy(data, offset, input, buffered, chunk);
                    offset += chunk;
                    buffered += chunk;

                    decoder.Advance(input, buffered, output);

                    var tail = buffered - decoder.ConsumedLength;
                    Array.Copy(input, decoder.ConsumedLength, input, 0, tail);
                    buffered = tail;
                    decoder.RestartInput();
                }

                var actualLength = decoder.Finish(input, buffered, output);

                Assert.Equal(expected, output.AsSpan(0, actualLength).ToArray());
            }
        }

        /// <summary>
        /// Streams with predictors from real producers, with the content they decoded to before the
        /// rewrite: PNG predictors on RGB, CMYK, grey and 1-bit images, the TIFF predictor on grey
        /// images, and the PNG Up predictor on cross-reference streams.
        /// </summary>
        public static IEnumerable<object[]> RealDocumentStreams => new[]
        {
            new object[] { "Pig Production Handbook.pdf", 138, 25340, "C7EE9E5BF28B6456E377C18E7C2F7B73A29D2E79871B46D76EF3059F4EB073EA" },
            new object[] { "Pig Production Handbook.pdf", 266, 80010, "C31D5910998C5FEAD0F67CF87B483072B232CEB9A8B7ED204EFF20B9319B2028" },
            new object[] { "Pig Production Handbook.pdf", 296, 128271, "F97BED0E2F2424A0437284AF84CE9C175AD82701CFD857D057B2A5A6A9DEED3E" },
            new object[] { "Pig Production Handbook.pdf", 300, 128271, "F09CCE7852432D45FFE0D9ABCC8B0E14C20F79B61CA2C2543437BABC257B66D4" },
            new object[] { "Pig Production Handbook.pdf", 302, 130050, "D835E85933ADE868EEA73BD25E888B336C2F6259CA92F08981D8DF807D96F201" },
            new object[] { "Pig Production Handbook.pdf", 316, 46810, "C46A2BDDBF3F7AAF0DBE27EB34D2749D0D41F5D30BA1A3695DA001571D7ADEB7" },
            new object[] { "Pig Production Handbook.pdf", 431, 90300, "1FAF5F1A9AFF6D8AB09F6F82DECCEF8A45BEA95446F2D32B5E8888EC4D69AC64" },
            new object[] { "Pig Production Handbook.pdf", 447, 364320, "DB9EE3EF5434EA2CEC3F55D5CD6863404EB2406995A2DBAB5F36AD7D99E88306" },
            new object[] { "Pig Production Handbook.pdf", 457, 543780, "378EA4F613B1F898903DC3CA5AAC8EA071175003A0620DDB93EBC34062CA0633" },
            new object[] { "Pig Production Handbook.pdf", 641, 31476, "1000A15CB3629CCE012701A4AF070C5F596EA2FBDD6FF1F4169F3E303E9612C6" },
            new object[] { "Pig Production Handbook.pdf", 654, 130050, "03F473386F846C0B0EC7AC5A11E4E168B24B11345ECA0840960BA45A80BA8144" },
            new object[] { "Pig Production Handbook.pdf", 854, 11160, "ED02A11768649FB9E580FA2612FB6FD92622DB6368E845EE3A068EEBCC79730B" },
            new object[] { "Pig Production Handbook.pdf", 855, 10800, "5F2F528846B0465C5FCC28D7BCBACD78F4648F13A925167C565D19CE2A27163D" },
            new object[] { "Pig Production Handbook.pdf", 858, 3720, "0932C63A769EA57B7FE4864CAF28C609C58047E036A14FD3D4B83BE7E5A1F7E3" },
            new object[] { "Pig Production Handbook.pdf", 861, 3600, "1170F44C4B8E1D5925BBF947C92B7157A6C4CCBBEF0C9C077E1CABC42ACB061F" },
            new object[] { "GHOSTSCRIPT-697234-0.pdf", 10, 453504, "CECBA85D9CEFC3ECA14FA9C9E9214C81AB6692EBA1E92102ECF9FDF5167812C9" },
            new object[] { "GHOSTSCRIPT-697234-0.pdf", 11, 158780, "7283EE45D13FD7005819A587AFE77E5E517366AC923CB449155E10CDDC94E01D" },
            new object[] { "GHOSTSCRIPT-697234-0.pdf", 12, 95480, "A6D163C28CAB22607CF9C1405E66D1E76794BB0B15FCED53E55D10B953018D58" },
            new object[] { "GHOSTSCRIPT-697234-0.pdf", 13, 1596540, "EBFEE1637A06A2539BAA06A436892F0A9B413FD13D441A83E50B53470A7E8024" },
            new object[] { "GHOSTSCRIPT-697234-0.pdf", 14, 320620, "77FF0FABC79A3DBA9CDD59BEBE471F33A04EBF71B66F692083AD32F1A13797B3" },
            new object[] { "GHOSTSCRIPT-697234-0.pdf", 135, 158780, "844B811C74A1431F3D0D800F9AA018060E410819AE8996FF5655292C77075FA7" },
            new object[] { "GHOSTSCRIPT-697234-0.pdf", 136, 66420, "303702D4DF2177229EAC554E126C6E19CF9A9C728DF2A380C588F1C9768DAD6C" },
            new object[] { "GHOSTSCRIPT-697234-0.pdf", 137, 1617040, "E244C694082FA0B0FEE416EA5D26F36AF1A8158BE064DFB03E0F66EB9134DCBB" },
            new object[] { "GHOSTSCRIPT-697234-0.pdf", 138, 235340, "AC9D9139B8B429990249171190E721EBC03EEF771CEAF939F212C87D4B5CEBAF" },
            new object[] { "GHOSTSCRIPT-697234-0.pdf", 140, 51240, "92F85263F9431634FCA5812C2F5CA50342AA5E24C3584A05002DC009378C936D" },
            new object[] { "GHOSTSCRIPT-697234-0.pdf", 220, 140760, "12DA1B79FE37528C745E7F16088E68E18CB6EBDC5DBD46521EFC2397C4F48072" },
            new object[] { "GHOSTSCRIPT-697234-0.pdf", 221, 336384, "6E2C3D6E8572C63A02B716E6BBB95A986ACE375BCA763E3B18A968F47778256C" },
            new object[] { "GHOSTSCRIPT-697234-0.pdf", 222, 142800, "FE6A0B53CF43F65B1F47001B6B180BBD6529A5AC3268FDEAFE369E3BB0BB1401" },
            new object[] { "GHOSTSCRIPT-697234-0.pdf", 269, 263848, "7A39B0E0985E2F2B165553CECA12E8D2F796B667F1747C0BB51319825D96DB0E" },
            new object[] { "GHOSTSCRIPT-697234-0.pdf", 270, 89424, "1FEB58AC00DEDA98C002FC777F2B886928125A5EAAABB44EE0E9DE213DC391DD" },
            new object[] { "GHOSTSCRIPT-697234-0.pdf", 271, 140760, "6DB68E517938E64A3B33B40E255D63DEF6545782751F3E65299D208863D9F238" },
            new object[] { "GHOSTSCRIPT-697234-0.pdf", 273, 87480, "F3EEF9EF1F6BA288C72673C609E2F41777A1016C39285A027E52726D05E986A1" },
            new object[] { "GHOSTSCRIPT-697234-0.pdf", 274, 334048, "EA82E4B0DBB6E26B792C98B3A1E9F564D6E777DA4A88AC73AD2EB8D759BBEC3D" },
            new object[] { "TIKA-1552-0.pdf", 569, 95760, "4C61FA43F59DBD102DE29F83800EC56F00B1A347499099667CF8AE6347470D36" },
            new object[] { "TIKA-1552-0.pdf", 572, 29165, "6CA5D4D110470257E320A7D26B2F488C368FD23613579DFAAC3B20DBE8F42D19" },
            new object[] { "TIKA-1552-0.pdf", 573, 31620, "7AF8BC33D2D83C5F2589B535B35900E185ECACB79D903B765AD634546B020D54" },
            new object[] { "TIKA-1552-0.pdf", 574, 73903, "01303747B6A4250EB1547B5911483212EDD3A52B2F2706C38E0F0D5E9FC02391" },
            new object[] { "TIKA-1552-0.pdf", 575, 4320, "75445EF5BB45B9A028E9D98BAC075E60A2719D9BB1E66F2722D878DEA88287AA" },
            new object[] { "TIKA-1552-0.pdf", 576, 23885, "D95FE58D4CB722911594997E453CE1896CFD121CABD4AA077CB2BF3D9B6C4ACB" },
            new object[] { "TIKA-1552-0.pdf", 1303, 6520, "0704040C99EE94A9B411B0FAE71C1D7E855AA3EB114062BEDF0098E028B3BD97" },
            new object[] { "TIKA-1552-0.pdf", 1307, 480, "1566E08E1E3EA0CB6F7F3C962C9AE5C4B094B0BFDC78F3F93D8DBB549B8AAB8C" },
            new object[] { "MOZILLA-10225-0.pdf", 237, 722976, "C151FA96A810EF9BE7034DC5790B7A5C28EBC58914C1E83A670C55B19AF3F5A6" },
            new object[] { "MOZILLA-10225-0.pdf", 395, 1089000, "902A0072FF7F46083C8EAA585EE664313926B8C695C50599C8ADE7CF0179DB7A" },
            new object[] { "MOZILLA-10225-0.pdf", 421, 573783, "D68C24C3979AB98CC9F7232123F85A9EE6CA44A7ADFED75A369C392017DF495A" },
            new object[] { "MOZILLA-10225-0.pdf", 704, 1148094, "9FD5FF121CB6DAB71F8E912A6661906F7431D4CB5FC7F999D5946D327503044D" },
            new object[] { "P2P-33713919.pdf", 35, 1143677, "CEB1055CE191F49527E64A3FA3CE08E6246CB28D3E6BD4106CFAE754B0C31961" },
            new object[] { "P2P-33713919.pdf", 37, 517440, "93E46ACF1D8C48EE1D139F45EB82B5E45E4D813D3448B5B5FCB5A01C019FBDF9" },
            new object[] { "GHOSTSCRIPT-700931-0.7z-5.pdf", 10, 76200, "EB35ADF0D7A3A1FD3DCDA6906E82732F0A4C2E0C1AA61309535E5B6A02ED8908" },
            new object[] { "GHOSTSCRIPT-700931-0.7z-5.pdf", 21, 60800, "CD9F8B24B9C2A1A5C015BE717C797C5836EF72EAA233F3084BC62A5EB2001B2B" }
        };

        [Theory]
        [MemberData(nameof(RealDocumentStreams))]
        public void DecodesRealDocumentStreamsToTheKnownContent(string documentName, int objectNumber, int expectedLength, string expectedSha256)
        {
            using var document = PdfDocument.Open(IntegrationHelpers.GetDocumentPath(documentName), new ParsingOptions { UseLenientParsing = true });

            var stream = Assert.IsType<StreamToken>(document.Structure.GetObject(new IndirectReference(objectNumber, 0)).Data);

            var decoded = stream.Decode(DefaultFilterProvider.Instance);

            Assert.Equal(expectedLength, decoded.Length);

            using var sha256 = SHA256.Create();
            var hash = BitConverter.ToString(sha256.ComputeHash(decoded.ToArray())).Replace("-", string.Empty);

            Assert.Equal(expectedSha256, hash);
        }

        /// <summary>
        /// The implementation before the rewrite, kept here as the oracle: a stream that gathers each
        /// row into a buffer, decodes it against a copy of the previous row and writes it on.
        /// </summary>
        private static class LegacyPredictor
        {
            public static byte[] Decode(byte[] data, int predictor, int colors, int bitsPerComponent, int columns)
            {
                if (predictor <= 1)
                {
                    return data;
                }

                using var output = new MemoryStream();
                var rowLength = ((columns * colors * bitsPerComponent) + 7) / 8;
                var perRow = predictor >= 10;
                var currentRow = new byte[rowLength];
                var lastRow = new byte[rowLength];
                var currentRowData = 0;
                var predictorRead = false;
                var rowPredictor = predictor;

                var offset = 0;
                while (offset < data.Length)
                {
                    if (perRow && currentRowData == 0 && !predictorRead)
                    {
                        rowPredictor = data[offset] + 10;
                        offset++;
                        predictorRead = true;
                    }
                    else
                    {
                        var toRead = Math.Min(rowLength - currentRowData, data.Length - offset);
                        Array.Copy(data, offset, currentRow, currentRowData, toRead);
                        currentRowData += toRead;
                        offset += toRead;

                        if (currentRowData == rowLength)
                        {
                            DecodeRow(rowPredictor, colors, bitsPerComponent, columns, currentRow, lastRow);
                            output.Write(currentRow, 0, rowLength);
                            (lastRow, currentRow) = (currentRow, lastRow);
                            currentRowData = 0;
                            predictorRead = false;
                        }
                    }
                }

                if (currentRowData > 0)
                {
                    Array.Clear(currentRow, currentRowData, rowLength - currentRowData);
                    DecodeRow(rowPredictor, colors, bitsPerComponent, columns, currentRow, lastRow);
                    output.Write(currentRow, 0, rowLength);
                }

                return output.ToArray();
            }

            private static void DecodeRow(int predictor, int colors, int bitsPerComponent, int columns, byte[] actline, byte[] lastline)
            {
                if (predictor == 1)
                {
                    return;
                }

                var bitsPerPixel = colors * bitsPerComponent;
                var bytesPerPixel = (bitsPerPixel + 7) / 8;
                var rowLength = actline.Length;

                switch (predictor)
                {
                    case 2:
                        if (bitsPerComponent == 8)
                        {
                            for (var p = bytesPerPixel; p < rowLength; p++)
                            {
                                actline[p] = (byte)(actline[p] + actline[p - bytesPerPixel]);
                            }
                        }
                        else if (bitsPerComponent == 16)
                        {
                            for (var p = bytesPerPixel; p < rowLength - 1; p += 2)
                            {
                                var sub = (actline[p] << 8) + actline[p + 1];
                                var left = (actline[p - bytesPerPixel] << 8) + actline[p - bytesPerPixel + 1];
                                var sum = sub + left;
                                actline[p] = (byte)((sum >> 8) & 0xff);
                                actline[p + 1] = (byte)(sum & 0xff);
                            }
                        }
                        else if (bitsPerComponent == 1 && colors == 1)
                        {
                            for (var p = 0; p < rowLength; p++)
                            {
                                for (var bit = 7; bit >= 0; --bit)
                                {
                                    var sub = (actline[p] >> bit) & 1;
                                    if (p == 0 && bit == 7)
                                    {
                                        continue;
                                    }

                                    var left = bit == 7 ? actline[p - 1] & 1 : (actline[p] >> (bit + 1)) & 1;

                                    if (((sub + left) & 1) == 0)
                                    {
                                        actline[p] &= (byte)~(1 << bit);
                                    }
                                    else
                                    {
                                        actline[p] |= (byte)(1 << bit);
                                    }
                                }
                            }
                        }
                        else
                        {
                            var elements = columns * colors;
                            for (var p = colors; p < elements; ++p)
                            {
                                var bytePosSub = p * bitsPerComponent / 8;
                                var bitPosSub = 8 - (p * bitsPerComponent % 8) - bitsPerComponent;
                                var bytePosLeft = (p - colors) * bitsPerComponent / 8;
                                var bitPosLeft = 8 - ((p - colors) * bitsPerComponent % 8) - bitsPerComponent;

                                var mask = (1 << bitsPerComponent) - 1;
                                var sub = (actline[bytePosSub] >> bitPosSub) & mask;
                                var left = (actline[bytePosLeft] >> bitPosLeft) & mask;
                                var truncated = (sub + left) & mask;
                                actline[bytePosSub] = (byte)((actline[bytePosSub] & ~(mask << bitPosSub)) | (truncated << bitPosSub));
                            }
                        }

                        break;
                    case 11:
                        for (var p = bytesPerPixel; p < rowLength; p++)
                        {
                            actline[p] = (byte)(actline[p] + actline[p - bytesPerPixel]);
                        }

                        break;
                    case 12:
                        for (var p = 0; p < rowLength; p++)
                        {
                            actline[p] = (byte)(actline[p] + lastline[p]);
                        }

                        break;
                    case 13:
                        for (var p = 0; p < rowLength; p++)
                        {
                            var left = p - bytesPerPixel >= 0 ? actline[p - bytesPerPixel] : 0;
                            actline[p] = (byte)(actline[p] + ((left + lastline[p]) / 2));
                        }

                        break;
                    case 14:
                        for (var p = 0; p < rowLength; p++)
                        {
                            var a = p - bytesPerPixel >= 0 ? actline[p - bytesPerPixel] : 0;
                            var b = lastline[p];
                            var c = p - bytesPerPixel >= 0 ? lastline[p - bytesPerPixel] : 0;
                            var value = a + b - c;
                            var absa = Math.Abs(value - a);
                            var absb = Math.Abs(value - b);
                            var absc = Math.Abs(value - c);

                            if (absa <= absb && absa <= absc)
                            {
                                actline[p] = (byte)(actline[p] + a);
                            }
                            else if (absb <= absc)
                            {
                                actline[p] = (byte)(actline[p] + b);
                            }
                            else
                            {
                                actline[p] = (byte)(actline[p] + c);
                            }
                        }

                        break;
                    default:
                        break;
                }
            }
        }
    }
}
