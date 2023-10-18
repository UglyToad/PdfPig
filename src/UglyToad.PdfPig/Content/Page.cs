namespace UglyToad.PdfPig.Content
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Annotations;
    using Geometry;
    using Graphics.Operations;
    using Tokens;
    using Util;
    using Util.JetBrains.Annotations;
    using Tokenization.Scanner;
    using Graphics;
    using System.Linq;

    /// <summary>
    /// Contains the content and provides access to methods of a single page in the <see cref="PdfDocument"/>.
    /// </summary>
    public class Page
    {
        internal readonly AnnotationProvider annotationProvider;
        internal readonly IPdfTokenScanner pdfScanner;
        private readonly Lazy<string> textLazy;

        /// <summary>
        /// The raw PDF dictionary token for this page in the document.
        /// </summary>
        public DictionaryToken Dictionary { get; }

        /// <summary>
        /// The page number (starting at 1).
        /// </summary>
        public int Number { get; }

        /// <summary>
        /// Defines the visible region of the page, content outside the <see cref="CropBox"/> is clipped/cropped.
        /// </summary>
        public CropBox CropBox { get; }

        /// <summary>
        /// Defines the boundaries of the physical medium on which the page shall be displayed or printed.
        /// </summary>
        public MediaBox MediaBox { get; }

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
        public double Width { get; }

        /// <summary>
        /// Gets the height of the page in points.
        /// </summary>
        public double Height { get; }

        /// <summary>
        /// The size of the page according to the standard page sizes or <see cref="PageSize.Custom"/> if no matching standard size found.
        /// </summary>
        public PageSize Size { get; }

        /// <summary>
        /// The number of images on this page. Use <see cref="GetImages"/> to access the image contents.
        /// </summary>
        public int NumberOfImages => Content.NumberOfImages;

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
            AnnotationProvider annotationProvider,
            IPdfTokenScanner pdfScanner)
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

            // Special case where cropbox is outside mediabox: use cropbox instead of intersection
            var viewBox = mediaBox.Bounds.Intersect(cropBox.Bounds) ?? cropBox.Bounds;

            Width = viewBox.Width;
            Height = viewBox.Height;
            Size = viewBox.GetPageSize();

            ExperimentalAccess = new Experimental(this, annotationProvider);
            this.annotationProvider = annotationProvider;
            this.pdfScanner = pdfScanner ?? throw new ArgumentNullException(nameof(pdfScanner));
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
        /// Get the hyperlinks which link to external resources on the page.
        /// These are based on the annotations on the page with a type of '/Link'.
        /// </summary>
        public IReadOnlyList<Hyperlink> GetHyperlinks()
        {
            return HyperlinkFactory.GetHyperlinks(this, pdfScanner, annotationProvider);
        }

        /// <summary>
        /// Gets any images on the page.
        /// </summary>
        public IEnumerable<IPdfImage> GetImages() => Content.GetImages();

        /// <summary>
        /// Gets any marked content on the page.
        /// </summary>
        public IReadOnlyList<MarkedContentElement> GetMarkedContents() => Content.GetMarkedContents();

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
            /// Gets any optional content on the page.
            /// <para>Does not handle XObjects and annotations for the time being.</para>
            /// </summary>
            public IReadOnlyDictionary<string, IReadOnlyList<OptionalContentGroupElement>> GetOptionalContents()
            {
                List<OptionalContentGroupElement> mcesOptional = new List<OptionalContentGroupElement>();

                // 4.10.2
                // Optional content in content stream
                GetOptionalContentsRecursively(page.Content?.GetMarkedContents(), ref mcesOptional);

                // Optional content in XObjects and annotations
                // TO DO
                //var annots = GetAnnotations().ToList();

                return mcesOptional.GroupBy(oc => oc.Name).ToDictionary(g => g.Key, g => g.ToList() as IReadOnlyList<OptionalContentGroupElement>);
            }

            private void GetOptionalContentsRecursively(IReadOnlyList<MarkedContentElement> markedContentElements, ref List<OptionalContentGroupElement> mcesOptional)
            {
                if (markedContentElements.Count == 0)
                {
                    return;
                }

                foreach (var mce in markedContentElements)
                {
                    if (mce.Tag == "OC")
                    {
                        mcesOptional.Add(new OptionalContentGroupElement(mce, page.pdfScanner));
                        // we don't recurse
                    }
                    else if (mce.Children?.Count > 0)
                    {
                        GetOptionalContentsRecursively(mce.Children, ref mcesOptional);
                    }
                }
            }
        }
    }
}
