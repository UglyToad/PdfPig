namespace UglyToad.PdfPig.Tests.Integration
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Text.RegularExpressions;
    using DocumentLayoutAnalysis.Export;
    using DocumentLayoutAnalysis.PageSegmenter;
    using DocumentLayoutAnalysis.ReadingOrderDetector;
    using PdfPig.Core;
    using PdfPig.Util;
    using Xunit;

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

            string xml;
            using (var document = PdfDocument.Open(GetFilename()))
            {
                var page = document.GetPage(1);
                xml = pageXmlTextExporter.Get(page);
            }

            return xml;
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
            Assert.Equal($"60,140", PageXmlTextExporter.PointToString(normalPoint, pageWidth, pageHeight));
        }
    }
}
