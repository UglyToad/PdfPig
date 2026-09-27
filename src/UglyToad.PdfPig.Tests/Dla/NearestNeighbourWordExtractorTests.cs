namespace UglyToad.PdfPig.Tests.Dla
{
    using UglyToad.PdfPig.Content;
    using UglyToad.PdfPig.Core;
    using UglyToad.PdfPig.DocumentLayoutAnalysis.WordExtractor;
    using UglyToad.PdfPig.Graphics.Core;
    using UglyToad.PdfPig.PdfFonts;

    public class NearestNeighbourWordExtractorTests
    {
        public static IEnumerable<object[]> DataWords => new[]
        {
            new object[]
            {
                "2559 words.pdf",
                5118,
                2559
            },
            new object[]
            {
                "fseprd1102849.pdf",
                12855,
                11129
            },
            new object[]
            {
                "90 180 270 rotated.pdf",
                589,
                292
            },
            new object[]
            {
                "complex rotated.pdf",
                805,
                403
            },
            new object[]
            {
                "no horizontal distance.pdf",
                4,
                2
            },
            new object[]
            {
                "no vertical distance.pdf",
                22,
                10
            },
            new object[]
            {
                "no vertical horizontal distance.pdf",
                4,
                2
            },
            new object[]
            {
                "Random 2 Columns Lists Hyph - Justified.pdf",
                1191,
                607
            },
            new object[]
            {
                "caly-issues-56-1.pdf",
                184,
                156
            },
            new object[]
            {
                "caly-issues-58-2.pdf",
                49,
                49
            },
        };

        [SkippableTheory]
        [MemberData(nameof(DataWords))]
        public void WordCount(string path, int wordCount, int noSpacesWordCount)
        {
            using (var document = PdfDocument.Open(DlaHelper.GetDocumentPath(path)))
            {
                var page = document.GetPage(1);
                var words = NearestNeighbourWordExtractor.Instance.GetWords(page.Letters).ToArray();

                Assert.Equal(wordCount, words.Length);

                var noSpacesWords = words.Where(x => !string.IsNullOrEmpty(x.Text.Trim())).ToArray();

                Assert.Equal(noSpacesWordCount, noSpacesWords.Length);
            }
        }

        private static Letter CreateLetter(string value, double x, double width)
        {
            var bbox = new PdfRectangle(x, 0, x + width, 7);
            return new Letter(value, bbox, bbox, new PdfPoint(x, 0), new PdfPoint(x + width, 0), width, 1,
                (FontDetails)null, TextRenderingMode.Fill, null, null, 10, 0);
        }

        [Fact]
        public void NarrowLetterIsNotItsOwnNeighbour()
        {
            // The narrow 'i' (width 1) is followed by a 1.5 gap, which is below the
            // maximum distance (20% of point size 10 = 2) but above its own width.
            var letters = new List<Letter>();
            double x = 0;
            foreach (var c in "abc")
            {
                letters.Add(CreateLetter(c.ToString(), x, 5));
                x += 5;
            }

            letters.Add(CreateLetter("i", x, 1));
            x += 1 + 1.5;

            foreach (var c in "def")
            {
                letters.Add(CreateLetter(c.ToString(), x, 5));
                x += 5;
            }

            var words = NearestNeighbourWordExtractor.Instance.GetWords(letters).ToArray();

            Assert.Equal("abcidef", Assert.Single(words).Text);
        }
    }
}
