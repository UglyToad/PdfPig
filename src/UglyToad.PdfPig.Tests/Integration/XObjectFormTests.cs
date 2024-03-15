namespace UglyToad.PdfPig.Tests.Integration
{
    using UglyToad.PdfPig.Core;

    public class XObjectFormTests
    {
        [Fact]
        public void CanReadDocumentWithoutStackOverflowIssue671()
        {
            using (var document = PdfDocument.Open(IntegrationHelpers.GetDocumentPath("issue_671")))
            {
                var page = document.GetPage(1);
            }
        }

        [Fact]
        public void CanReadDocumentThrowsIssue671()
        {
            using (var document = PdfDocument.Open(IntegrationHelpers.GetDocumentPath("issue_671"), ParsingOptions.LenientParsingOff))
            {
                var exception = Assert.Throws<PdfDocumentFormatException>(() => document.GetPage(1));
                Assert.Contains("is referencing itself which can cause unexpected behaviour", exception.Message);
            }
        }

        [Fact]
        public void CanReadDocumentMOZILLA_3136_0()
        {
            // This document does not actually contain circular references
            using (var document = PdfDocument.Open(IntegrationHelpers.GetDocumentPath("MOZILLA-3136-0"), ParsingOptions.LenientParsingOff))
            {
                var page = document.GetPage(1);
            }
        }
    }
}
