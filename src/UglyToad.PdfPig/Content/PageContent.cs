namespace UglyToad.PdfPig.Content
{
    using Core;
    using Filters;
    using Graphics;
    using Graphics.Operations;
    using System;
    using System.Collections.Generic;
    using Tokenization.Scanner;
    using UglyToad.PdfPig.Parser;
    using XObjects;

    /// <summary>
    /// Wraps content parsed from a page content stream for access.
    /// </summary>
    /// <remarks>
    /// This should contain a replayable stack of drawing instructions for page content
    /// from a content stream in addition to lazily evaluated state such as text on the page or images.
    /// </remarks>
    internal class PageContent
    {
        internal readonly IReadOnlyList<Union<XObjectContentRecord, InlineImage>> images;
        internal readonly IReadOnlyList<MarkedContentElement> markedContents;
        internal readonly IPdfTokenScanner pdfScanner;
        internal readonly IPageContentParser pageContentParser;
        internal readonly IFilterProvider filterProvider;
        internal readonly IResourceStore resourceStore;

        internal IReadOnlyList<IGraphicsStateOperation> GraphicsStateOperations { get; }

        public IReadOnlyList<Letter> Letters { get; }

        public IReadOnlyList<PdfPath> Paths { get; }

        public int NumberOfImages => images.Count;

        internal PageContent(IReadOnlyList<IGraphicsStateOperation> graphicsStateOperations, IReadOnlyList<Letter> letters,
            IReadOnlyList<PdfPath> paths,
            IReadOnlyList<Union<XObjectContentRecord, InlineImage>> images,
            IReadOnlyList<MarkedContentElement> markedContents,
            IPdfTokenScanner pdfScanner,
            IPageContentParser pageContentParser,
            IFilterProvider filterProvider,
            IResourceStore resourceStore)
        {
            GraphicsStateOperations = graphicsStateOperations;
            Letters = letters;
            Paths = paths;
            this.images = images;
            this.markedContents = markedContents;
            this.pdfScanner = pdfScanner ?? throw new ArgumentNullException(nameof(pdfScanner));
            this.pageContentParser = pageContentParser ?? throw new ArgumentNullException(nameof(pageContentParser));
            this.filterProvider = filterProvider ?? throw new ArgumentNullException(nameof(filterProvider));
            this.resourceStore = resourceStore ?? throw new ArgumentNullException(nameof(resourceStore));
        }

        public IEnumerable<IPdfImage> GetImages()
        {
            foreach (var image in images)
            {
                if (image.TryGetFirst(out var xObjectContentRecord))
                {
                    yield return XObjectFactory.ReadImage(xObjectContentRecord, pdfScanner, filterProvider, resourceStore);
                }
                else if (image.TryGetSecond(out var inlineImage))
                {
                    yield return inlineImage;
                }
            }
        }

        public IReadOnlyList<MarkedContentElement> GetMarkedContents() => markedContents;
    }
}
