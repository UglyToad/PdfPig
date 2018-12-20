namespace UglyToad.PdfPig.Content
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Annotations;
    using Tokens;
    using Util;
    using Util.JetBrains.Annotations;
    using XObjects;

    /// <summary>
    /// Contains the content and provides access to methods of a single page in the <see cref="PdfDocument"/>.
    /// </summary>
    public class Page
    {
        private readonly DictionaryToken dictionary;
        private readonly AnnotationProvider annotationProvider;

        /// <summary>
        /// The page number (starting at 1).
        /// </summary>
        public int Number { get; }

        internal MediaBox MediaBox { get; }

        internal CropBox CropBox { get; }

        internal PageContent Content { get; }

        /// <summary>
        /// The set of <see cref="Letter"/>s drawn by the PDF content.
        /// </summary>
        public IReadOnlyList<Letter> Letters => Content?.Letters ?? new Letter[0];

        /// <summary>
        /// The full text of all characters on the page in the order they are presented in the PDF content.
        /// </summary>
        public string Text { get; }

        /// <summary>
        /// Gets the width of the page in points.
        /// </summary>
        public decimal Width { get; }

        /// <summary>
        /// Gets the height of the page in points.
        /// </summary>
        public decimal Height { get; }

        /// <summary>
        /// The size of the page according to the standard page sizes or Custom if no matching standard size found.
        /// </summary>
        public PageSize Size { get; }

        /// <summary>
        /// Access to members whose future locations within the API will change without warning.
        /// </summary>
        [NotNull]
        public Experimental ExperimentalAccess { get; }

        internal Page(int number, DictionaryToken dictionary, MediaBox mediaBox, CropBox cropBox, PageContent content,
            AnnotationProvider annotationProvider)
        {
            if (number <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(number), "Page number cannot be 0 or negative.");
            }

            this.dictionary = dictionary ?? throw new ArgumentNullException(nameof(dictionary));
            this.annotationProvider = annotationProvider ?? throw new ArgumentNullException(nameof(annotationProvider));

            Number = number;
            MediaBox = mediaBox;
            CropBox = cropBox;
            Content = content;
            Text = GetText(content);

            Width = mediaBox.Bounds.Width;
            Height = mediaBox.Bounds.Height;

            Size = mediaBox.Bounds.GetPageSize();
            ExperimentalAccess = new Experimental(this);
        }

        private static string GetText(PageContent content)
        {
            if (content?.Letters == null)
            {
                return string.Empty;
            }

            return string.Join(string.Empty, content.Letters.Select(x => x.Value));
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

        internal IEnumerable<Annotation> GetAnnotations()
        {
            return annotationProvider.GetAnnotations();
        }

        /// <summary>
        /// Provides access to useful members which will change in future releases.
        /// </summary>
        public class Experimental
        {
            private readonly Page page;

            internal Experimental(Page page)
            {
                this.page = page;
            }

            /// <summary>
            /// Retrieve any images referenced in this page's content.
            /// These are returned as <see cref="XObjectImage"/>s which are 
            /// raw data from the PDF's content rather than images.
            /// </summary>
            public IEnumerable<XObjectImage> GetRawImages()
            {
                return page.Content.GetImages();
            }
        }
    }
}