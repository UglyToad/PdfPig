using UglyToad.PdfPig.Tests.Integration;
using UglyToad.PdfPig.Tokens;
using UglyToad.PdfPig.Writer;

namespace UglyToad.PdfPig.Tests.Writer
{
    public class PdfTextRemoverTests
    {
        [Fact]
        public void StreamsFromAesSourceHaveCorrectLength()
        {
            // AES decryption strips the IV and padding, so the source /Length is too long.
            var bytes = PdfTextRemover.RemoveText(IntegrationHelpers.GetSpecificTestDocumentPath("r4_aesv2_no_length.pdf"));

            using (var document = PdfDocument.Open(bytes, ParsingOptions.LenientParsingOff))
            {
                var streamCount = 0;
                foreach (var offset in document.Structure.CrossReferenceTable.ObjectOffsets)
                {
                    if (document.Structure.GetObject(offset.Key).Data is not StreamToken stream)
                    {
                        continue;
                    }

                    var length = stream.StreamDictionary.Get<NumericToken>(NameToken.Length, document.Structure.TokenScanner);
                    Assert.Equal(stream.Data.Length, length.Int);
                    streamCount++;
                }

                Assert.True(streamCount > 0);
            }
        }

        [Theory]
        [InlineData("Two Page Text Only - from libre office.pdf")]
        [InlineData("cat-genetics.pdf")]
        [InlineData("Motor Insurance claim form.pdf")]
        [InlineData("Single Page Images - from libre office.pdf")]
        public void TextRemoverRemovesText(string file)
        {
            var filePath = IntegrationHelpers.GetDocumentPath(file);
            using (var document = PdfDocument.Open(filePath))
            {
                var withoutText = PdfTextRemover.RemoveText(filePath);
                WriteFile($"{nameof(TextRemoverRemovesText)}_{file}", withoutText);

                using (var documentWithoutText = PdfDocument.Open(withoutText))
                {
                    Assert.Equal(document.NumberOfPages, documentWithoutText.NumberOfPages);
                    for (var i = 1; i <= documentWithoutText.NumberOfPages; i++)
                    {
                        Assert.NotEqual(document.GetPage(i).Text, string.Empty);
                        Assert.Equal(documentWithoutText.GetPage(i).Text, string.Empty);
                    }
                }
            }
        }

        private static void WriteFile(string name, byte[] bytes)
        {
            try
            {
                if (!Directory.Exists("Writer"))
                {
                    Directory.CreateDirectory("Writer");
                }

                var output = Path.Combine("Writer", $"{name}");

                File.WriteAllBytes(output, bytes);
            }
            catch
            {
                // ignored.
            }
        }
    }
}
