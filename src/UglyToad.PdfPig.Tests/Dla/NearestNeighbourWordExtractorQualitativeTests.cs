namespace UglyToad.PdfPig.Tests.Dla
{
    using UglyToad.PdfPig.Content;
    using UglyToad.PdfPig.DocumentLayoutAnalysis.WordExtractor;
    using UglyToad.PdfPig.Tests.Integration;

    /// <summary>
    /// Qualitative tests of the words extracted by <see cref="NearestNeighbourWordExtractor"/> on real documents:
    /// words are neither split nor merged, and are in the expected order and orientation.
    /// </summary>
    public class NearestNeighbourWordExtractorQualitativeTests
    {
        private static readonly char[] WordSeparators = [' ', '\r', '\n', '\t'];

        private static string GetPath(string name)
        {
            var path = DlaHelper.GetDocumentPath(name);
            return File.Exists(path) ? path : IntegrationHelpers.GetSpecificTestDocumentPath(name);
        }

        /// <summary>
        /// The page's words, excluding white spaces, in the order returned by the extractor.
        /// </summary>
        internal static List<Word> GetWords(string name, int pageNumber)
        {
            using (var document = PdfDocument.Open(GetPath(name), new ParsingOptions { UseLenientParsing = true }))
            {
                var page = document.GetPage(pageNumber);
                return NearestNeighbourWordExtractor.Instance.GetWords(page.Letters)
                    .Where(w => !string.IsNullOrWhiteSpace(w.Text))
                    .ToList();
            }
        }

        /// <summary>
        /// Assert the expected words appear consecutively, each word exactly (not split, not merged with its neighbours).
        /// </summary>
        internal static void AssertContainsWords(IReadOnlyList<Word> words, string expected, TextOrientation? orientation = null)
        {
            var expectedWords = expected.Split(WordSeparators, StringSplitOptions.RemoveEmptyEntries);

            for (int start = 0; start <= words.Count - expectedWords.Length; start++)
            {
                bool match = true;
                for (int i = 0; i < expectedWords.Length && match; i++)
                {
                    var word = words[start + i];
                    match = word.Text == expectedWords[i] && (!orientation.HasValue || word.TextOrientation == orientation.Value);
                }

                if (match)
                {
                    return;
                }
            }

            Assert.Fail($"Could not find the consecutive words '{expected}'{(orientation.HasValue ? $" ({orientation})" : string.Empty)}. " +
                        $"Words were: {string.Join(" ", words.Select(w => w.Text).Take(300))}");
        }

        public static IEnumerable<object[]> WordSequences => new[]
        {
            // Simple text, from different producers
            new object[] { "Two Page Text Only - from libre office", 1, "Apache License Version 2.0, January 2004 http://www.apache.org/licenses/" },
            new object[] { "Two Page Text Only - from libre office", 1, "TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION" },
            new object[] { "Two Page Text Only - from libre office", 2, "2. Grant of Copyright License. Subject to the terms and conditions of this License," },
            new object[] { "Single Page Type 1 Font", 1, "PDF test that contains the word yadda" },
            new object[] { "bold-italic", 1, "Lorem ipsum dolor sit amet, consectetur adipiscing elit." },
            new object[] { "byz", 1, "The Byzantine Generals Problem" },
            new object[] { "capas", 1, "GASB Statement #34 Capital Assets & Depreciation Guidance August 31, 2001" },
            new object[] { "Judgement Document", 1, "Neutral Citation Number: [2017] EWHC 3175 (IPEC) Case No: IP-2016-000174" },
            new object[] { "GHOSTSCRIPT-699178-0", 1, "Welcome This is a sample document that merely demonstrates some basic graphics features" },

            // Very different font sizes on the same page
            new object[] { "Font Size Test - from libre office", 1, "36pt font 14 pt font 6pt font" },

            // Type 3 fonts, including glyphs with a zero height
            new object[] { "Type3Test", 1, "ab ba abba ab ba abba" },
            new object[] { "type3-font-zero-height", 1, "Comments P&ID DCS Tags Loop Description Alarm Priority" },

            // Scientific papers: title, e-mail addresses, ligatures
            new object[] { "2108.11480", 1, "On Approximate Nearest Neighbour Selection for Multi-Stage Dense Retrieval" },
            new object[] { "2108.11480", 1, "craig.macdonald@glasgow.ac.uk Nicola Tonellotto University of Pisa, Italy Nicola.Tonellotto@unipi.it" },
            new object[] { "ICML03-081", 1, "Tackling the Poor Assumptions of Naive Bayes Text Classiﬁers" }, // 'fi' ligature
            new object[] { "ICML03-081", 1, "make such eﬃciency possible" }, // 'ffi' ligature

            // Technical data sheet: units and part numbers
            new object[] { "EE24LC01_EE24LC02#MIC.pdf", 1, "1 mA active current typical" },
            new object[] { "EE24LC01_EE24LC02#MIC.pdf", 1, "10 µA standby current typical at 5.5V" },

            // Accented and non Latin scripts
            new object[] { "Diacritics_export", 1, "Espinosa Spínola Moraña," },
            new object[] { "Old Gutnish Internet Explorer", 1, "spoken on the Baltic island of Gotland." },
            new object[] { "Old Gutnish Internet Explorer", 1, "adjoining island of Fårö." },
            new object[] { "soundandvision", 1, "Client Fliis Trade, UAB Saltoniškių g. 9-101" },

            // The apostrophe starts before the end of the 'L' (kerning), see https://github.com/UglyToad/PdfPig/issues/1219
            new object[] { "issues-1219", 1, "Fouilles de Conimbriga I, L’architecture .Paris." },
            new object[] { "issues-1219-2", 1, "chap. 6 of L’Ambiguïté du Livre:" },
            new object[] { "issues-1219-2", 1, "Gratian’s" },
            new object[] { "issues-1219-2", 1, "Clarke’s" },
            new object[] { "issues-1219-2", 1, "Ockham’s" },
            new object[] { "little-pig-in-armenian", 1, "փոքրիկ խոզ" },
            new object[] { "Type0_CJK_Font", 1, "中航动力控制股份有限公司 2010 年半年度报告" },
            new object[] { "caly-issues-56-1", 1, "Livermore National Lab." },

            // Mixed Latin and CJK text
            new object[] { "Motor Insurance claim form", 1, "MOTOR INSURANCE CLAIM FORM 汽車保險索償申請表 Policy No. 保單號碼" },

            // Tables and forms: dates, amounts and codes
            new object[] { "FICTIF_TABLE_INDEX", 1, "BULLETIN DE SALAIRE" },
            new object[] { "FICTIF_TABLE_INDEX", 1, "Novembre 2022 DU 01/11/2022 AU 30/11/2022" },
            new object[] { "soundandvision", 1, "Tax/VAT No.: LT100007828811" },
        };

        [Theory]
        [MemberData(nameof(WordSequences))]
        public void ContainsWords(string document, int page, string expected)
        {
            var words = GetWords(document, page);

            AssertContainsWords(words, expected);
        }

        public static IEnumerable<object[]> OrientedWordSequences => new[]
        {
            // Whole page rotated
            new object[] { "SinglePage90ClockwiseRotation - from PdfPig", TextOrientation.Rotate90, "Hello World! This is some further text continuing to write" },
            new object[] { "SinglePage180ClockwiseRotation - from PdfPig", TextOrientation.Rotate180, "Hello World! This is some further text continuing to write" },
            new object[] { "SinglePage270ClockwiseRotation - from PdfPig", TextOrientation.Rotate270, "Hello World! This is some further text continuing to write" },
            new object[] { "cropped-and-rotated", TextOrientation.Rotate90, "A B C D" },

            // Several orientations on the same page
            new object[] { "90 180 270 rotated", TextOrientation.Rotate90, "Lorem ipsum dolor sit amet, consectetur adipiscing elit." },
            new object[] { "90 180 270 rotated", TextOrientation.Rotate180, "Cras gravida vel risus sit amet sagittis." },
            new object[] { "90 180 270 rotated", TextOrientation.Rotate270, "Morbi euismod mattis libero, nec porta neque aliquam et." },
            new object[] { "Rotated Text Libre Office", TextOrientation.Horizontal, "Normal Text" },
            new object[] { "Rotated Text Libre Office", TextOrientation.Other, "This is some angled text" },

            // Text at arbitrary angles
            new object[] { "complex rotated", TextOrientation.Other, "Lorem ipsum dolor sit amet, consectetur adipiscing elit." },
            new object[] { "PDFBOX-492-4.jar-8", TextOrientation.Other, "Projektmanagement-Tag Südwest" },

            // Vertical text in the margin of a horizontal page
            new object[] { "2108.11480", TextOrientation.Rotate270, "arXiv:2108.11480v1 [cs.IR] 25 Aug 2021" },
            new object[] { "GHOSTSCRIPT-699178-0", TextOrientation.Rotate270, "Generated by PDF Clown on Sat Mar 05 20:06:01 CET 2011" },
            new object[] { "EE24LC01_EE24LC02#MIC.pdf", TextOrientation.Rotate90, "24LC01B/02B" },
        };

        [Theory]
        [MemberData(nameof(OrientedWordSequences))]
        public void ContainsWordsWithOrientation(string document, TextOrientation orientation, string expected)
        {
            var words = GetWords(document, 1);

            AssertContainsWords(words, expected, orientation);
        }

        [Theory]
        [InlineData("SinglePage90ClockwiseRotation - from PdfPig", TextOrientation.Rotate90)]
        [InlineData("SinglePage180ClockwiseRotation - from PdfPig", TextOrientation.Rotate180)]
        [InlineData("SinglePage270ClockwiseRotation - from PdfPig", TextOrientation.Rotate270)]
        public void RotatedPageHasAllWords(string document, TextOrientation orientation)
        {
            var words = GetWords(document, 1);

            Assert.Equal(new[] { "Hello", "World!", "This", "is", "some", "further", "text", "continuing", "to", "write" },
                words.Select(w => w.Text), StringComparer.Ordinal);
            Assert.All(words, w => Assert.Equal(orientation, w.TextOrientation));
        }

        [Theory]
        [InlineData("no horizontal distance", new[] { "First.", "Second." })]
        [InlineData("no vertical horizontal distance", new[] { "First.", "Second." })]
        [InlineData("no vertical distance", new[] { "Document’s", "first", "line", "right", "aligned.", "Document’s", "second", "line", "left", "aligned." })]
        public void TouchingLinesAreNotMerged(string document, string[] expected)
        {
            var words = GetWords(document, 1);

            Assert.Equal(expected, words.Select(w => w.Text), StringComparer.Ordinal);
        }

        [Fact]
        public void EmojiGraphemeClusterIsOneWord()
        {
            var words = GetWords("Grapheme clusters emoji", 1);

            // Woman firefighter: medium skin tone, joined with a zero width joiner
            Assert.Equal("\U0001F469\U0001F3FD\u200D\U0001F692", Assert.Single(words).Text);
        }

        [Fact]
        public void TwoColumnsJustifiedTextMatchesTranscript()
        {
            // The transcript has the page's words in reading order, with the hyphenated words joined.
            var expected = File.ReadAllText(DlaHelper.GetDocumentPath("Random 2 Columns Lists Hyph - Justified.txt", false))
                .Split(WordSeparators, StringSplitOptions.RemoveEmptyEntries);

            var words = GetWords("Random 2 Columns Lists Hyph - Justified", 1).Select(w => w.Text).ToList();

            var actual = new List<string>();
            for (int i = 0; i < words.Count; i++)
            {
                var word = words[i];
                while (word.EndsWith("-") && i + 1 < words.Count)
                {
                    word += words[++i];
                }

                actual.Add(word);
            }

            Assert.Equal(expected, actual, StringComparer.Ordinal);
        }

        public static IEnumerable<object[]> DiverseDocuments => new[]
        {
            new object[] { "Two Page Text Only - from libre office" },
            new object[] { "90 180 270 rotated" },
            new object[] { "complex rotated" },
            new object[] { "Rotated Text Libre Office" },
            new object[] { "2108.11480" },
            new object[] { "ICML03-081" },
            new object[] { "Old Gutnish Internet Explorer" },
            new object[] { "Type0_CJK_Font" },
            new object[] { "Motor Insurance claim form" },
            new object[] { "FICTIF_TABLE_INDEX" },
            new object[] { "Type3Test" },
            new object[] { "PDFBOX-492-4.jar-8" },
            new object[] { "EE24LC01_EE24LC02#MIC.pdf" },
            new object[] { "fseprd1102849" },
        };

        [Theory]
        [MemberData(nameof(DiverseDocuments))]
        public void EveryLetterIsInExactlyOneWord(string document)
        {
            using (var pdf = PdfDocument.Open(GetPath(document), new ParsingOptions { UseLenientParsing = true }))
            {
                var letters = pdf.GetPage(1).Letters;
                var words = NearestNeighbourWordExtractor.Instance.GetWords(letters).ToList();

                var lettersInWords = words.SelectMany(w => w.Letters).ToList();
                Assert.Equal(letters.Count, lettersInWords.Count);
                Assert.Equal(letters.Count, lettersInWords.Distinct().Count());
            }
        }

        [Theory]
        [MemberData(nameof(DiverseDocuments))]
        public void WhiteSpacesAreNotPartOfWords(string document)
        {
            using (var pdf = PdfDocument.Open(GetPath(document), new ParsingOptions { UseLenientParsing = true }))
            {
                var words = NearestNeighbourWordExtractor.Instance.GetWords(pdf.GetPage(1).Letters);

                // A white space is always a word on its own
                Assert.All(words.Where(w => w.Letters.Any(l => string.IsNullOrWhiteSpace(l.Value))),
                    w => Assert.Single(w.Letters));
            }
        }

        [Theory]
        [MemberData(nameof(DiverseDocuments))]
        public void WordsHaveASingleOrientation(string document)
        {
            using (var pdf = PdfDocument.Open(GetPath(document), new ParsingOptions { UseLenientParsing = true }))
            {
                var words = NearestNeighbourWordExtractor.Instance.GetWords(pdf.GetPage(1).Letters);

                // Letters are grouped by orientation before being grouped into words
                Assert.All(words, w => Assert.Single(w.Letters.Select(l => l.TextOrientation).Distinct()));
            }
        }
    }
}
