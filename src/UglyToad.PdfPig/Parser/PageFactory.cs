namespace UglyToad.PdfPig.Parser
{
    using System.Collections.Generic;
    using Annotations;
    using Content;
    using Core;
    using Filters;
    using Geometry;
    using Graphics;
    using Graphics.Operations;
    using Outline.Destinations;
    using Tokenization.Scanner;
    using Tokens;

    internal class PageFactory : BasePageFactory<Page>
    {
        public PageFactory(
            IPdfTokenScanner pdfScanner,
            IResourceStore resourceStore,
            ILookupFilterProvider filterProvider,
            IPageContentParser pageContentParser,
            ParsingOptions parsingOptions)
            : base(pdfScanner, resourceStore, filterProvider, pageContentParser, parsingOptions)
        {
        }

        protected override Page ProcessPage(int pageNumber,
            DictionaryToken dictionary,
            NamedDestinations namedDestinations,
            MediaBox mediaBox,
            CropBox cropBox,
            UserSpaceUnit userSpaceUnit,
            PageRotationDegrees rotation,
            TransformationMatrix initialMatrix,
            IReadOnlyList<IGraphicsStateOperation> operations)
        {
            var annotationProvider = new AnnotationProvider(PdfScanner,
                dictionary,
                initialMatrix,
                namedDestinations,
                ParsingOptions.Logger);

            if (operations == null || operations.Count == 0)
            {
                PageContent emptyContent = new PageContent(EmptyArray<IGraphicsStateOperation>.Instance,
                    EmptyArray<Letter>.Instance,
                    EmptyArray<PdfPath>.Instance,
                    EmptyArray<Union<XObjectContentRecord, InlineImage>>.Instance,
                    EmptyArray<MarkedContentElement>.Instance,
                    PdfScanner,
                    FilterProvider,
                    ResourceStore);

                return new Page(pageNumber,
                    dictionary,
                    mediaBox,
                    cropBox,
                    rotation,
                    emptyContent,
                    annotationProvider,
                    PdfScanner);
            }

            var context = new ContentStreamProcessor(
                pageNumber,
                ResourceStore,
                PdfScanner,
                PageContentParser,
                FilterProvider,
                cropBox,
                userSpaceUnit,
                rotation,
                initialMatrix,
                ParsingOptions);

            PageContent content = context.Process(pageNumber, operations);

            return new Page(pageNumber,
                dictionary,
                mediaBox,
                cropBox,
                rotation,
                content,
                annotationProvider,
                PdfScanner);
        }
    }
}
