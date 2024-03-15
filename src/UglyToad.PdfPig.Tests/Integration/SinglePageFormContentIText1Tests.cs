namespace UglyToad.PdfPig.Tests.Integration
{
    using System.Globalization;
    using System.IO;
    using Content;

    public class SinglePageFormContentIText1Tests
    {
        private static string GetFilename()
        {
            return IntegrationHelpers.GetDocumentPath("Single Page Form Content - from itext 1_1.pdf");
        }

        [Fact]
        public void HasCorrectNumberOfPages()
        {
            var file = GetFilename();

            using (var document = PdfDocument.Open(File.ReadAllBytes(file)))
            {
                Assert.Equal(1, document.NumberOfPages);
            }
        }

        [Fact]
        public void HasCorrectPageSize()
        {
            using (var document = PdfDocument.Open(GetFilename()))
            {
                var page = document.GetPage(1);

                Assert.Equal(PageSize.A4, page.Size);
            }
        }

        [Fact]
        public void ExtractsText()
        {
            using (var document = PdfDocument.Open(GetFilename()))
            {
                var page = document.GetPage(1);

                const string expected = @"Ich bin Marios test.$Anhang=/home/im/tmp/AGB.pdf$$Auftragsnummer=2004001452-001$$Mandant=1$$MailTo=mario@ops.co.at$$User=mario$Ende";

                Assert.Equal(expected, page.Text);
            }
        }

        [Fact]
        public void TextHasCorrectPositions()
        {
            const string positionData = @"   0|I|56.88|774.08
                                             1|c|60.72|774.08
                                             2|h|66.00|774.08
                                            20|$|56.88|744.80
                                            26|g|94.80|744.80
                                            49|$|56.88|730.16";

            var expectedData = positionData.Split(new[] {"\r\n", "\n"}, StringSplitOptions.RemoveEmptyEntries)
                .Select(x =>
                {
                    var stripped = x.Trim();
                    var parts = stripped.Split('|');
                    var index = int.Parse(parts[0]);
                    var xval = double.Parse(parts[2], CultureInfo.InvariantCulture);
                    var yval = double.Parse(parts[3], CultureInfo.InvariantCulture);
                    return (index, letter: parts[1], x: xval, y: yval);
                }).ToArray();

            using (var document = PdfDocument.Open(GetFilename()))
            {
                var page = document.GetPage(1);

                foreach (var expected in expectedData)
                {
                    var letter = page.Letters[expected.index];

                    Assert.Equal(expected.letter, letter.Value);
                    Assert.Equal(expected.x, Math.Round(letter.Location.X, 2));
                    Assert.Equal(expected.y, Math.Round(letter.Location.Y, 2));
                }
            }
        }
    }
}
