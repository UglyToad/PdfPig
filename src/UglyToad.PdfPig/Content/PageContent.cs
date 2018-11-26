namespace UglyToad.PdfPig.Content
{
    using System.Collections.Generic;
    using Graphics;
    using Graphics.Operations;
    using Tokenization.Scanner;
    using XObjects;

    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// This should contain a replayable stack of drawing instructions for page content
    /// from a content stream in addition to lazily evaluated state such as text on the page or images.
    /// </remarks>
    internal class PageContent
    {
        private readonly IReadOnlyDictionary<XObjectType, List<XObjectContentRecord>> xObjects;
        private readonly IPdfTokenScanner pdfScanner;
        private readonly XObjectFactory xObjectFactory;
        private readonly bool isLenientParsing;

        internal IReadOnlyList<IGraphicsStateOperation> GraphicsStateOperations { get; }

        public IReadOnlyList<Letter> Letters { get; }

        internal PageContent(IReadOnlyList<IGraphicsStateOperation> graphicsStateOperations, IReadOnlyList<Letter> letters,
            IReadOnlyDictionary<XObjectType, List<XObjectContentRecord>> xObjects,
            IPdfTokenScanner pdfScanner,
            XObjectFactory xObjectFactory,
            bool isLenientParsing)
        {
            GraphicsStateOperations = graphicsStateOperations;
            Letters = letters;
            this.xObjects = xObjects;
            this.pdfScanner = pdfScanner;
            this.xObjectFactory = xObjectFactory;
            this.isLenientParsing = isLenientParsing;
        }

        public IEnumerable<XObjectImage> GetImages()
        {
            foreach (var contentRecord in xObjects[XObjectType.Image])
            {
                yield return xObjectFactory.CreateImage(contentRecord, pdfScanner, isLenientParsing);
            }
        }
    }
}
