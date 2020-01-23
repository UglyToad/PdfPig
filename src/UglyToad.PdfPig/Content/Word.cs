namespace UglyToad.PdfPig.Content
{
    using Core;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using UglyToad.PdfPig.Geometry;

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
            if (tempTextDirection != TextDirection.Other)
            {
                foreach (var letter in letters)
                {
                    if (letter.TextDirection != tempTextDirection)
                    {
                        tempTextDirection = TextDirection.Other;
                        break;
                    }
                }
            }

            Tuple<string, PdfRectangle> data;

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

                case TextDirection.Other:
                default:
                    data = GetBoundingBoxOther(letters);
                    break;
            }

            Text = data.Item1;
            BoundingBox = data.Item2;

            FontName = letters[0].FontName;
            TextDirection = tempTextDirection;
        }

        #region Bounding box
        private Tuple<string, PdfRectangle> GetBoundingBoxH(IReadOnlyList<Letter> letters)
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

                var right = letter.StartBaseLine.X + Math.Max(letter.Width, letter.GlyphRectangle.Width);
                if (right > maxX)
                {
                    maxX = right;
                }

                if (letter.GlyphRectangle.Top > maxY)
                {
                    maxY = letter.GlyphRectangle.Top;
                }
            }

            return new Tuple<string, PdfRectangle>(builder.ToString(), new PdfRectangle(minX, minY, maxX, maxY));
        }

        private Tuple<string, PdfRectangle> GetBoundingBox180(IReadOnlyList<Letter> letters)
        {
            var builder = new StringBuilder();

            var minX = double.MaxValue;
            var maxX = double.MinValue;
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

                var right = letter.StartBaseLine.X + Math.Min(letter.Width, letter.GlyphRectangle.Width);
                if (right < minX)
                {
                    minX = right;
                }

                if (letter.GlyphRectangle.Top < minY)
                {
                    minY = letter.GlyphRectangle.Top;
                }
            }

            return new Tuple<string, PdfRectangle>(builder.ToString(), new PdfRectangle(maxX, maxY, minX, minY));
        }

        private Tuple<string, PdfRectangle> GetBoundingBox90(IReadOnlyList<Letter> letters)
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

                var right = letter.StartBaseLine.X - letter.GlyphRectangle.Height;
                if (right > maxX)
                {
                    maxX = right;
                }

                if (letter.GlyphRectangle.Top > maxY)
                {
                    maxY = letter.GlyphRectangle.Top;
                }
            }

            return new Tuple<string, PdfRectangle>(builder.ToString(), new PdfRectangle(new PdfPoint(maxX, maxY),
                                                         new PdfPoint(maxX, minY),
                                                         new PdfPoint(minX, maxY),
                                                         new PdfPoint(minX, minY)));
        }

        private Tuple<string, PdfRectangle> GetBoundingBox270(IReadOnlyList<Letter> letters)
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

                if (letter.StartBaseLine.X > maxX)
                {
                    maxX = letter.StartBaseLine.X;
                }

                if (letter.StartBaseLine.Y < minY)
                {
                    minY = letter.StartBaseLine.Y;
                }

                var right = letter.StartBaseLine.X - letter.GlyphRectangle.Height;
                if (right < minX)
                {
                    minX = right;
                }

                if (letter.GlyphRectangle.Bottom > maxY)
                {
                    maxY = letter.GlyphRectangle.Bottom;
                }
            }

            return new Tuple<string, PdfRectangle>(builder.ToString(), new PdfRectangle(new PdfPoint(minX, minY),
                                                         new PdfPoint(minX, maxY),
                                                         new PdfPoint(maxX, minY),
                                                         new PdfPoint(maxX, maxY)));
        }

        private Tuple<string, PdfRectangle> GetBoundingBoxOther(IReadOnlyList<Letter> letters)
        {
            var builder = new StringBuilder();
            for (var i = 0; i < letters.Count; i++)
            {
                builder.Append(letters[i].Value);
            }

            var points = letters.SelectMany(r => new[]
            {
                r.StartBaseLine,
                r.EndBaseLine,
                r.GlyphRectangle.TopLeft,
                r.GlyphRectangle.TopRight
            }).Distinct();
            var convexHull = GeometryExtensions.GrahamScan(points).ToList();
            var mbr = GeometryExtensions.ParametricPerpendicularProjection(convexHull);
            var mbrPoints = new[] { mbr.TopLeft, mbr.TopRight, mbr.BottomLeft, mbr.BottomRight };

            // Find the orientation of the minimum bounding box, using the baseline angle.
            // This method needs improvment as the baseline angle and the bbox angle can 
            // belong to different quadrants of the unit circle.
            var firstLetter = letters[0];
            var lastLetter = letters[letters.Count - 1];
            var rotation = Math.Atan2(
                lastLetter.EndBaseLine.Y - firstLetter.StartBaseLine.Y,
                lastLetter.EndBaseLine.X - firstLetter.StartBaseLine.X);

            if (rotation >= -Math.PI && rotation <= -1.570796) // (-180 to -90deg)
            {
                var br = mbrPoints.OrderBy(p => p.X).ThenByDescending(p => p.Y).First();
                var bl = mbrPoints.OrderByDescending(p => p.Y).ThenByDescending(p => p.X).First();
                var tl = mbrPoints.OrderByDescending(p => p.Y).ThenBy(p => p.X).First();
                var tr = mbrPoints.OrderBy(p => p.X).ThenBy(p => p.Y).First();
                return new Tuple<string, PdfRectangle>(builder.ToString(), new PdfRectangle(tl, tr, bl, br));
            }
            else if (rotation > -1.570796 && rotation <= 0.0) // (-90deg to 0)
            {
                var bl = mbrPoints.OrderBy(p => p.X).ThenBy(p => p.Y).First();
                var tl = mbrPoints.OrderByDescending(p => p.Y).ThenBy(p => p.X).First();
                var tr = mbrPoints.OrderByDescending(p => p.X).ThenByDescending(p => p.Y).First();
                var br = mbrPoints.OrderBy(p => p.Y).ThenByDescending(p => p.X).First();
                return new Tuple<string, PdfRectangle>(builder.ToString(), new PdfRectangle(tl, tr, bl, br));
            }
            else if (rotation > 0.0 && rotation <= 1.570796) // (0 to 90deg)
            {
                var tl = mbrPoints.OrderBy(p => p.X).ThenByDescending(p => p.Y).First();
                var bl = mbrPoints.OrderBy(p => p.Y).ThenBy(p => p.X).First();
                var br = mbrPoints.OrderByDescending(p => p.X).ThenBy(p => p.Y).First();
                var tr = mbrPoints.OrderByDescending(p => p.Y).ThenByDescending(p => p.X).First();
                return new Tuple<string, PdfRectangle>(builder.ToString(), new PdfRectangle(tl, tr, bl, br));
            }
            else if (rotation > 1.570796 && rotation <= Math.PI) // (90 to 180deg)
            {
                var tr = mbrPoints.OrderBy(p => p.X).ThenBy(p => p.Y).First();
                var bl = mbrPoints.OrderByDescending(p => p.X).ThenByDescending(p => p.Y).First();
                var br = mbrPoints.OrderByDescending(p => p.Y).ThenBy(p => p.X).First();
                var tl = mbrPoints.OrderBy(p => p.X).ThenByDescending(p => p.Y).First();
                return new Tuple<string, PdfRectangle>(builder.ToString(), new PdfRectangle(tl, tr, bl, br));
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(rotation), "Word orientation not handled.");
            }
        }
        #endregion

        /// <inheritdoc />
        public override string ToString()
        {
            return Text;
        }
    }
}
