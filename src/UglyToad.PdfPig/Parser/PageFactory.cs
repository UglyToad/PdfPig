namespace UglyToad.PdfPig.Parser
{
    using Annotations;
    using Content;
    using Filters;
    using Geometry;
    using Graphics;
    using Graphics.Operations;
    using Logging;
    using Outline;
    using System.Collections.Generic;
    using Tokenization.Scanner;
    using Tokens;
    using UglyToad.PdfPig.Core;

    internal sealed class PageFactory : PageFactoryBase<Page>
    {
        public PageFactory(
            IPdfTokenScanner pdfScanner,
            IResourceStore resourceStore,
            ILookupFilterProvider filterProvider,
            IPageContentParser pageContentParser,
            ILog log)
            : base(pdfScanner, resourceStore, filterProvider, pageContentParser, log)
        { }

        protected override Page ProcessPage(int pageNumber,
            DictionaryToken dictionary,
            NamedDestinations namedDestinations,
            IReadOnlyList<byte> contentBytes,
            CropBox cropBox,
            UserSpaceUnit userSpaceUnit,
            PageRotationDegrees rotation,
            MediaBox mediaBox,
            IParsingOptions parsingOptions)
        {
            var context = new ContentStreamProcessor(
                pageNumber,
                ResourceStore,
                userSpaceUnit,
                mediaBox,
                cropBox,
                rotation,
                PdfScanner,
                PageContentParser,
                FilterProvider,
                parsingOptions);

            var operations = PageContentParser.Parse(pageNumber, new ByteArrayInputBytes(contentBytes), parsingOptions.Logger);
            var content = context.Process(pageNumber, operations);

            var initialMatrix = StreamProcessorHelper.GetInitialMatrix(userSpaceUnit, mediaBox, cropBox, rotation, Log);
            var annotationProvider = new AnnotationProvider(PdfScanner, dictionary, initialMatrix, namedDestinations, Log);
            return new Page(pageNumber, dictionary, mediaBox, cropBox, rotation, content, annotationProvider, PdfScanner);
        }

        protected override Page ProcessPage(
            int pageNumber,
            DictionaryToken dictionary,
            NamedDestinations namedDestinations,
            CropBox cropBox,
            UserSpaceUnit userSpaceUnit,
            PageRotationDegrees rotation,
            MediaBox mediaBox,
            IParsingOptions parsingOptions)
        {
            var initialMatrix = StreamProcessorHelper.GetInitialMatrix(userSpaceUnit, mediaBox, cropBox, rotation, Log);
            var annotationProvider = new AnnotationProvider(PdfScanner, dictionary, initialMatrix, namedDestinations, Log);

            var content = new PageContent(EmptyArray<IGraphicsStateOperation>.Instance,
                EmptyArray<Letter>.Instance,
                EmptyArray<PdfPath>.Instance,
                EmptyArray<Union<XObjectContentRecord, InlineImage>>.Instance,
                EmptyArray<MarkedContentElement>.Instance,
                PdfScanner,
                FilterProvider,
                ResourceStore);
            // ignored for now, is it possible? check the spec...

            return new Page(pageNumber, dictionary, mediaBox, cropBox, rotation, content, annotationProvider, PdfScanner);
        }
    }
}
