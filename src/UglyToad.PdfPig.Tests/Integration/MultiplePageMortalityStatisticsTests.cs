namespace UglyToad.PdfPig.Tests.Integration
{
    using System.IO;
    using Content;
    using Xunit;

    public class MultiplePageMortalityStatisticsTests
    {
        private static string GetFilename()
        {
            return IntegrationHelpers.GetDocumentPath("Multiple Page - from Mortality Statistics.pdf");
        }

        [Fact]
        public void HasCorrectNumberOfPages()
        {
            var file = GetFilename();

            using (var document = PdfDocument.Open(File.ReadAllBytes(file)))
            {
                Assert.Equal(6, document.NumberOfPages);
            }
        }

        [Fact]
        public void HasCorrectVersion()
        {
            using (var document = PdfDocument.Open(GetFilename()))
            {
                Assert.Equal(1.7m, document.Version);
            }
        }

        [Fact]
        public void GetsFirstPageContent()
        {
            using (var document = PdfDocument.Open(GetFilename()))
            {
                var page = document.GetPage(1);

                Assert.Contains("Mortality Statistics: Metadata", page.Text);
                Assert.Contains("Notification to the registrar by the coroner that he does not consider it necessary to hold an inquest – no post-mortem held (Form 100A – salmon pink)", page.Text);
                Assert.Contains("Presumption of death certificate", page.Text);

                Assert.Equal(PageSize.Letter, page.Size);
            }
        }

        [Fact]
        public void GetsPagesContent()
        {
            using (var document = PdfDocument.Open(GetFilename()))
            {
                var pages = new[]
                {
                    document.GetPage(1),
                    document.GetPage(2),
                    document.GetPage(3),
                    document.GetPage(4),
                    document.GetPage(5),
                    document.GetPage(6)
                };

                Assert.Contains(@"Up to 1992, publications gave numbers of deaths registered in the period concerned. From 1993 to 2005, the figures in annual reference volumes relate to the number of deaths that "
+ "occurred in the reference period. From 2006 onwards, all tables in Series DR are based on "
+ "deaths registered in a calendar period. More details on these changes can be found in the "
+ "publication Mortality Statistics: Deaths Registered in 2006 (ONS, 2008)", pages[5].Text);
            }
        }
    }
}
