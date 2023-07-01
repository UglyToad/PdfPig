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

    internal class PageFactory : PageFactoryBase<Page>
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
                resourceStore,
                userSpaceUnit,
                mediaBox,
                cropBox,
                rotation,
                pdfScanner,
                pageContentParser,
                filterProvider,
                parsingOptions);

            var operations = pageContentParser.Parse(pageNumber, new ByteArrayInputBytes(contentBytes), parsingOptions.Logger);
            var content = context.Process(pageNumber, operations);

            var initialMatrix = ContentStreamProcessor.GetInitialMatrix(userSpaceUnit, mediaBox, cropBox, rotation, log);
            var annotationProvider = new AnnotationProvider(pdfScanner, dictionary, initialMatrix, namedDestinations, log);
            return new Page(pageNumber, dictionary, mediaBox, cropBox, rotation, content, annotationProvider, pdfScanner);
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
            var initialMatrix = ContentStreamProcessor.GetInitialMatrix(userSpaceUnit, mediaBox, cropBox, rotation, log);
            var annotationProvider = new AnnotationProvider(pdfScanner, dictionary, initialMatrix, namedDestinations, log);

            var content = new PageContent(EmptyArray<IGraphicsStateOperation>.Instance,
                EmptyArray<Letter>.Instance,
                EmptyArray<PdfPath>.Instance,
                EmptyArray<Union<XObjectContentRecord, InlineImage>>.Instance,
                EmptyArray<MarkedContentElement>.Instance,
                pdfScanner,
                filterProvider,
                resourceStore);
            // ignored for now, is it possible? check the spec...

            return new Page(pageNumber, dictionary, mediaBox, cropBox, rotation, content, annotationProvider, pdfScanner);
        }
    }
}
