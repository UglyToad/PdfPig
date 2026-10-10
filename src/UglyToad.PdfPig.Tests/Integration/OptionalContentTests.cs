namespace UglyToad.PdfPig.Tests.Integration
{
    public class OptionalContentTests
    {
        [Fact]
        public void NoMarkedOptionalContent()
        {
            using (var document = PdfDocument.Open(IntegrationHelpers.GetDocumentPath("AcroFormsBasicFields.pdf")))
            {
                var page = document.GetPage(1);
                var oc = page.GetOptionalContents();

                Assert.Empty(oc);
            }
        }

        [Fact]
        public void MarkedOptionalContent()
        {
            using (var document = PdfDocument.Open(IntegrationHelpers.GetDocumentPath("odwriteex.pdf")))
            {
                var page = document.GetPage(1);
                var oc = page.GetOptionalContents();

                Assert.Equal(3, oc.Count);

                Assert.Contains("0", oc);
                Assert.Contains("Dimentions", oc);
                Assert.Contains("Text", oc);

                Assert.Single(oc["0"]);
                Assert.Equal(2, oc["Dimentions"].Count);
                Assert.Single(oc["Text"]);
            }
        }

        [Fact]
        public void MarkedOptionalContentRecursion()
        {
            using (var document = PdfDocument.Open(IntegrationHelpers.GetDocumentPath("Layer pdf - 322_High_Holborn_building_Brochure.pdf")))
            {
                var page1 = document.GetPage(1);
                var oc1 = page1.GetOptionalContents();
                Assert.Equal(16, oc1.Count);
                Assert.Contains("NEW ARRANGEMENT", oc1);

                var page2 = document.GetPage(2);
                var oc2 = page2.GetOptionalContents();
                Assert.Equal(15, oc2.Count);
                Assert.DoesNotContain("NEW ARRANGEMENT", oc2);
                Assert.Contains("WDL Shell text", oc2);
                Assert.Equal(2, oc2["WDL Shell text"].Count);

                var page3 = document.GetPage(3);
                var oc3 = page3.GetOptionalContents();
                Assert.Equal(15, oc3.Count);
                Assert.Contains("WDL Shell text", oc3);
                Assert.Equal(2, oc3["WDL Shell text"].Count);
            }
        }

        // Ghent Workgroup optional content test files: the default configuration shows the "Default View"
        // layer only. The hidden "GWG View 1" / "GWG View 2" layers each carry a label and the paths of a
        // tick, drawn mirrored so that together with the visible tick they form an X.
        // GWG150: /D with alternate /Configs; GWG151: radio-button group; GWG152: membership dictionaries.
        [Theory]
        [InlineData("GWG150_OptionalContent-OCCD_X4", 11)]
        [InlineData("GWG151_OptionalContent-RBGroup_X4", 10)]
        [InlineData("GWG152_OptionalContent-OCMD_X4", 11)]
        public void GetPage_ReturnsEveryLayer(string document, int allPaths)
        {
            var path = IntegrationHelpers.GetDocumentPath(document);

            using var all = PdfDocument.Open(path);

            // Hidden content is returned too: Page does not depend on the document's optional content state.
            var page = all.GetPage(1);

            Assert.Contains("Default View", page.Text);
            Assert.EndsWith("GWG View 1GWG View 2", page.Text);
            Assert.Equal(allPaths, page.Paths.Count);
        }

        [Fact]
        public void DocumentWithoutOptionalContentHasNoState()
        {
            using var document = PdfDocument.Open(IntegrationHelpers.GetDocumentPath("AcroFormsBasicFields.pdf"));

            Assert.Null(document.OptionalContent);
        }
    }
}
