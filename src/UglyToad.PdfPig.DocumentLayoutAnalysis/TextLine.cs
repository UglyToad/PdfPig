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
        /// The text of the line.
        /// </summary>
        public string Text { get; }

        /// <summary>
        /// The text direction of the line.
        /// </summary>
        public TextDirection TextDirection { get; }

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
        /// <param name="words">The words contained in the line.</param>
        public TextLine(IReadOnlyList<Word> words)
        {
            if (words == null)
            {
                throw new ArgumentNullException(nameof(words));
            }

            if (words.Count == 0)
            {
                throw new ArgumentException("Empty words provided.", nameof(words));
            }

            Words = words;

            Text = string.Join(" ", words.Where(s => !string.IsNullOrWhiteSpace(s.Text)).Select(x => x.Text));

            var normalisedBoundingBoxes = words.Select(x => NormaliseRectangle(x.BoundingBox)).ToList();
            var minX = normalisedBoundingBoxes.Min(x => x.Left);
            var minY = normalisedBoundingBoxes.Min(x => x.Bottom);
            var maxX = normalisedBoundingBoxes.Max(x => x.Right);
            var maxY = normalisedBoundingBoxes.Max(x => x.Top);

            BoundingBox = new PdfRectangle(minX, minY, maxX, maxY);

            if (words.All(x => x.TextDirection == words[0].TextDirection))
            {
                TextDirection = words[0].TextDirection;
            }
            else
            {
                TextDirection = TextDirection.Unknown;
            }
        }

        private PdfRectangle NormaliseRectangle(PdfRectangle rectangle)
        {
            return new PdfRectangle(Math.Min(rectangle.Left, rectangle.Right),
                                    Math.Min(rectangle.Bottom, rectangle.Top),
                                    Math.Max(rectangle.Left, rectangle.Right),
                                    Math.Max(rectangle.Bottom, rectangle.Top));
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return Text;
        }
    }
}
