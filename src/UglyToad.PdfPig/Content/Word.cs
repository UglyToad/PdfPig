namespace UglyToad.PdfPig.Content
{
    using System;
    using System.Collections.Generic;
    using System.Text;
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
        /// The text direction of the word.
        /// </summary>
        public TextDirection TextDirection { get; }

        /// <summary>
        /// The rectangle completely containing the word.
        /// </summary>
        public PdfRectangle BoundingBox { get; }

        /// <summary>
        /// The name of the font for the word.
        /// </summary>
        public string FontName { get; }

        /// <summary>
        /// The letters contained in the word.
        /// </summary>
        public IReadOnlyList<Letter> Letters { get; }

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

            Letters = letters;

            var builder = new StringBuilder();

            var minX = double.MaxValue;
            var minY = double.MaxValue;
            var maxX = double.MinValue;
            var maxY = double.MinValue;

            for (var i = 0; i < letters.Count; i++)
            {
                var letter = letters[i];
                builder.Append(letter.Value);

                if (letter.Location.X < minX)
                {
                    minX = letter.Location.X;
                }

                if (letter.Location.Y < minY)
                {
                    minY = letter.Location.Y;
                }

                var right = letter.Location.X + letter.Width;
                if (right > maxX)
                {
                    maxX = right;
                }

                if (letter.GlyphRectangle.Top > maxY)
                {
                    maxY = letter.GlyphRectangle.Top;
                }
            }

            Text = builder.ToString();
            BoundingBox = new PdfRectangle(minX, minY, maxX, maxY);

            FontName = letters[0].FontName;
            TextDirection = letters[0].TextDirection;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return Text;
        }
    }
}
