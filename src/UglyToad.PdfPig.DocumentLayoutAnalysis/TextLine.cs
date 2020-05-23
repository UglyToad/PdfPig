namespace UglyToad.PdfPig.DocumentLayoutAnalysis
{
    using Content;
    using Core;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// A line of text.
    /// </summary>
    public class TextLine
    {
        /// <summary>
        /// The separator used between words in the line.
        /// </summary>
        public readonly string Separator;

        /// <summary>
        /// The text of the line.
        /// </summary>
        public string Text { get; }

        /// <summary>
        /// The text orientation of the line.
        /// </summary>
        public TextOrientation TextOrientation { get; }

        /// <summary>
        /// The rectangle completely containing the line.
        /// </summary>
        public PdfRectangle BoundingBox { get; }

        /// <summary>
        /// The words contained in the line.
        /// </summary>
        public IReadOnlyList<Word> Words { get; }

        /// <summary>
        /// Create a new <see cref="TextLine"/>.
        /// </summary>
        /// <param name="words">The words contained in the line, in the correct order.</param>
        /// <param name="separator">The separator used between words in the line.</param>
        public TextLine(IReadOnlyList<Word> words, string separator = " ")
        {
            if (words == null)
            {
                throw new ArgumentNullException(nameof(words));
            }

            if (words.Count == 0)
            {
                throw new ArgumentException("Empty words provided.", nameof(words));
            }

            Separator = separator;

            Words = words;

            Text = string.Join(" ", words.Where(s => !string.IsNullOrWhiteSpace(s.Text)).Select(x => x.Text));

            var normalisedBoundingBoxes = words.Select(x => NormaliseRectangle(x.BoundingBox)).ToList();
            var minX = normalisedBoundingBoxes.Min(x => x.Left);
            var minY = normalisedBoundingBoxes.Min(x => x.Bottom);
            var maxX = normalisedBoundingBoxes.Max(x => x.Right);
            var maxY = normalisedBoundingBoxes.Max(x => x.Top);

            BoundingBox = new PdfRectangle(minX, minY, maxX, maxY);

            if (words.All(x => x.TextOrientation == words[0].TextOrientation))
            {
                TextOrientation = words[0].TextOrientation;
            }
            else
            {
                TextOrientation = TextOrientation.Other;
            }
        }

        private PdfRectangle NormaliseRectangle(PdfRectangle rectangle)
        {
            return new PdfRectangle(Math.Min(Math.Min(Math.Min(rectangle.TopLeft.X, rectangle.TopRight.X), rectangle.BottomLeft.X), rectangle.BottomRight.X),
                                    Math.Min(Math.Min(Math.Min(rectangle.TopLeft.Y, rectangle.TopRight.Y), rectangle.BottomLeft.Y), rectangle.BottomRight.Y),
                                    Math.Max(Math.Max(Math.Max(rectangle.TopLeft.X, rectangle.TopRight.X), rectangle.BottomLeft.X), rectangle.BottomRight.X),
                                    Math.Max(Math.Max(Math.Max(rectangle.TopLeft.Y, rectangle.TopRight.Y), rectangle.BottomLeft.Y), rectangle.BottomRight.Y));
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return Text;
        }
    }
}
