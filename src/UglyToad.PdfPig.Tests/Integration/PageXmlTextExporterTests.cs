namespace UglyToad.PdfPig.Tests.Integration
{
    using DocumentLayoutAnalysis.Export;
    using DocumentLayoutAnalysis.PageSegmenter;
    using DocumentLayoutAnalysis.ReadingOrderDetector;
    using PdfPig.Core;
    using PdfPig.Util;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Xml;
    using UglyToad.PdfPig.DocumentLayoutAnalysis.Export.PAGE;

    public class PageXmlTextExporterTests
    {
        private static string GetFilename()
        {
            return IntegrationHelpers.GetDocumentPath("2006_Swedish_Touring_Car_Championship.pdf");
        }

        private static string GetXml(PageXmlTextExporter pageXmlTextExporter = null)
        {
            pageXmlTextExporter = pageXmlTextExporter ?? new PageXmlTextExporter(
                DefaultWordExtractor.Instance,
                RecursiveXYCut.Instance,
                UnsupervisedReadingOrderDetector.Instance);

            using (var document = PdfDocument.Open(GetFilename()))
            {
                var page = document.GetPage(1);
                return pageXmlTextExporter.Get(page);
            }
        }

        [Fact]
        public void WhenReadingOrder_ContainsReadingOrderXmlElements()
        {
            var pageXmlTextExporter = new PageXmlTextExporter(
                DefaultWordExtractor.Instance,
                RecursiveXYCut.Instance,
                UnsupervisedReadingOrderDetector.Instance);
            var xml = GetXml(pageXmlTextExporter);

            Assert.Contains("<ReadingOrder>", xml);
            Assert.Contains("</OrderedGroup>", xml);
        }

        [Fact]
        public void PageHeightAndWidthArePresent()
        {
            var xml = GetXml();
            Assert.Contains(@"<Page imageFilename=""unknown"" imageWidth=""595"" imageHeight=""842"">", xml);
        }

        [Fact]
        public void ContainsExpectedNumberOfTextRegions()
        {
            var xml = GetXml();
            var count = Regex.Matches(xml, "</TextRegion>").Count;

            Assert.Equal(22, count);
        }

        [Fact]
        public void ContainsExpectedText()
        {
            var xml = GetXml();
            Assert.Contains("2006 Swedish Touring Car Championship", xml);
            // the coords for that text
            Assert.Contains(@"<Coords points=""35,77 35,62 397,62 397,77"" />", xml);
        }

        [Fact]
        public void NoPointsAreOnThePageBoundary()
        {
            var pageWidth = 100;
            var pageHeight = 200;

            var topLeftPagePoint = new PdfPoint(0,0);
            var bottomLeftPagePoint = new PdfPoint(0, pageHeight);
            var bottomRightPagePoint = new PdfPoint(pageWidth, pageHeight);
            var normalPoint = new PdfPoint(60, 60);

            Assert.Equal("1,199", PageXmlTextExporter.PointToString(topLeftPagePoint, pageWidth, pageHeight));
            Assert.Equal("1,1", PageXmlTextExporter.PointToString(bottomLeftPagePoint, pageWidth, pageHeight));
            Assert.Equal("99,1", PageXmlTextExporter.PointToString(bottomRightPagePoint, pageWidth, pageHeight));
            Assert.Equal("60,140", PageXmlTextExporter.PointToString(normalPoint, pageWidth, pageHeight));
        }

        [Fact]
        public void Issue655NoCheckStrategy()
        {
            var hex_0x0006 = IntegrationHelpers.GetDocumentPath("hex_0x0006.pdf");

            var pageXmlTextExporter = new PageXmlTextExporter(
                DefaultWordExtractor.Instance,
                RecursiveXYCut.Instance,
                UnsupervisedReadingOrderDetector.Instance);

            Assert.Equal(InvalidCharStrategy.DoNotCheck, pageXmlTextExporter.InvalidCharStrategy);

            using (var document = PdfDocument.Open(hex_0x0006))
            {
                var page = document.GetPage(1);

                // Convert page to text
                string xml = pageXmlTextExporter.Get(page);

                // Save text to an xml file
                File.WriteAllText("issue655.nocheck.pagexml.xml", xml);

                var pageXml = PageXmlTextExporter.Deserialize("issue655.nocheck.pagexml.xml");

                var textRegions = pageXml.Page.Items.OfType<PageXmlDocument.PageXmlTextRegion>().ToArray();
                Assert.Single(textRegions);

                var textEquivs = textRegions.Single().TextEquivs;
                Assert.Single(textEquivs);

                string unicode = textEquivs.Single().Unicode;
                Assert.Equal("TM 1\u00062345\u0006678\u0006ABC", unicode); // no check strategy, contains invalid xml chars
            }
        }

        [Fact]
        public void Issue655RemoveStrategy()
        {
            var hex_0x0006 = IntegrationHelpers.GetDocumentPath("hex_0x0006.pdf");

            var pageXmlTextExporter = new PageXmlTextExporter(
                DefaultWordExtractor.Instance,
                RecursiveXYCut.Instance,
                UnsupervisedReadingOrderDetector.Instance,
                invalidCharacterStrategy: InvalidCharStrategy.Remove);

            Assert.Equal(InvalidCharStrategy.Remove, pageXmlTextExporter.InvalidCharStrategy);

            using (var document = PdfDocument.Open(hex_0x0006))
            {
                var page = document.GetPage(1);

                // Convert page to text
                string xml = pageXmlTextExporter.Get(page);

                // Save text to an xml file
                File.WriteAllText("issue655.remove.pagexml.xml", xml);

                var pageXml = PageXmlTextExporter.Deserialize("issue655.remove.pagexml.xml");

                var textRegions = pageXml.Page.Items.OfType<PageXmlDocument.PageXmlTextRegion>().ToArray();
                Assert.Single(textRegions);

                var textEquivs = textRegions.Single().TextEquivs;
                Assert.Single(textEquivs);

                string unicode = textEquivs.Single().Unicode;
                Assert.Equal("TM 12345678ABC", unicode);
            }
        }

        [Fact]
        public void Issue655ConvertToHexadecimalStrategy()
        {
            var hex_0x0006 = IntegrationHelpers.GetDocumentPath("hex_0x0006.pdf");

            var pageXmlTextExporter = new PageXmlTextExporter(
                DefaultWordExtractor.Instance,
                RecursiveXYCut.Instance,
                UnsupervisedReadingOrderDetector.Instance,
                invalidCharacterStrategy: InvalidCharStrategy.ConvertToHexadecimal);

            Assert.Equal(InvalidCharStrategy.ConvertToHexadecimal, pageXmlTextExporter.InvalidCharStrategy);

            using (var document = PdfDocument.Open(hex_0x0006))
            {
                var page = document.GetPage(1);

                // Convert page to text
                string xml = pageXmlTextExporter.Get(page);

                // Save text to an xml file
                File.WriteAllText("issue655.hex.pagexml.xml", xml);

                var pageXml = PageXmlTextExporter.Deserialize("issue655.hex.pagexml.xml");

                var textRegions = pageXml.Page.Items.OfType<PageXmlDocument.PageXmlTextRegion>().ToArray();
                Assert.Single(textRegions);

                var textEquivs = textRegions.Single().TextEquivs;
                Assert.Single(textEquivs);

                string unicode = textEquivs.Single().Unicode;
                Assert.Equal("TM 10x0623450x066780x06ABC", unicode);
            }
        }

        [Fact]
        public void Issue655CustomStrategy()
        {
            var hex_0x0006 = IntegrationHelpers.GetDocumentPath("hex_0x0006.pdf");

            var pageXmlTextExporter = new PageXmlTextExporter(
                DefaultWordExtractor.Instance,
                RecursiveXYCut.Instance,
                UnsupervisedReadingOrderDetector.Instance, 1.0, "\t",
                new Func<string, string>(s =>
                {
                    // Adapted from https://stackoverflow.com/a/17735649
                    if (string.IsNullOrEmpty(s))
                    {
                        return s;
                    }

                    int length = s.Length;
                    StringBuilder stringBuilder = new StringBuilder(length);
                    for (int i = 0; i < length; ++i)
                    {
                        if (XmlConvert.IsXmlChar(s[i]))
                        {
                            stringBuilder.Append(s[i]);
                        }
                        else
                        {
                            stringBuilder.Append("!?");
                        }
                    }

                    return stringBuilder.ToString();
                }));

            Assert.Equal(InvalidCharStrategy.Custom, pageXmlTextExporter.InvalidCharStrategy);

            using (var document = PdfDocument.Open(hex_0x0006))
            {
                var page = document.GetPage(1);

                // Convert page to text
                string xml = pageXmlTextExporter.Get(page);

                // Save text to an xml file
                File.WriteAllText("issue655.custom.pagexml.xml", xml);

                var pageXml = PageXmlTextExporter.Deserialize("issue655.custom.pagexml.xml");

                var textRegions = pageXml.Page.Items.OfType<PageXmlDocument.PageXmlTextRegion>().ToArray();
                Assert.Single(textRegions);

                var textEquivs = textRegions.Single().TextEquivs;
                Assert.Single(textEquivs);

                string unicode = textEquivs.Single().Unicode;
                Assert.Equal("TM 1!?2345!?678!?ABC", unicode);
            }
        }
    }
}
