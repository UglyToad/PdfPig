using System;
using UglyToad.PdfPig.Tokens;
using Xunit;

namespace UglyToad.PdfPig.Tests.Integration
{
    public class OptionalContentTests
    {
        [Fact]
        public void MarkedOptionalContent()
        {
            using (var document = PdfDocument.Open(IntegrationHelpers.GetDocumentPath("odwriteex.pdf")))
            {
                var page = document.GetPage(1);
                var oc = page.ExperimentalAccess.GetOptionalContents();

                Assert.Equal(3, oc.Count);

                Assert.Contains("0", oc);
                Assert.Contains("Dimentions", oc);
                Assert.Contains("Text", oc);

                Assert.Equal(1, oc["0"].Count);
                Assert.Equal(2, oc["Dimentions"].Count);
                Assert.Equal(1, oc["Text"].Count);
            }
        }

        [Fact]
        public void MarkedOptionalContentRecursion()
        {
            using (var document = PdfDocument.Open(IntegrationHelpers.GetDocumentPath("Layer pdf - 322_High_Holborn_building_Brochure.pdf")))
            {
                var page1 = document.GetPage(1);
                var oc1 = page1.ExperimentalAccess.GetOptionalContents();
                Assert.Equal(16, oc1.Count);
                Assert.Contains("NEW ARRANGEMENT", oc1);

                var page2 = document.GetPage(2);
                var oc2 = page2.ExperimentalAccess.GetOptionalContents();
                Assert.Equal(15, oc2.Count);
                Assert.DoesNotContain("NEW ARRANGEMENT", oc2);
                Assert.Contains("WDL Shell text", oc2);
                Assert.Equal(2, oc2["WDL Shell text"].Count);

                var page3 = document.GetPage(3);
                var oc3 = page3.ExperimentalAccess.GetOptionalContents();
                Assert.Equal(15, oc3.Count);
                Assert.Contains("WDL Shell text", oc3);
                Assert.Equal(2, oc3["WDL Shell text"].Count);
            }
        }
    }
}
