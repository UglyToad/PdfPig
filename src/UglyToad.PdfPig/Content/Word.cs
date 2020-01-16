namespace UglyToad.PdfPig.Content
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Core;

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

            var tempTextDirection = letters[0].TextDirection;
            if (letters.Any(l => l.TextDirection != tempTextDirection))
            {
                tempTextDirection = TextDirection.Unknown;
            }

            (string, PdfRectangle) data;

            switch (tempTextDirection)
            {
                case TextDirection.Horizontal:
                    data = GetBoundingBoxH(letters);
                    break;

                case TextDirection.Rotate180:
                    data = GetBoundingBox180(letters);
                    break;

                case TextDirection.Rotate90:
                    data = GetBoundingBox90(letters);
                    break;

                case TextDirection.Rotate270:
                    data = GetBoundingBox270(letters);
                    break;

                case TextDirection.Unknown:
                default:
                    data = GetBoundingBoxH(letters);
                    break;
            }

            Text = data.Item1;
            BoundingBox = data.Item2;

            FontName = letters[0].FontName;
            TextDirection = tempTextDirection;
        }

        #region Bounding box
        private (string, PdfRectangle) GetBoundingBoxH(IReadOnlyList<Letter> letters)
        {
            var builder = new StringBuilder();

            var minX = double.MaxValue;
            var maxX = double.MinValue;
            var minY = double.MaxValue;
            var maxY = double.MinValue;

            for (var i = 0; i < letters.Count; i++)
            {
                var letter = letters[i];
                builder.Append(letter.Value);

                if (letter.StartBaseLine.X < minX)
                {
                    minX = letter.StartBaseLine.X;
                }

                if (letter.StartBaseLine.Y < minY)
                {
                    minY = letter.StartBaseLine.Y;
                }

                var right = letter.StartBaseLine.X + letter.GlyphRectangle.Width;
                if (right > maxX)
                {
                    maxX = right;
                }

                if (letter.GlyphRectangle.Top > maxY)
                {
                    maxY = letter.GlyphRectangle.Top;
                }
            }

            return (builder.ToString(), new PdfRectangle(minX, minY, maxX, maxY));
        }

        private (string, PdfRectangle) GetBoundingBox180(IReadOnlyList<Letter> letters)
        {
            var builder = new StringBuilder();

            var maxX = double.MinValue;
            var minX = double.MaxValue;
            var maxY = double.MinValue;
            var minY = double.MaxValue;

            for (var i = 0; i < letters.Count; i++)
            {
                var letter = letters[i];
                builder.Append(letter.Value);

                if (letter.StartBaseLine.X > maxX)
                {
                    maxX = letter.StartBaseLine.X;
                }

                if (letter.StartBaseLine.Y > maxY)
                {
                    maxY = letter.StartBaseLine.Y;
                }

                var right = letter.StartBaseLine.X + letter.GlyphRectangle.Width;
                if (right < minX)
                {
                    minX = right;
                }

                if (letter.GlyphRectangle.Top < minY)
                {
                    minY = letter.GlyphRectangle.Top;
                }
            }

            return (builder.ToString(), new PdfRectangle(maxX, maxY, minX, minY));
        }

        private (string, PdfRectangle) GetBoundingBox90(IReadOnlyList<Letter> letters)
        {
            var builder = new StringBuilder();

            var minX = double.MaxValue;
            var maxX = double.MinValue;
            var minY = double.MaxValue;
            var maxY = double.MinValue;

            for (var i = 0; i < letters.Count; i++)
            {
                var letter = letters[i];
                builder.Append(letter.Value);

                if (letter.StartBaseLine.X < minX)
                {
                    minX = letter.StartBaseLine.X;
                }

                if (letter.EndBaseLine.Y < minY)
                {
                    minY = letter.EndBaseLine.Y;
                }

                var right = letter.StartBaseLine.X - letter.GlyphRectangle.Width;
                if (right > maxX)
                {
                    maxX = right;
                }

                if (letter.GlyphRectangle.Top > maxY)
                {
                    maxY = letter.GlyphRectangle.Top;
                }
            }

            return (builder.ToString(), new PdfRectangle(new PdfPoint(maxX, maxY),
                                                         new PdfPoint(maxX, minY),
                                                         new PdfPoint(minX, maxY),
                                                         new PdfPoint(minX, minY)));
        }

        private (string, PdfRectangle) GetBoundingBox270(IReadOnlyList<Letter> letters)
        {
            var builder = new StringBuilder();

            var maxX = double.MinValue;
            var minX = double.MaxValue;
            var minY = double.MaxValue;
            var maxY = double.MinValue;

            for (var i = 0; i < letters.Count; i++)
            {
                var letter = letters[i];
                builder.Append(letter.Value);

                if (letter.StartBaseLine.X > maxX)
                {
                    maxX = letter.StartBaseLine.X;
                }

                if (letter.StartBaseLine.Y < minY)
                {
                    minY = letter.StartBaseLine.Y;
                }

                var right = letter.StartBaseLine.X - letter.GlyphRectangle.Width;
                if (right < minX)
                {
                    minX = right;
                }

                if (letter.GlyphRectangle.Bottom > maxY)
                {
                    maxY = letter.GlyphRectangle.Bottom;
                }
            }

            return (builder.ToString(), new PdfRectangle(new PdfPoint(minX, minY),
                                                         new PdfPoint(minX, maxY),
                                                         new PdfPoint(maxX, minY),
                                                         new PdfPoint(maxX, maxY)));
        }
        #endregion

        /// <inheritdoc />
        public override string ToString()
        {
            return Text;
        }
    }
}
