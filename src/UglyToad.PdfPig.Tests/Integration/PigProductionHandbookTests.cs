namespace UglyToad.PdfPig.Tests.Integration
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Xml;
    using System.Xml.Linq;
    using DocumentLayoutAnalysis;
    using Export;
    using Xunit;

    public class PigProductionHandbookTests
    {
        private static string GetFilename()
        {
            return IntegrationHelpers.GetDocumentPath("Pig Production Handbook.pdf");
        }

        [Fact]
        public void CanReadContent()
        {
            using (var document = PdfDocument.Open(GetFilename(), ParsingOptions.LenientParsingOff))
            {
                var page = document.GetPage(1);

                Assert.Contains("For the small holders at village level", page.Text);
            }
        }

        [Fact]
        public void LettersHaveCorrectColors()
        {
            using (var document = PdfDocument.Open(GetFilename(), ParsingOptions.LenientParsingOff))
            {
                var page = document.GetPage(1);

                // Pinkish.
                var (r, g, b) = page.Letters[0].Color.ToRGBValues();

                Assert.Equal(1, r);
                Assert.Equal(0.914m, g);
                Assert.Equal(0.765m, b);

                // White.
                (r, g, b) = page.Letters[37].Color.ToRGBValues();

                Assert.Equal(1, r);
                Assert.Equal(1, g);
                Assert.Equal(1, b);

                // Blackish.
                (r, g, b) = page.Letters[76].Color.ToRGBValues();

                Assert.Equal(0.137m, r);
                Assert.Equal(0.122m, g);
                Assert.Equal(0.125m, b);
            }
        }

        [Fact]
        public void Page1HasCorrectWords()
        {
            var expected = new List<string>
            {
                "European",
                "Comission",
                "Farmer's",
                "Hand",
                "Book",
                "on",
                "Pig",
                "Production",
                "(For",
                "the",
                "small",
                "holders",
                "at",
                "village",
                "level)",
                "GCP/NEP/065/EC",
                "Food",
                "and",
                "Agriculture",
                "Organization",
                "of",
                "the",
                "United",
                "Nations"
            };

            using (var document = PdfDocument.Open(GetFilename(), ParsingOptions.LenientParsingOff))
            {
                var page = document.GetPage(1);

                var words = page.GetWords().ToList();

                Assert.Equal(expected, words.Select(x => x.Text));
            }
        }

        [Fact]
        public void Page4HasCorrectWords()
        {
            var expected = WordsPage4.Split(new[] { "\r", "\r\n", "\n", " " }, StringSplitOptions.RemoveEmptyEntries);
            using (var document = PdfDocument.Open(GetFilename(), ParsingOptions.LenientParsingOff))
            {
                var page = document.GetPage(4);

                var words = page.GetWords().ToList();

                Assert.Equal(expected, words.Select(x => x.Text));
            }
        }

        [Fact]
        public void CanReadPage9()
        {
            using (var document = PdfDocument.Open(GetFilename(), ParsingOptions.LenientParsingOff))
            {
                var page = document.GetPage(9);

                Assert.Contains("BreedsNative breeds of pig can be found throughout the country. They are a small body size compared to other exotic and crosses pig types. There name varies from region to region, for example", page.Text);
            }
        }

        [Fact]
        public void HasCorrectNumberOfPages()
        {
            using (var document = PdfDocument.Open(GetFilename(), ParsingOptions.LenientParsingOff))
            {
                Assert.Equal(86, document.NumberOfPages);
            }
        }

        [Fact]
        public void CanExportAltoXmlFormat()
        {
            using (var document = PdfDocument.Open(GetFilename(), ParsingOptions.LenientParsingOff))
            {
                var exporter = new AltoXmlTextExporter(new NearestNeighbourWordExtractor(), new DocstrumBoundingBoxes());
                var xml = exporter.Get(document.GetPage(4), true);
                Assert.NotNull(xml);
                using (var xmlStream = new MemoryStream(Encoding.UTF8.GetBytes(xml)))
                using (var xmlReader = new XmlTextReader(xmlStream))
                {
                    var xDocument = XDocument.Load(xmlReader);
                    Assert.NotNull(xDocument);
                }
            }
        }

        [Fact]
        public void CanExportAltoXmlFormatPage16()
        {
            // Page 16 contains an unprintable string and a single line of text which causes problems for Docstrum.
            using (var document = PdfDocument.Open(GetFilename(), ParsingOptions.LenientParsingOff))
            {
                var exporter = new AltoXmlTextExporter(new NearestNeighbourWordExtractor(), new DocstrumBoundingBoxes());
                var xml = exporter.Get(document.GetPage(16), true);
                Assert.NotNull(xml);
                using (var xmlStream = new MemoryStream(Encoding.UTF8.GetBytes(xml)))
                using (var xmlReader = new XmlTextReader(xmlStream))
                {
                    var xDocument = XDocument.Load(xmlReader);
                    Assert.NotNull(xDocument);
                }
            }
        }

        [Fact]
        public void LettersHaveCorrectPosition()
        {
            using (var document = PdfDocument.Open(GetFilename(), ParsingOptions.LenientParsingOff))
            {
                var page = document.GetPage(1);
                var letters = page.Letters;
                var positions = GetPdfBoxPositionData();

                for (var i = 0; i < letters.Count; i++)
                {
                    var letter = letters[i];
                    var position = positions[i];

                    position.AssertWithinTolerance(letter, page, false);
                }
            }
        }

        private static IReadOnlyList<AssertablePositionData> GetPdfBoxPositionData()
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Integration", "Documents", "Pig Production Handbook.Page1.Positions.txt");
            var data = File.ReadAllText(path);

            var result = data.Split(new[] { "\r", "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(AssertablePositionData.Parse)
                .ToList();

            return result;
        }

        private const string WordsPage4 = @"Disclaimer
The designations employed end the presentation of the material in this information
product do not imply the expression of any opinion whatsoever on the part of the
Food and Agriculture Organization of the United Nations (FAO) concerning the
legal or development status of any country, territory, city or area of its authorities,
or concerning the delimitation of its frontiers or boundaries. The mention of
specific companies or products of manufacturers, whether or not these have been
patented, does not imply that these have been endorsed or recommended by FAO
in preference to others of similar nature that are not mentioned.
The views expressed in this publication are those of the author(s) and do not
necessarily reflects the views of FAO.
All rights reserved. Reproduction and dissemination of materials in this information
product for educational or other non-commercial purposes are authorized without
any prior written permission from the copyright holders provided the source is
fully acknowledged. Reproduction in this information product for resale or other
commercial purposes is prohibited without written permission of the copyright
holders. Applications for such permission should be addressed to: Chief, Electronic
Publishing Policy and Support Branch Communication Division, FAO, Viale delle
Terme di Caracalla, 00153 Rome, Italy or by e-mail to: copyright@fao.org
FAO 2009
design&print: wps, eMail: printnepal@gmail.com";
    }
}