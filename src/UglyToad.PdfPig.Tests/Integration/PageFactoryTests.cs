namespace UglyToad.PdfPig.Tests.Integration
{
    using System.Collections.Generic;
    using UglyToad.PdfPig.Content;
    using UglyToad.PdfPig.Filters;
    using UglyToad.PdfPig.Geometry;
    using UglyToad.PdfPig.Logging;
    using UglyToad.PdfPig.Outline;
    using UglyToad.PdfPig.Parser;
    using UglyToad.PdfPig.Tokenization.Scanner;
    using UglyToad.PdfPig.Tokens;
    using Xunit;

    public class PageFactoryTests
    {
        [Fact]
        public void SimpleFactory1()
        {
            var file = IntegrationHelpers.GetDocumentPath("Various Content Types");

            using (var document = PdfDocument.Open(file))
            {
                document.AddPageFactory<SimplePage>(typeof(SimplePageFactory));

                var page = document.GetPage<SimplePage>(1);
                Assert.Equal(1, page.Number);

                page = document.GetPage<SimplePage>(1);
                Assert.Equal(1, page.Number);
            }
        }

        [Fact]
        public void SimpleFactory2()
        {
            var file = IntegrationHelpers.GetDocumentPath("Various Content Types");

            using (var document = PdfDocument.Open(file))
            {
                document.AddPageFactory(new SimplePageFactory());

                var page = document.GetPage<SimplePage>(1);
                Assert.Equal(1, page.Number);

                page = document.GetPage<SimplePage>(1);
                Assert.Equal(1, page.Number);
            }
        }

        [Fact]
        public void InformationFactory()
        {
            var file = IntegrationHelpers.GetDocumentPath("Various Content Types");

            using (var document = PdfDocument.Open(file))
            {
                document.AddPageFactory<PageInformation>(typeof(PageInformationFactory));

                Page page = document.GetPage(1);

                PageInformation pageInfo = document.GetPage<PageInformation>(1);
                Assert.Equal(page.Number, pageInfo.Number);
                Assert.Equal(page.Rotation, pageInfo.Rotation);
                Assert.Equal(page.MediaBox.Bounds, pageInfo.MediaBox.Bounds);
                Assert.Equal(page.CropBox.Bounds, pageInfo.CropBox.Bounds);
                //Assert.Equal(page.Unit, pageInfo.UserSpaceUnit);

                pageInfo = document.GetPage<PageInformation>(1);
                Assert.Equal(page.Number, pageInfo.Number);
                Assert.Equal(page.Rotation, pageInfo.Rotation);
                Assert.Equal(page.MediaBox.Bounds, pageInfo.MediaBox.Bounds);
                Assert.Equal(page.CropBox.Bounds, pageInfo.CropBox.Bounds);
            }
        }

        #region SimplePage
        public class SimplePage
        {
            public int Number { get; }

            public int Rotation { get; }

            public MediaBox MediaBox { get; }

            public SimplePage(int number, int rotation, MediaBox mediaBox)
            {
                Number = number;
                Rotation = rotation;
                MediaBox = mediaBox;
            }
        }

        public class SimplePageFactory : IPageFactory<SimplePage>
        {
            public SimplePageFactory()
            {
                // do nothing
            }

            public SimplePageFactory(
                IPdfTokenScanner pdfScanner,
                IResourceStore resourceStore,
                ILookupFilterProvider filterProvider,
                IPageContentParser pageContentParser,
                ILog log)
            { }

            public SimplePage Create(int number, DictionaryToken dictionary, PageTreeMembers pageTreeMembers, NamedDestinations annotationProvider, IParsingOptions parsingOptions)
            {
                return new SimplePage(number, pageTreeMembers.Rotation, pageTreeMembers.MediaBox);
            }
        }
        #endregion

        #region PageInformation
        public class PageInformation
        {
            public int Number { get; set; }

            public PageRotationDegrees Rotation { get; set; }

            public MediaBox MediaBox { get; set; }

            public CropBox CropBox { get; set; }

            public UserSpaceUnit UserSpaceUnit { get; set; }
        }

        public class PageInformationFactory : PageFactoryBase<PageInformation>
        {
            public PageInformationFactory(
                IPdfTokenScanner pdfScanner,
                IResourceStore resourceStore,
                ILookupFilterProvider filterProvider,
                IPageContentParser pageContentParser,
                ILog log)
                : base(pdfScanner, resourceStore, filterProvider, pageContentParser, log)
            {
            }

            protected override PageInformation ProcessPage(
                int pageNumber,
                DictionaryToken dictionary,
                NamedDestinations namedDestinations,
                IReadOnlyList<byte> contentBytes,
                CropBox cropBox,
                UserSpaceUnit userSpaceUnit,
                PageRotationDegrees rotation,
                MediaBox mediaBox,
                IParsingOptions parsingOptions)
            {
                return ProcessPage(pageNumber, dictionary, namedDestinations, cropBox, userSpaceUnit, rotation, mediaBox, parsingOptions);
            }

            protected override PageInformation ProcessPage(int pageNumber,
                DictionaryToken dictionary,
                NamedDestinations namedDestinations,
                CropBox cropBox,
                UserSpaceUnit userSpaceUnit,
                PageRotationDegrees rotation,
                MediaBox mediaBox,
                IParsingOptions parsingOptions)
            {
                return new PageInformation()
                {
                    Number = pageNumber,
                    Rotation = rotation,
                    MediaBox = mediaBox,
                    CropBox = cropBox,
                    UserSpaceUnit = userSpaceUnit
                };
            }
        }
        #endregion
    }
}
