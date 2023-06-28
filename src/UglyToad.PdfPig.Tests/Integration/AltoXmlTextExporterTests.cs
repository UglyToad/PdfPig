namespace UglyToad.PdfPig.Tests.Integration
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Xml;
    using UglyToad.PdfPig.DocumentLayoutAnalysis.Export;
    using UglyToad.PdfPig.DocumentLayoutAnalysis.PageSegmenter;
    using UglyToad.PdfPig.Util;
    using Xunit;

    public class AltoXmlTextExporterTests
    {
        [Fact]
        public void Issue655NoCheckStrategy()
        {
            var hex_0x0006 = IntegrationHelpers.GetDocumentPath("hex_0x0006.pdf");

            var pageXmlTextExporter = new AltoXmlTextExporter(
                DefaultWordExtractor.Instance,
                RecursiveXYCut.Instance);

            Assert.Equal(InvalidCharStrategy.DoNotCheck, pageXmlTextExporter.InvalidCharStrategy);

            using (var document = PdfDocument.Open(hex_0x0006))
            {
                var page = document.GetPage(1);

                // Convert page to text
                string xml = pageXmlTextExporter.Get(page);

                // Save text to an xml file
                File.WriteAllText("issue655.nocheck.altoxml.xml", xml);

                var pageXml = AltoXmlTextExporter.Deserialize("issue655.nocheck.altoxml.xml");

                var textRegions = pageXml.Layout.Pages[0].PrintSpace.TextBlock;
                Assert.Single(textRegions);

                var textLines = textRegions.Single().TextLines;
                Assert.Single(textLines);

                var strings = textLines.Single().Strings;
                Assert.Equal(2, strings.Length);

                Assert.Equal("TM", strings[0].Content);
                Assert.Equal("1\u00062345\u0006678\u0006ABC", strings[1].Content); // no check strategy, contains invalid xml chars
            }
        }

        [Fact]
        public void Issue655RemoveStrategy()
        {
            var hex_0x0006 = IntegrationHelpers.GetDocumentPath("hex_0x0006.pdf");

            var pageXmlTextExporter = new AltoXmlTextExporter(
                DefaultWordExtractor.Instance,
                RecursiveXYCut.Instance,
                invalidCharacterStrategy: InvalidCharStrategy.Remove);

            Assert.Equal(InvalidCharStrategy.Remove, pageXmlTextExporter.InvalidCharStrategy);

            using (var document = PdfDocument.Open(hex_0x0006))
            {
                var page = document.GetPage(1);

                // Convert page to text
                string xml = pageXmlTextExporter.Get(page);

                // Save text to an xml file
                File.WriteAllText("issue655.remove.altoxml.xml", xml);

                var pageXml = AltoXmlTextExporter.Deserialize("issue655.remove.altoxml.xml");

                var textRegions = pageXml.Layout.Pages[0].PrintSpace.TextBlock;
                Assert.Single(textRegions);

                var textLines = textRegions.Single().TextLines;
                Assert.Single(textLines);

                var strings = textLines.Single().Strings;
                Assert.Equal(2, strings.Length);

                Assert.Equal("TM", strings[0].Content);
                Assert.Equal("12345678ABC", strings[1].Content);
            }
        }

        [Fact]
        public void Issue655ConvertToHexadecimalStrategy()
        {
            var hex_0x0006 = IntegrationHelpers.GetDocumentPath("hex_0x0006.pdf");

            var pageXmlTextExporter = new AltoXmlTextExporter(
                DefaultWordExtractor.Instance,
                RecursiveXYCut.Instance,
                invalidCharacterStrategy: InvalidCharStrategy.ConvertToHexadecimal);

            Assert.Equal(InvalidCharStrategy.ConvertToHexadecimal, pageXmlTextExporter.InvalidCharStrategy);

            using (var document = PdfDocument.Open(hex_0x0006))
            {
                var page = document.GetPage(1);

                // Convert page to text
                string xml = pageXmlTextExporter.Get(page);

                // Save text to an xml file
                File.WriteAllText("issue655.hex.altoxml.xml", xml);

                var pageXml = AltoXmlTextExporter.Deserialize("issue655.hex.altoxml.xml");

                var textRegions = pageXml.Layout.Pages[0].PrintSpace.TextBlock;
                Assert.Single(textRegions);

                var textLines = textRegions.Single().TextLines;
                Assert.Single(textLines);

                var strings = textLines.Single().Strings;
                Assert.Equal(2, strings.Length);

                Assert.Equal("TM", strings[0].Content);
                Assert.Equal("10x0623450x066780x06ABC", strings[1].Content);
            }
        }

        [Fact]
        public void Issue655CustomStrategy()
        {
            var hex_0x0006 = IntegrationHelpers.GetDocumentPath("hex_0x0006.pdf");

            var pageXmlTextExporter = new AltoXmlTextExporter(
                DefaultWordExtractor.Instance,
                RecursiveXYCut.Instance, 1.0, "\t",
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
                File.WriteAllText("issue655.custom.altoxml.xml", xml);

                var pageXml = AltoXmlTextExporter.Deserialize("issue655.custom.altoxml.xml");

                var textRegions = pageXml.Layout.Pages[0].PrintSpace.TextBlock;
                Assert.Single(textRegions);

                var textLines = textRegions.Single().TextLines;
                Assert.Single(textLines);

                var strings = textLines.Single().Strings;
                Assert.Equal(2, strings.Length);

                Assert.Equal("TM", strings[0].Content);
                Assert.Equal("1!?2345!?678!?ABC", strings[1].Content);
            }
        }
    }
}
