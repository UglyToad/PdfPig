namespace UglyToad.PdfPig.Content
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Geometry;

    /// <summary>
    /// A word.
    /// </summary>
    public class Word
    {
        /// <summary>
        /// The text of the word.
        /// </summary>
        public string Text { get; }

        /// <summary>
        /// The rectangle completely containing the word.
        /// </summary>
        public PdfRectangle BoundingBox { get; }

        /// <summary>
        /// The name of the font for the word.
        /// </summary>
        public string FontName { get; }

        /// <summary>
        /// Create a new <see cref="Word"/>.
        /// </summary>
        /// <param name="letters">The letters contained in the word.</param>
        public Word(IReadOnlyList<Letter> letters)
        {
            if (letters == null)
            {
                throw new ArgumentNullException(nameof(letters));
            }

            if (letters.Count == 0)
            {
                throw new ArgumentException("Empty letters provided.", nameof(letters));
            }

            Text = string.Join(string.Empty, letters.Select(x => x.Value));

            var minX = letters.Min(x => x.Location.X);
            var minY = letters.Min(x => x.Location.Y);
            var maxX = letters.Max(x => x.Location.X + x.Width);
            var maxY = letters.Max(x => x.GlyphRectangle.Top);

            BoundingBox = new PdfRectangle(minX, minY, maxX, maxY);
            FontName = letters[0].FontName;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return Text;
        }
    }
}
