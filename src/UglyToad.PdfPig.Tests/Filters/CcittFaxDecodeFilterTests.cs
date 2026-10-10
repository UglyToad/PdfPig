namespace UglyToad.PdfPig.Tests.Filters
{
    using System.Globalization;
    using System.Text;
    using UglyToad.PdfPig.Fonts;
    using UglyToad.PdfPig.Filters;
    using UglyToad.PdfPig.Tests.Images;
    using UglyToad.PdfPig.Tokens;

    public class CcittFaxDecodeFilterTests
    {
        [Theory]
        [InlineData(0, 1)]
        [InlineData(-1, 1)]
        [InlineData(1, 0)]
        [InlineData(1, -1)]
        [InlineData(1728, int.MaxValue)]
        [InlineData(int.MaxValue, 1)]
        [InlineData(int.MaxValue, int.MaxValue)]
        [InlineData(40_000_000, 1)] // The bitmap fits, but the change arrays exceed the limit.
        public void RejectsUnsafeDimensionsBeforeReadingInput(int columns, int rows)
        {
            var parameters = new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                { NameToken.Columns, new NumericToken(columns) },
                { NameToken.Rows, new NumericToken(rows) }
            });
            var dictionary = new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                { NameToken.DecodeParms, parameters }
            });

            // Empty input would fail during compression sniffing if validation ran too late.
            Assert.Throws<CorruptCompressedDataException>(() =>
                new CcittFaxDecodeFilter().Decode(Memory<byte>.Empty, dictionary, TestFilterProvider.Instance, 0));
        }

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
                { NameToken.DecodeParms, parameters },
                { inlineHeight ? NameToken.H : NameToken.Height, new NumericToken(int.MaxValue) }
            });

            Assert.Throws<CorruptCompressedDataException>(() =>
                new CcittFaxDecodeFilter().Decode(Memory<byte>.Empty, dictionary, TestFilterProvider.Instance, 0));
        }

        [Fact]
        public void IncludesChangeArraysAtTheLimit()
        {
            // One byte per row plus two three-entry int arrays; equality is allowed.
            const int lastAllowedRowCount = 256 * 1024 * 1024 - 24;
            Assert.Equal(lastAllowedRowCount, CcittFaxDecodeFilter.GetDecodedBufferSize(1, lastAllowedRowCount));
            Assert.Throws<CorruptCompressedDataException>(() =>
                CcittFaxDecodeFilter.GetDecodedBufferSize(1, lastAllowedRowCount + 1));
        }

        [Theory]
        [InlineData(1, 1, 1)]
        [InlineData(8, 3, 3)]
        [InlineData(9, 3, 6)]
        [InlineData(1800, 3113, 700425)]
        public void CalculatesRoundedBitmapSize(int columns, int rows, int expected)
        {
            Assert.Equal(expected, CcittFaxDecodeFilter.GetDecodedBufferSize(columns, rows));
        }

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
            // Measuring only the decode call avoids charging document parsing to this guard.
            Assert.InRange(GC.GetAllocatedBytesForCurrentThread() - allocatedBefore, 0L, 1024L * 1024);
#endif
            Assert.StartsWith("CCITT decode buffers require ", exception.Message);
        }

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

        [Fact]
        public void CanDecodeCCittFaxCompressedImageData()
        {
            var encodedBytes = ImageHelpers.LoadFileBytes("ccittfax-encoded.bin");

            var filter = new CcittFaxDecodeFilter();
            var dictionary = new Dictionary<NameToken, IToken>
            {
                { NameToken.D, new ArrayToken(new []{ new NumericToken(1), new NumericToken(0) })},
                { NameToken.W, new NumericToken(1800) },
                { NameToken.H, new NumericToken(3113) },
                { NameToken.Bpc, new NumericToken(1) },
                { NameToken.F, NameToken.CcittfaxDecode },
                { NameToken.DecodeParms,
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
            var decodedBytes = filter.Decode(encodedBytes, new DictionaryToken(dictionary), TestFilterProvider.Instance, 0);
            Assert.Equal(expectedBytes, decodedBytes.ToArray());
        }
    }
}
