namespace UglyToad.Pdf.Content
{
    using System;
    using System.Collections.Generic;

    public class Page
    {
        /// <summary>
        /// The 1 indexed page number.
        /// </summary>
        public int Number { get; }

        internal MediaBox MediaBox { get; }

        internal CropBox CropBox { get; }

        internal PageContent Content { get; }

        public IReadOnlyList<Letter> Letters => Content?.Letters ?? new Letter[0];

        /// <summary>
        /// Gets the width of the page in points.
        /// </summary>
        public decimal Width { get; }

        /// <summary>
        /// Gets the height of the page in points.
        /// </summary>
        public decimal Height { get; }

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

            Width = mediaBox.Bounds.Width;
            Height = mediaBox.Bounds.Height;
        }
    }
}