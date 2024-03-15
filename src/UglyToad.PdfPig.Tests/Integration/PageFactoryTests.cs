namespace UglyToad.PdfPig.Tests.Integration
{
    using Content;
    using Outline.Destinations;
    using PdfPig.Core;
    using PdfPig.Filters;
    using PdfPig.Geometry;
    using PdfPig.Graphics.Operations;
    using PdfPig.Parser;
    using PdfPig.Tokenization.Scanner;
    using PdfPig.Tokens;

    public class PageFactoryTests
    {
        [Fact]
        public void SimpleFactory1()
        {
            var file = IntegrationHelpers.GetDocumentPath("ICML03-081");

            using (var document = PdfDocument.Open(file))
            {
                document.AddPageFactory<SimplePage, SimplePageFactory>();

                for (int p = 1; p < document.NumberOfPages; p++)
                {
                    var page = document.GetPage(p);
                    var pageInfo = document.GetPage<SimplePage>(p);

                    Assert.Equal(page.Number, pageInfo.Number);
                    Assert.Equal(page.Rotation.Value, pageInfo.Rotation);
                    Assert.Equal(page.MediaBox.Bounds, pageInfo.MediaBox.Bounds);
                }
            }
        }

        [Fact]
        public void SimpleFactory2()
        {
            var file = IntegrationHelpers.GetDocumentPath("cat-genetics");

            using (var document = PdfDocument.Open(file))
            {
                document.AddPageFactory(new SimplePageFactory());

                var page = document.GetPage(1);
                var pageInfo = document.GetPage<SimplePage>(1);

                Assert.Equal(page.Number, pageInfo.Number);
                Assert.Equal(page.Rotation.Value, pageInfo.Rotation);
                Assert.Equal(page.MediaBox.Bounds, pageInfo.MediaBox.Bounds);

                // Run again
                pageInfo = document.GetPage<SimplePage>(1);
                Assert.Equal(page.Number, pageInfo.Number);
                Assert.Equal(page.Rotation.Value, pageInfo.Rotation);
                Assert.Equal(page.MediaBox.Bounds, pageInfo.MediaBox.Bounds);
            }
        }

        [Fact]
        public void InformationFactory()
        {
            var file = IntegrationHelpers.GetDocumentPath("Gamebook");

            using (var document = PdfDocument.Open(file))
            {
                document.AddPageFactory<PageInformation, PageInformationFactory>();

                for (int p = 1; p < document.NumberOfPages; p++)
                {
                    var page = document.GetPage(p);

                    var pageInfo = document.GetPage<PageInformation>(p);
                    Assert.Equal(page.Number, pageInfo.Number);
                    Assert.Equal(page.Rotation, pageInfo.Rotation);
                    Assert.Equal(page.Width, pageInfo.Width);
                    Assert.Equal(page.Height, pageInfo.Height);

                    // Run again
                    pageInfo = document.GetPage<PageInformation>(p);
                    Assert.Equal(page.Number, pageInfo.Number);
                    Assert.Equal(page.Rotation, pageInfo.Rotation);
                    Assert.Equal(page.Width, pageInfo.Width);
                    Assert.Equal(page.Height, pageInfo.Height);
                }
            }
        }

        [Fact]
        public void SimpleAndInformationFactory()
        {
            var file = IntegrationHelpers.GetDocumentPath("DeviceN_CS_test");

            using (var document = PdfDocument.Open(file))
            {
                document.AddPageFactory<PageInformation, PageInformationFactory>();
                document.AddPageFactory<SimplePage, SimplePageFactory>();

                for (int p = 1; p < document.NumberOfPages; p++)
                {
                    var page = document.GetPage(p);

                    var pageInfo = document.GetPage<PageInformation>(p);
                    Assert.Equal(page.Number, pageInfo.Number);
                    Assert.Equal(page.Rotation, pageInfo.Rotation);
                    Assert.Equal(page.Width, pageInfo.Width);
                    Assert.Equal(page.Height, pageInfo.Height);

                    var simplePage = document.GetPage<SimplePage>(p);
                    Assert.Equal(page.Number, simplePage.Number);
                    Assert.Equal(page.Rotation.Value, simplePage.Rotation);
                    Assert.Equal(page.MediaBox.Bounds, simplePage.MediaBox.Bounds);
                }
            }
        }

        [Fact]
        public void NoPageFactory()
        {
            var file = IntegrationHelpers.GetDocumentPath("cat-genetics");

            using (var document = PdfDocument.Open(file))
            {
                var exception = Assert.Throws<InvalidOperationException>(() => document.GetPage<SimplePage>(1));
                Assert.StartsWith("Could not find page factory of type", exception.Message);
            }
        }

        [Fact]
        public void WrongSignatureFactory()
        {
            var file = IntegrationHelpers.GetDocumentPath("Gamebook");

            using (var document = PdfDocument.Open(file))
            {
                var exception = Assert.Throws<InvalidOperationException>(() =>
                    document.AddPageFactory<PageInformation, WrongConstructorFactory>());
                Assert.StartsWith("Could not find valid constructor for page factory of type ", exception.Message);
            }
        }

        #region Wrong

        public class WrongConstructorFactory : BasePageFactory<PageInformation>
        {
            public WrongConstructorFactory(
                IResourceStore resourceStore,
                ILookupFilterProvider filterProvider,
                IPageContentParser pageContentParser,
                ParsingOptions parsingOptions)
                : base(null, resourceStore, filterProvider, pageContentParser, parsingOptions)
            {
            }

            protected override PageInformation ProcessPage(int pageNumber,
                DictionaryToken dictionary,
                NamedDestinations namedDestinations,
                MediaBox mediaBox,
                CropBox cropBox,
                UserSpaceUnit userSpaceUnit,
                PageRotationDegrees rotation,
                TransformationMatrix initialMatrix,
                IReadOnlyList<IGraphicsStateOperation> operations)
            {
                throw new Exception();
            }
        }

        #endregion

        #region SimplePage

        public sealed class SimplePage
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

        public sealed class SimplePageFactory : IPageFactory<SimplePage>
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
                ParsingOptions parsingOptions)
            {
                // do nothing
            }

            public SimplePage Create(int number,
                DictionaryToken dictionary,
                PageTreeMembers pageTreeMembers,
                NamedDestinations namedDestinations)
            {
                return new SimplePage(number, pageTreeMembers.Rotation, pageTreeMembers.MediaBox);
            }
        }

        #endregion

        #region PageInformation

        public readonly struct PageInformation
        {
            public int Number { get; }

            public PageRotationDegrees Rotation { get; }

            public double Width { get; }

            public double Height { get; }

            public UserSpaceUnit UserSpaceUnit { get; }

            public PageInformation(int number,
                PageRotationDegrees rotation,
                double width,
                double height,
                UserSpaceUnit userSpaceUnit)
            {
                Number = number;
                Rotation = rotation;
                Width = width;
                Height = height;
                UserSpaceUnit = userSpaceUnit;
            }
        }

        public sealed class PageInformationFactory : BasePageFactory<PageInformation>
        {
            public PageInformationFactory(
                IPdfTokenScanner pdfScanner,
                IResourceStore resourceStore,
                ILookupFilterProvider filterProvider,
                IPageContentParser pageContentParser,
                ParsingOptions parsingOptions)
                : base(pdfScanner, resourceStore, filterProvider, pageContentParser, parsingOptions)
            {
            }

            protected override PageInformation ProcessPage(int pageNumber,
                DictionaryToken dictionary,
                NamedDestinations namedDestinations,
                MediaBox mediaBox,
                CropBox cropBox,
                UserSpaceUnit userSpaceUnit,
                PageRotationDegrees rotation,
                TransformationMatrix initialMatrix,
                IReadOnlyList<IGraphicsStateOperation> operations)
            {
                // Same logic as in Page class:
                // Special case where cropbox is outside mediabox: use cropbox instead of intersection
                var viewBox = mediaBox.Bounds.Intersect(cropBox.Bounds) ?? cropBox.Bounds;

                return new PageInformation(pageNumber, rotation, viewBox.Width, viewBox.Height, userSpaceUnit);
            }
        }

        #endregion
    }
}
