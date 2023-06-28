namespace UglyToad.PdfPig.Tests.Integration
{
    using System;
    using System.IO;
    using System.Text;
    using System.Xml;
    using UglyToad.PdfPig.DocumentLayoutAnalysis.Export;
    using Xunit;

    public class SvgTextExporterTests
    {
        [Fact]
        public void Doc68_1990_01_A()
        {
            var hex_0x0006 = IntegrationHelpers.GetDocumentPath("68-1990-01_A.pdf");

            var pageXmlTextExporter = new SvgTextExporter();

            Assert.Equal(InvalidCharStrategy.DoNotCheck, pageXmlTextExporter.InvalidCharStrategy);

            using (var document = PdfDocument.Open(hex_0x0006))
            {
                var page = document.GetPage(7);

                // Convert page to text
                string xml = pageXmlTextExporter.Get(page);

                // Save text to an xml file
                File.WriteAllText("68-1990-01_A.7.svg", xml);
            }
        }

        [Fact]
        public void Issue655NoCheckStrategy()
        {
            var hex_0x0006 = IntegrationHelpers.GetDocumentPath("hex_0x0006.pdf");

            var pageXmlTextExporter = new SvgTextExporter();

            Assert.Equal(InvalidCharStrategy.DoNotCheck, pageXmlTextExporter.InvalidCharStrategy);

            using (var document = PdfDocument.Open(hex_0x0006))
            {
                var page = document.GetPage(1);

                // Convert page to text
                string xml = pageXmlTextExporter.Get(page);

                // Save text to an xml file
                File.WriteAllText("issue655.nocheck.svg", xml);

                string rawText = File.ReadAllText("issue655.nocheck.svg");
                Assert.Contains(">&#x6;<", rawText);
            }
        }

        [Fact]
        public void Issue655RemoveStrategy()
        {
            var hex_0x0006 = IntegrationHelpers.GetDocumentPath("hex_0x0006.pdf");

            var pageXmlTextExporter = new SvgTextExporter(InvalidCharStrategy.Remove);

            Assert.Equal(InvalidCharStrategy.Remove, pageXmlTextExporter.InvalidCharStrategy);

            using (var document = PdfDocument.Open(hex_0x0006))
            {
                var page = document.GetPage(1);

                // Convert page to text
                string xml = pageXmlTextExporter.Get(page);

                // Save text to an xml file
                File.WriteAllText("issue655.remove.svg", xml);

                string rawText = File.ReadAllText("issue655.remove.svg");
                Assert.DoesNotContain(">0x06<", rawText);
            }
        }

        [Fact]
        public void Issue655ConvertToHexadecimalStrategy()
        {
            var hex_0x0006 = IntegrationHelpers.GetDocumentPath("hex_0x0006.pdf");

            var pageXmlTextExporter = new SvgTextExporter(InvalidCharStrategy.ConvertToHexadecimal);

            Assert.Equal(InvalidCharStrategy.ConvertToHexadecimal, pageXmlTextExporter.InvalidCharStrategy);

            using (var document = PdfDocument.Open(hex_0x0006))
            {
                var page = document.GetPage(1);

                // Convert page to text
                string xml = pageXmlTextExporter.Get(page);

                // Save text to an xml file
                File.WriteAllText("issue655.hex.svg", xml);

                string rawText = File.ReadAllText("issue655.hex.svg");
                Assert.Contains(">0x06<", rawText);
            }
        }

        [Fact]
        public void Issue655CustomStrategy()
        {
            var hex_0x0006 = IntegrationHelpers.GetDocumentPath("hex_0x0006.pdf");

            var pageXmlTextExporter = new SvgTextExporter(
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
                File.WriteAllText("issue655.custom.svg", xml);

                string rawText = File.ReadAllText("issue655.custom.svg");
                Assert.Contains(">!?<", rawText);
            }
        }
    }
}
