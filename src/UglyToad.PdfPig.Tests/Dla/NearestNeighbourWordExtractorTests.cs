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

        private static Letter CreateLetter(string value, PdfPoint start, double width, double angleDeg)
        {
            double rad = angleDeg * Math.PI / 180.0;
            var direction = new PdfPoint(Math.Cos(rad), Math.Sin(rad));
            var normal = new PdfPoint(-direction.Y, direction.X);
            var end = new PdfPoint(start.X + width * direction.X, start.Y + width * direction.Y);
            var bbox = new PdfRectangle(
                new PdfPoint(start.X + 7 * normal.X, start.Y + 7 * normal.Y),
                new PdfPoint(end.X + 7 * normal.X, end.Y + 7 * normal.Y),
                start,
                end);
            return new Letter(value, bbox, bbox, start, end, width, 1,
                (FontDetails)null, TextRenderingMode.Fill, null, null, 10, 0);
        }

        private static List<Letter> CreateWord(string text, double angleDeg)
        {
            var letters = new List<Letter>();
            double rad = angleDeg * Math.PI / 180.0;
            for (int i = 0; i < text.Length; i++)
            {
                var start = new PdfPoint(100 + i * 5 * Math.Cos(rad), 100 + i * 5 * Math.Sin(rad));
                letters.Add(CreateLetter(text[i].ToString(), start, 5, angleDeg));
            }
            return letters;
        }

        [Theory]
        [InlineData(0)]
        [InlineData(30)]
        [InlineData(90)]
        [InlineData(135)]
        [InlineData(180)]
        [InlineData(270)]
        public void LettersDrawnInReverseOrder(double angleDeg)
        {
            var letters = CreateWord("hello", angleDeg);
            letters.Reverse();

            var words = NearestNeighbourWordExtractor.Instance.GetWords(letters).ToArray();

            Assert.Equal("hello", Assert.Single(words).Text);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(30)]
        [InlineData(210)]
        public void LettersDrawnOutOfOrder(double angleDeg)
        {
            var letters = CreateWord("abcdef", angleDeg);
            var shuffled = new[] { letters[2], letters[0], letters[5], letters[1], letters[4], letters[3] };

            var words = NearestNeighbourWordExtractor.Instance.GetWords(shuffled).ToArray();

            Assert.Equal("abcdef", Assert.Single(words).Text);
        }

        [Fact]
        public void MutualNeighboursDoNotLoop()
        {
            // 'a' ends where 'z' starts and 'z' ends where 'a' starts: a 2-cycle.
            var a = CreateLetter("a", new PdfPoint(0, 0), 5, 0);
            var z = CreateLetter("z", new PdfPoint(5, 0), 5, 180);
            var c = CreateLetter("c", new PdfPoint(-5, 0), 5, 0); // c -> a

            var extractor = new NearestNeighbourWordExtractor(new NearestNeighbourWordExtractor.NearestNeighbourWordExtractorOptions()
            {
                GroupByOrientation = false
            });

            var words = extractor.GetWords(new[] { a, z, c }).ToArray();

            // The cycle is cut after its highest-index letter ('z').
            Assert.Equal("caz", Assert.Single(words).Text);
        }

        [Fact]
        public void WordsKeepContentStreamOrder()
        {
            var first = CreateWord("first", 0);
            var second = CreateWord("second", 0).Select(l => CreateLetter(l.Value, new PdfPoint(l.StartBaseLine.X, 200), 5, 0)).ToList();
            second.Reverse();

            var words = NearestNeighbourWordExtractor.Instance.GetWords(first.Concat(second).ToArray()).ToArray();

            Assert.Equal(new[] { "first", "second" }, words.Select(w => w.Text));
        }
    }
}
