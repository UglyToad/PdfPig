namespace UglyToad.PdfPig.Tests.Integration
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Xunit;

    public class AnnotationsTest
    {
        [Fact]
        public void AnnotationsHaveDestinations()
        {
            var pdf = IntegrationHelpers.GetDocumentPath("toc");

            using (var doc = PdfDocument.Open(pdf))
            {
                var annots = doc.GetPage(1).ExperimentalAccess.GetAnnotations().ToArray();
                Assert.Equal(5, annots.Length);
                Assert.All(annots, a => Assert.NotNull(a.Destination));
                Assert.All(annots, a => Assert.True(a.Destination.PageNumber > 0));
            }
        }
    }
}
