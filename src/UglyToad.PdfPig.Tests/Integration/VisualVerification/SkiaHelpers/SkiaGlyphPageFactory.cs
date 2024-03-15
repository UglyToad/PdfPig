namespace UglyToad.PdfPig.Tests.Integration.VisualVerification.SkiaHelpers
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
    using SkiaSharp;

    internal sealed class SkiaGlyphPageFactory : BasePageFactory<SKPicture>
    {
        public SkiaGlyphPageFactory(
            IPdfTokenScanner pdfScanner,
            IResourceStore resourceStore,
            ILookupFilterProvider filterProvider,
            IPageContentParser pageContentParser,
            ParsingOptions parsingOptions)
            : base(pdfScanner, resourceStore, filterProvider, pageContentParser, parsingOptions)
        {
        }

        protected override SKPicture ProcessPage(int pageNumber, DictionaryToken dictionary, NamedDestinations namedDestinations,
            MediaBox mediaBox, CropBox cropBox, UserSpaceUnit userSpaceUnit, PageRotationDegrees rotation,
            TransformationMatrix initialMatrix, IReadOnlyList<IGraphicsStateOperation> operations)
        {
            // Special case where cropbox is outside mediabox: use cropbox instead of intersection
            var effectiveCropBox = new CropBox(mediaBox.Bounds.Intersect(cropBox.Bounds) ?? cropBox.Bounds);

            var context = new SkiaGlyphStreamProcessor(pageNumber, ResourceStore, PdfScanner, PageContentParser,
                FilterProvider, effectiveCropBox, userSpaceUnit, rotation, initialMatrix, ParsingOptions);

            return context.Process(pageNumber, operations);
        }
    }
}
