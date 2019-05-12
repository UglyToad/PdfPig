using System;
using System.Collections.Generic;
using System.Text;
using UglyToad.PdfPig.Geometry;
using System.Linq;

namespace UglyToad.PdfPig.Content
{
    /// <summary>
    /// A line.
    /// </summary>
    public class Line
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
        /// Create a new <see cref="Word"/>.
        /// </summary>
        /// <param name="words">The letters contained in the word.</param>
        public Line(IReadOnlyList<Word> words)
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

            TextDirection = words[0].TextDirection;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return Text;
        }
    }
}
