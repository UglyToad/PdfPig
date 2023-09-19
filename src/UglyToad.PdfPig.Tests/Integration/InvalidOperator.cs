namespace UglyToad.PdfPig.Tests.Integration
{
    using System;
    using Xunit;

    public class InvalidOperatorTests
    {
        [Fact]
        public void InvalidOperatorThrowsExceptionIfNotUsingLenientParsing()
        {
            var path = IntegrationHelpers.GetSpecificTestDocumentPath("invalid-operator.pdf");

            using (var document = PdfDocument.Open(path, new ParsingOptions { UseLenientParsing = false }))
            {
                Assert.Throws<ArgumentException>(() => document.GetPage(1));
            }
        }

        [Fact]
        public void InvalidOperatorDoesNotThrowExceptionIfUsingLenientParsing()
        {
            var path = IntegrationHelpers.GetSpecificTestDocumentPath("invalid-operator.pdf");

            using (var document = PdfDocument.Open(path, new ParsingOptions { UseLenientParsing = true }))
            {
                var page = document.GetPage(1);
                var text = page.Text;
                Assert.Contains(text, "Text line 1");
                Assert.Contains(text, "Text line 2");
            }
        }
    }
}
