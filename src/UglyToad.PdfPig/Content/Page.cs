namespace UglyToad.PdfPig.Content
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Util;

    /// <summary>
    /// Contains the content and provides access to methods of a single page in the <see cref="PdfDocument"/>.
    /// </summary>
    public class Page
    {
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

        internal Page(int number, MediaBox mediaBox, CropBox cropBox, PageContent content)
        {
            if (number <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(number), "Page number cannot be 0 or negative.");
            }

            Number = number;
            MediaBox = mediaBox;
            CropBox = cropBox;
            Content = content;
            Text = GetText(content);

            Width = mediaBox.Bounds.Width;
            Height = mediaBox.Bounds.Height;

            Size = mediaBox.Bounds.GetPageSize();
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
    }
}