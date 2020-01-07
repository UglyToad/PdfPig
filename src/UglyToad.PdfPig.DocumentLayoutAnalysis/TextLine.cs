namespace UglyToad.PdfPig.DocumentLayoutAnalysis
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Content;
    using Core;

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

            var minX = words.Min(x => x.BoundingBox.Left);
            var minY = words.Min(x => x.BoundingBox.Bottom);
            var maxX = words.Max(x => x.BoundingBox.Right);
            var maxY = words.Max(x => x.BoundingBox.Top);
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

        /// <inheritdoc />
        public override string ToString()
        {
            return Text;
        }
    }
}
