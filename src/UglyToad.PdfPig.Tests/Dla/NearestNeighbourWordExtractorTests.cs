namespace UglyToad.PdfPig.Tests.Dla
{
    using UglyToad.PdfPig.DocumentLayoutAnalysis.WordExtractor;

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
                12903,
                11177
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
    }
}
