namespace UglyToad.PdfPig.Tests.Integration
{
    using Annotations;
    using UglyToad.PdfPig.Actions;

    public class AnnotationsTest
    {
        [Fact]
        public void DocHasRenditionAction()
        {
            var pdf = IntegrationHelpers.GetSpecificTestDocumentPath("GuitarLovers-LicksRiffs-5v0-en-demo.pdf");
            using (var document = PdfDocument.Open(pdf, new ParsingOptions() { UseLenientParsing = true }))
            {
                var page = document.GetPage(5);
                var annotations = page.GetAnnotations().ToArray();
                Assert.Equal(5, annotations.Length);
                
                foreach (var annotation in annotations)
                {
                    Assert.Equal(AnnotationType.Screen, annotation.Type);
                    Assert.True(annotation.Rectangle.Area > 0);
                    Assert.IsType<RenditionAction>(annotation.Action);
                    Assert.Equal(RenditionOperation.PlayAndAssociate, ((RenditionAction)annotation.Action).Operation);
                }
            }
        }

        [Fact]
        public void DocHasNamedAction()
        {
            var pdf = IntegrationHelpers.GetSpecificTestDocumentPath("0000301.pdf");
            using (var document = PdfDocument.Open(pdf, new ParsingOptions() { UseLenientParsing = true }))
            {
                var page = document.GetPage(1);
                var annotations = page.GetAnnotations().ToArray();
                Assert.Equal(21, annotations.Length);

                var goTo = annotations[0];
                Assert.Equal(AnnotationType.Link, goTo.Type);
                Assert.True(goTo.Rectangle.Area > 0);
                Assert.IsType<GoToAction>(goTo.Action);

                var named = annotations[1];
                Assert.Equal(AnnotationType.Link, named.Type);
                Assert.True(named.Rectangle.Area > 0);
                Assert.IsType<NamedAction>(named.Action);
            }
        }

        [Fact]
        public void DocHasSubmitFormAction()
        {
            var pdf = IntegrationHelpers.GetSpecificTestDocumentPath("0000505.pdf");
            using (var document = PdfDocument.Open(pdf, new ParsingOptions() { UseLenientParsing = true }))
            {
                var page = document.GetPage(1);
                var annotations = page.GetAnnotations().ToArray();
                Assert.Equal(28, annotations.Length);

                var uri = annotations[0];
                Assert.Equal(AnnotationType.Link, uri.Type);
                Assert.True(uri.Rectangle.Area > 0);
                Assert.IsType<UriAction>(uri.Action);

                var submitForm = annotations[3];
                Assert.Equal(AnnotationType.Widget, submitForm.Type);
                Assert.True(submitForm.Rectangle.Area > 0);
                Assert.IsType<SubmitFormAction>(submitForm.Action);

                var goTo = annotations[26];
                Assert.Equal(AnnotationType.Link, goTo.Type);
                Assert.True(goTo.Rectangle.Area > 0);
                Assert.IsType<GoToAction>(goTo.Action);
            }
        }

        [Fact]
        public void AnnotationsHaveActions()
        {
            var pdf = IntegrationHelpers.GetDocumentPath("toc");

            using (var doc = PdfDocument.Open(pdf))
            {
                var annots = doc.GetPage(1).GetAnnotations().ToArray();
                Assert.Equal(5, annots.Length);
                Assert.All(annots, a => Assert.NotNull(a.Action));
                Assert.All(annots, a => Assert.IsType<GoToAction>(a.Action));
                Assert.All(annots, a => Assert.True((a.Action as GoToAction).Destination.PageNumber > 0));
            }
        }

        [Fact]
        public void CheckAnnotationAppearanceStreams()
        {
            var pdf = IntegrationHelpers.GetSpecificTestDocumentPath("appearances");
            using (var doc = PdfDocument.Open(pdf))
            {
                var annotations = doc.GetPage(1).GetAnnotations().ToArray();
                var annotation = Assert.Single(annotations);

                Assert.True(annotation.HasDownAppearance);
                Assert.True(annotation.HasNormalAppearance);
                Assert.False(annotation.HasRollOverAppearance);

                Assert.False(annotation.downAppearanceStream.IsStateless);
                Assert.False(annotation.normalAppearanceStream.IsStateless);

                Assert.Contains("Off", annotation.downAppearanceStream.GetStates);
                Assert.Contains("Yes", annotation.downAppearanceStream.GetStates);

                Assert.Contains("Off", annotation.normalAppearanceStream.GetStates);
                Assert.Contains("Yes", annotation.normalAppearanceStream.GetStates);
                
                Assert.Equal("Off", annotation.appearanceState);
            }
        }
    }
}
