namespace UglyToad.PdfPig.Tests.Integration
{
    using System.Text;
    using PdfPig.Fonts;

    public class MaxDecompressedStreamBytesTests
    {
        // "BT ET" as ASCIIHexDecode: a content stream that decodes to five bytes.
        private const string Content = "<4254204554>";
        private const int DecodedLength = 5;

        [Fact]
        public void ReadsAStreamThatDecodesToTheLimit()
        {
            using var document = PdfDocument.Open(Document(), new ParsingOptions { MaxDecompressedStreamBytes = DecodedLength });

            Assert.Empty(document.GetPage(1).Letters);
        }

        [Fact]
        public void RefusesAStreamThatDecodesToMoreThanTheLimit()
        {
            using var document = PdfDocument.Open(Document(), new ParsingOptions { MaxDecompressedStreamBytes = DecodedLength - 1 });

            Assert.ThrowsAny<CorruptCompressedDataException>(() => document.GetPage(1));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void RejectsALimitThatIsNotPositive(int limit)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                PdfDocument.Open(Document(), new ParsingOptions { MaxDecompressedStreamBytes = limit }));
        }

        private static byte[] Document()
        {
            var objects = new[]
            {
                "<< /Type /Catalog /Pages 2 0 R >>",
                "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
                "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 200 200] /Contents 4 0 R >>",
                $"<< /Length {Content.Length} /Filter /ASCIIHexDecode >>\nstream\n{Content}\nendstream"
            };

            var builder = new StringBuilder("%PDF-1.4\n");
            var offsets = new int[objects.Length];
            for (var i = 0; i < objects.Length; i++)
            {
                offsets[i] = builder.Length;
                builder.Append(i + 1).Append(" 0 obj\n").Append(objects[i]).Append("\nendobj\n");
            }

            var xref = builder.Length;
            builder.Append("xref\n0 ").Append(objects.Length + 1).Append("\n0000000000 65535 f \n");
            foreach (var offset in offsets)
            {
                builder.Append(offset.ToString("D10")).Append(" 00000 n \n");
            }

            builder.Append("trailer\n<< /Size ").Append(objects.Length + 1).Append(" /Root 1 0 R >>\nstartxref\n")
                .Append(xref).Append("\n%%EOF\n");

            return Encoding.ASCII.GetBytes(builder.ToString());
        }
    }
}
