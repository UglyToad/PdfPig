namespace UglyToad.PdfPig.Tests.Integration
{
    using Xunit;

    public class WrongPathCountClippingTests
    {
        [Fact]
        public void WrongPathCount()
        {
            using (var document = PdfDocument.Open(IntegrationHelpers.GetDocumentPath("Publication_of_award_of_Bids_for_Transport_Sector__August_2016.pdf"),
                new ParsingOptions()
                {
                    ClipPaths = true
                }))
            {
                var page = document.GetPage(1);
                Assert.Equal(612, page.Height);
                Assert.Equal(224, page.ExperimentalAccess.Paths.Count);
            }
        }
    }
}
