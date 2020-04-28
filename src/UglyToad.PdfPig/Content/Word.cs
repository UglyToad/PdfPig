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

            var baseLinePoints = letters.SelectMany(r => new[]
            {
                r.StartBaseLine,
                r.EndBaseLine,
            }).ToList();

            // Fitting a line through the base lines points
            // to find the orientation (slope)
            double x0 = baseLinePoints.Average(p => p.X);
            double y0 = baseLinePoints.Average(p => p.Y);
            double sumProduct = 0;
            double sumDiffSquaredX = 0;

            for (int i = 0; i < baseLinePoints.Count; i++)
            {
                var point = baseLinePoints[i];
                var x_diff = point.X - x0;
                var y_diff = point.Y - y0;
                sumProduct += x_diff * y_diff;
                sumDiffSquaredX += x_diff * x_diff;
            }

            var slope = sumProduct / sumDiffSquaredX;

            // Rotate the points to build the axis-aligned bounding box (AABB)
            var angleRad = Math.Atan(slope);
            var cos = Math.Cos(angleRad);
            var sin = Math.Sin(angleRad);

            var inverseRotation = new TransformationMatrix(
                cos, -sin, 0,
                sin, cos, 0,
                0, 0, 1);

            var transformedPoints = letters.SelectMany(r => new[]
            {
                r.StartBaseLine,
                r.EndBaseLine,
                r.GlyphRectangle.TopLeft,
                r.GlyphRectangle.TopRight
            }).Distinct().Select(p => inverseRotation.Transform(p));
            var aabb = new PdfRectangle(transformedPoints.Min(p => p.X),
                                        transformedPoints.Min(p => p.Y),
                                        transformedPoints.Max(p => p.X),
                                        transformedPoints.Max(p => p.Y));

            // Rotate back the AABB to obtain to oriented bounding box (OBB)
            var rotateBack = new TransformationMatrix(
                cos, sin, 0,
                -sin, cos, 0,
                0, 0, 1);

            // Candidates bounding boxes
            var obb = rotateBack.Transform(aabb);
            var obb1 = new PdfRectangle(obb.BottomLeft, obb.TopLeft, obb.BottomRight, obb.TopRight);
            var obb2 = new PdfRectangle(obb.TopRight, obb.BottomRight, obb.TopLeft, obb.BottomLeft);
            var obb3 = new PdfRectangle(obb.BottomRight, obb.BottomLeft, obb.TopRight, obb.TopLeft);

            // Find the orientation of the OBB, using the baseline angle
            var firstLetter = letters[0];
            var lastLetter = letters[letters.Count - 1];

            var baseLineAngle = Math.Atan2(
                lastLetter.EndBaseLine.Y - firstLetter.StartBaseLine.Y,
                lastLetter.EndBaseLine.X - firstLetter.StartBaseLine.X) * 180 / Math.PI;

            double deltaAngle = Math.Abs(baseLineAngle - obb.Rotation);
            double deltaAngle1 = Math.Abs(baseLineAngle - obb1.Rotation);
            if (deltaAngle1 < deltaAngle)
            {
                deltaAngle = deltaAngle1;
                obb = obb1;
            }

            double deltaAngle2 = Math.Abs(baseLineAngle - obb2.Rotation);
            if (deltaAngle2 < deltaAngle)
            {
                deltaAngle = deltaAngle2;
                obb = obb2;
            }

            double deltaAngle3 = Math.Abs(baseLineAngle - obb3.Rotation);
            if (deltaAngle3 < deltaAngle)
            {
                obb = obb3;
            }

            return new Tuple<string, PdfRectangle>(builder.ToString(), obb);
        }
        #endregion

        /// <inheritdoc />
        public override string ToString()
        {
            return Text;
        }
    }
}
