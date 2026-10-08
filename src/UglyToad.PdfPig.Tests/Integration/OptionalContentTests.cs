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
        // tick, drawn mirrored so that together with the visible tick they form an X. The visible paragraph
        // also quotes the layer names, so the labels are counted rather than looked for.
        // GWG150: /D with alternate /Configs; GWG151: radio-button group; GWG152: membership dictionaries.
        [Theory]
        [InlineData("GWG150_OptionalContent-OCCD_X4", 11, 6)]
        [InlineData("GWG151_OptionalContent-RBGroup_X4", 10, 6)]
        [InlineData("GWG152_OptionalContent-OCMD_X4", 11, 6)]
        public void SkipHiddenOptionalContent(string document, int allPaths, int visiblePaths)
        {
            var path = IntegrationHelpers.GetDocumentPath(document);

            int allLabels;
            using (var all = PdfDocument.Open(path))
            {
                // Off by default: hidden content is still returned.
                var page = all.GetPage(1);

                Assert.Contains("Default View", page.Text);
                Assert.EndsWith("GWG View 1GWG View 2", page.Text);
                Assert.Equal(allPaths, page.Paths.Count);
                allLabels = CountOccurrences(page.Text, "GWG View 1");
            }

            using (var visible = PdfDocument.Open(path, new ParsingOptions { SkipHiddenOptionalContent = true }))
            {
                var page = visible.GetPage(1);

                Assert.Contains("Default View", page.Text);
                Assert.DoesNotContain("GWG View 1GWG View 2", page.Text);
                Assert.Equal(allLabels - 1, CountOccurrences(page.Text, "GWG View 1"));
                Assert.Equal(visiblePaths, page.Paths.Count);
            }
        }

        private static int CountOccurrences(string text, string value)
        {
            int count = 0;
            for (int i = text.IndexOf(value, StringComparison.Ordinal); i >= 0; i = text.IndexOf(value, i + value.Length, StringComparison.Ordinal))
            {
                count++;
            }

            return count;
        }

        [Theory]
        [InlineData("odwriteex.pdf")]
        [InlineData("Layer pdf - 322_High_Holborn_building_Brochure.pdf")]
        public void SkipHiddenOptionalContentNeverAddsContent(string document)
        {
            var path = IntegrationHelpers.GetDocumentPath(document);

            using (var all = PdfDocument.Open(path))
            using (var visible = PdfDocument.Open(path, new ParsingOptions { SkipHiddenOptionalContent = true }))
            {
                for (int p = 1; p <= all.NumberOfPages; p++)
                {
                    var allPage = all.GetPage(p);
                    var visiblePage = visible.GetPage(p);

                    Assert.True(visiblePage.Letters.Count <= allPage.Letters.Count);
                    Assert.True(visiblePage.Paths.Count <= allPage.Paths.Count);
                    Assert.True(visiblePage.GetImages().Count() <= allPage.GetImages().Count());
                }
            }
        }
    }
}
