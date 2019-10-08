namespace UglyToad.PdfPig.Content
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Annotations;
    using Graphics.Operations;
    using Tokens;
    using Util;
    using Util.JetBrains.Annotations;
    using Geometry;

    /// <summary>
    /// Contains the content and provides access to methods of a single page in the <see cref="PdfDocument"/>.
    /// </summary>
    public class Page
    {
        private readonly Lazy<string> textLazy;

        /// <summary>
        /// The raw PDF dictionary token for this page in the document.
        /// </summary>
        public DictionaryToken Dictionary { get; }

        /// <summary>
        /// The page number (starting at 1).
        /// </summary>
        public int Number { get; }

        internal MediaBox MediaBox { get; }

        internal CropBox CropBox { get; }

        internal PageContent Content { get; }

        /// <summary>
        /// The rotation of the page in degrees (clockwise). Valid values are 0, 90, 180 and 270.
        /// </summary>
        public PageRotationDegrees Rotation { get; }

        /// <summary>
        /// The set of <see cref="Letter"/>s drawn by the PDF content.
        /// </summary>
        public IReadOnlyList<Letter> Letters => Content?.Letters ?? new Letter[0];

        /// <summary>
        /// The full text of all characters on the page in the order they are presented in the PDF content.
        /// </summary>
        public string Text => textLazy.Value;

        /// <summary>
        /// Gets the width of the page in points.
        /// </summary>
        public decimal Width { get; }

        /// <summary>
        /// Gets the height of the page in points.
        /// </summary>
        public decimal Height { get; }

        /// <summary>
        /// The size of the page according to the standard page sizes or <see cref="PageSize.Custom"/> if no matching standard size found.
        /// </summary>
        public PageSize Size { get; }

        /// <summary>
        /// The parsed graphics state operations in the content stream for this page. 
        /// </summary>
        public IReadOnlyList<IGraphicsStateOperation> Operations => Content.GraphicsStateOperations;
        
        /// <summary>
        /// Access to members whose future locations within the API will change without warning.
        /// </summary>
        [NotNull]
        public Experimental ExperimentalAccess { get; }

        internal Page(int number, DictionaryToken dictionary, MediaBox mediaBox, CropBox cropBox, PageRotationDegrees rotation, PageContent content,
            AnnotationProvider annotationProvider)
        {
            if (number <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(number), "Page number cannot be 0 or negative.");
            }

            Dictionary = dictionary ?? throw new ArgumentNullException(nameof(dictionary));

            Number = number;
            MediaBox = mediaBox;
            CropBox = cropBox;
            Rotation = rotation;
            Content = content;
            textLazy = new Lazy<string>(() => GetText(Content));

            Width = mediaBox.Bounds.Width;
            Height = mediaBox.Bounds.Height;

            Size = mediaBox.Bounds.GetPageSize();
            ExperimentalAccess = new Experimental(this, annotationProvider);
        }

        private static string GetText(PageContent content)
        {
            if (content?.Letters == null)
            {
                return string.Empty;
            }

            var builder = new StringBuilder();
            for (var i = 0; i < content.Letters.Count; i++)
            {
                builder.Append(content.Letters[i].Value);
            }

            return builder.ToString();
        }

        /// <summary>
        /// Use the default <see cref="IWordExtractor"/> to get the words for this page.
        /// </summary>
        /// <returns>The words on this page.</returns>
        public IEnumerable<Word> GetWords() => GetWords(DefaultWordExtractor.Instance);

        /// <summary>
        /// Use a custom <see cref="IWordExtractor"/> to get the words for this page.
        /// </summary>
        /// <param name="wordExtractor">The word extractor to use to generate words.</param>
        /// <returns>The words on this page.</returns>
        public IEnumerable<Word> GetWords(IWordExtractor wordExtractor)
        {
            return (wordExtractor ?? DefaultWordExtractor.Instance).GetWords(Letters);
        }

        /// <summary>
        /// Gets any images on the page.
        /// </summary>
        public IEnumerable<IPdfImage> GetImages() => Content.GetImages();

        /// <summary>
        /// Provides access to useful members which will change in future releases.
        /// </summary>
        public class Experimental
        {
            private readonly Page page;
            private readonly AnnotationProvider annotationProvider;

            /// <summary>
            /// The set of <see cref="PdfPath"/>s drawn by the PDF content.
            /// </summary>
            public IReadOnlyList<PdfPath> Paths => page.Content?.Paths ?? new List<PdfPath>();

            internal Experimental(Page page, AnnotationProvider annotationProvider)
            {
                this.page = page;
                this.annotationProvider = annotationProvider;
            }

            /// <summary>
            /// Get the annotation objects from the page.
            /// </summary>
            /// <returns>The lazily evaluated set of annotations on this page.</returns>
            public IEnumerable<Annotation> GetAnnotations()
            {
                return annotationProvider.GetAnnotations();
            }

            /// <summary>
            /// Gets the calculated letter size in points.
            /// This is considered experimental because the calculated value is incorrect for some documents at present.
            /// </summary>
            public decimal GetPointSize(Letter letter)
            {
                return letter.PointSize;
            }

            /// <summary>
            /// Get the hOCR (html) string of the page layout.
            /// <para>This is considered experimental because it needs more testing.</para>
            /// </summary>
            /// <param name="wordExtractor">The word extractor to use to generate words.</param>
            /// <param name="pageSegmenter">The page segmenter to use.</param>
            /// <param name="indent">Indent character to use.</param>
            /// <param name="drawPaths">Draw <see cref="PdfPath"/>s present in the page.</param>
            /// <param name="useHocrjs">Will add a reference to the 'hocrjs' script just before the closing 'body' tag, adding the interface to a plain hOCR file.<para>See https://github.com/kba/hocrjs for more information.</para></param>
            public string GetHOCR(IWordExtractor wordExtractor, DocumentLayoutAnalysis.IPageSegmenter pageSegmenter, string indent = "\t", bool drawPaths = false, bool useHocrjs = false)
            {
                var hocr = new Export.HOcrTextExporter(wordExtractor, pageSegmenter, 2, indent);
                return hocr.Get(page, drawPaths, useHocrjs: useHocrjs);
            }

            /// <summary>
            /// Get the Alto (xml) string of the page layout.
            /// <para>This is considered experimental because it needs more testing.</para>
            /// </summary>
            /// <param name="wordExtractor">The word extractor to use to generate words.</param>
            /// <param name="pageSegmenter">The page segmenter to use.</param>
            /// <param name="indent">Indent character to use.</param>
            /// <param name="drawPaths">Draw <see cref="PdfPath"/>s present in the page.</param>
            public string GetAltoXml(IWordExtractor wordExtractor, DocumentLayoutAnalysis.IPageSegmenter pageSegmenter, string indent = "\t", bool drawPaths = false)
            {
                var alto = new Export.AltoXmlTextExporter(wordExtractor, pageSegmenter, 2, indent);
                return alto.Get(page, drawPaths);
            }

            /// <summary>
            /// Get the PageXml (xml) string of the page layout.
            /// <para>This is considered experimental because it needs more testing.</para>
            /// </summary>
            /// <param name="wordExtractor">The word extractor to use to generate words.</param>
            /// <param name="pageSegmenter">The page segmenter to use.</param>
            /// <param name="indent">Indent character to use.</param>
            /// <param name="drawPaths">Draw <see cref="PdfPath"/>s present in the page.</param>
            public string GetPageXml(IWordExtractor wordExtractor, DocumentLayoutAnalysis.IPageSegmenter pageSegmenter, string indent = "\t", bool drawPaths = false)
            {
                var pageXml = new Export.PageXmlTextExporter(wordExtractor, pageSegmenter, 2, indent);
                return pageXml.Get(page, drawPaths);
            }
        }
    }
}
