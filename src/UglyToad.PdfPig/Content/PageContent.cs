namespace UglyToad.PdfPig.Content
{
    using System;
    using System.Collections.Generic;
    using Core;
    using Filters;
    using Graphics;
    using Graphics.Operations;
    using Tokenization.Scanner;
    using XObjects;
    using Geometry;

    /// <summary>
    /// Wraps content parsed from a page content stream for access.
    /// </summary>
    /// <remarks>
    /// This should contain a replayable stack of drawing instructions for page content
    /// from a content stream in addition to lazily evaluated state such as text on the page or images.
    /// </remarks>
    internal class PageContent
    {
        private readonly IReadOnlyList<Union<XObjectContentRecord, InlineImage>> images;
        private readonly IReadOnlyList<PdfMarkedContent> markedContents;
        private readonly IPdfTokenScanner pdfScanner;
        private readonly IFilterProvider filterProvider;
        private readonly IResourceStore resourceStore;
        private readonly bool isLenientParsing;

        internal IReadOnlyList<IGraphicsStateOperation> GraphicsStateOperations { get; }

        public IReadOnlyList<Letter> Letters { get; }

        public IReadOnlyList<PdfPath> Paths { get; }

        internal PageContent(IReadOnlyList<IGraphicsStateOperation> graphicsStateOperations, IReadOnlyList<Letter> letters,
            IReadOnlyList<PdfPath> paths,
            IReadOnlyList<Union<XObjectContentRecord, InlineImage>> images,
            IReadOnlyList<PdfMarkedContent> markedContents,
            IPdfTokenScanner pdfScanner,
            IFilterProvider filterProvider,
            IResourceStore resourceStore,
            bool isLenientParsing)
        {
            GraphicsStateOperations = graphicsStateOperations;
            Letters = letters;
            Paths = paths;
            this.images = images;
            this.markedContents = markedContents;
            this.pdfScanner = pdfScanner ?? throw new ArgumentNullException(nameof(pdfScanner));
            this.filterProvider = filterProvider ?? throw new ArgumentNullException(nameof(filterProvider));
            this.resourceStore = resourceStore ?? throw new ArgumentNullException(nameof(resourceStore));
            this.isLenientParsing = isLenientParsing;
        }

        public IEnumerable<IPdfImage> GetImages()
        {
            foreach (var image in images)
            {

                IPdfImage result = null;
                image.Match(x => { result = XObjectFactory.ReadImage(x, pdfScanner, filterProvider, resourceStore, isLenientParsing); },
                    x => { result = x; });

                yield return result;
            }
        }

        public IReadOnlyList<PdfMarkedContent> GetMarkedContents() => markedContents;
    }
}
