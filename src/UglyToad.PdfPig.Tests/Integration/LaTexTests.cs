namespace UglyToad.PdfPig.Tests.Integration
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Xunit;

    public class LaTexTests
    {
        private static string GetFilename()
        {
            return IntegrationHelpers.GetDocumentPath("ICML03-081.pdf");
        }

        [Fact]
        public void CanReadContent()
        {
            using (var document = PdfDocument.Open(GetFilename()))
            {
                var page = document.GetPage(1);

                Assert.Contains("TacklingthePoorAssumptionsofNaiveBayesTextClassiﬁers", page.Text);

                var page2 = document.GetPage(2);

                Assert.Contains("is~θc={θc1,θc2,...,θcn},", page2.Text);
            }
        }

        [Fact]
        public void LettersHaveHeight()
        {
            using (var document = PdfDocument.Open(GetFilename()))
            {
                var page = document.GetPage(1);

                Assert.NotEqual(0, page.Letters[0].GlyphRectangle.Height);
            }
        }

        [Fact]
        public void HasCorrectNumberOfPages()
        {
            using (var document = PdfDocument.Open(GetFilename()))
            {
                Assert.Equal(8, document.NumberOfPages);
            }
        }

        [Fact]
        public void LettersHaveCorrectPositionsXfinium()
        {
            var positions = GetXfiniumPositionData();
            using (var document = PdfDocument.Open(GetFilename()))
            {
                var page = document.GetPage(1);

                for (var i = 0; i < page.Letters.Count; i++)
                {
                    if (i >= positions.Count)
                    {
                        break;
                    }
                    var letter = page.Letters[i];
                    var expected = positions[i];

                    expected.AssertWithinTolerance(letter, page, false);
                }
            }
        }

        [Fact]
        public void LettersHaveCorrectPositionsPdfBox()
        {
            var positions = GetPdfBoxPositionData();
            using (var document = PdfDocument.Open(GetFilename()))
            {
                var page = document.GetPage(1);

                for (var i = 0; i < page.Letters.Count; i++)
                {
                    if (i >= positions.Count)
                    {
                        break;
                    }
                    var letter = page.Letters[i];
                    var expected = positions[i];

                    expected.AssertWithinTolerance(letter, page);
                }
            }
        }

        [Fact]
        public void Page1Words()
        {
            const string expectedString = @"Tackling the Poor Assumptions of Naive Bayes Text Classiﬁers
Jason D. M. Rennie jrennie@mit.edu
Lawrence Shih kai@mit.edu
Jaime Teevan teevan@mit.edu
David R. Karger karger@mit.edu
Artiﬁcial Intelligence Laboratory; Massachusetts Institute of Technology; Cambridge, MA 02139
Abstract amples. To balance the amount of training examples
used per estimate, we introduce a “complement class”Naive Bayes is often used as a baseline in";

            var expected = expectedString.Split(new[] {"\r", "\r\n", "\n", " "}, StringSplitOptions.RemoveEmptyEntries);

            using (var document = PdfDocument.Open(GetFilename()))
            {
                var page = document.GetPage(1);

                var words = page.GetWords().ToList();

                for (var i = 0; i < words.Count; i++)
                {
                    if (i == expected.Length)
                    {
                        break;
                    }

                    Assert.True(string.Equals(expected[i], words[i].Text, StringComparison.Ordinal),
                        $"Expected word {expected[i]} got word {words[i].Text} at index {i}.");
                }
            }
        }

        private static IReadOnlyList<AssertablePositionData> GetPdfBoxPositionData()
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Integration", "Documents", "ICML03-081.Page1.Positions.txt");
            var data = File.ReadAllText(path);
            var result = data.Split(new[] { "\r", "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(AssertablePositionData.Parse)
                .ToList();

            return result;
        }

        private static IReadOnlyList<AssertablePositionData> GetXfiniumPositionData()
        {
            const string data = @"75.731	83.12866	11.218572	T	14.346	WDKAAR+CMBX12	9.956124
85.6153934	83.123866	7.847262	a	11.218572	WDKAAR+CMBX12	9.956124
93.462656	83.123866	7.173	c	11.218572	WDKAAR+CMBX12	9.956124
100.176584	83.123866	8.521524	k	11.218572	WDKAAR+CMBX12	9.956124
108.698108	83.123866	4.490298	l	11.218572	WDKAAR+CMBX12	9.956124";

            var result = data.Split(new[] { "\r", "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(AssertablePositionData.Parse)
                .ToList();

            return result;
        }
    }
}