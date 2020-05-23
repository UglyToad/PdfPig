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

            if (Words.Count == 1)
            {
                BoundingBox = Words[0].BoundingBox;
                Text = Words[0].Text;
                TextOrientation = words[0].TextOrientation;
            }
            else
            {
                var tempTextOrientation = words[0].TextOrientation;
                if (tempTextOrientation != TextOrientation.Other)
                {
                    foreach (var letter in words)
                    {
                        if (letter.TextOrientation != tempTextOrientation)
                        {
                            tempTextOrientation = TextOrientation.Other;
                            break;
                        }
                    }
                }

                switch (tempTextOrientation)
                {
                    case TextOrientation.Horizontal:
                        BoundingBox = GetBoundingBoxH(words);
                        break;

                    case TextOrientation.Rotate180:
                        BoundingBox = GetBoundingBox180(words);
                        break;

                    case TextOrientation.Rotate90:
                        BoundingBox = GetBoundingBox90(words);
                        break;

                    case TextOrientation.Rotate270:
                        BoundingBox = GetBoundingBox270(words);
                        break;

                    case TextOrientation.Other:
                    default:
                        BoundingBox = GetBoundingBoxOther(words);
                        break;
                }

                Text = string.Join(Separator, words.Where(s => !string.IsNullOrWhiteSpace(s.Text)).Select(x => x.Text));
                TextOrientation = tempTextOrientation;
            }
        }

        #region Bounding box
        private PdfRectangle GetBoundingBoxH(IReadOnlyList<Word> words)
        {
            var blX = double.MaxValue;
            var trX = double.MinValue;
            var blY = double.MaxValue;
            var trY = double.MinValue;

            for (var i = 0; i < words.Count; i++)
            {
                var word = words[i];
                if (word.BoundingBox.BottomLeft.X < blX)
                {
                    blX = word.BoundingBox.BottomLeft.X;
                }

                if (word.BoundingBox.BottomLeft.Y < blY)
                {
                    blY = word.BoundingBox.BottomLeft.Y;
                }

                var right = word.BoundingBox.BottomLeft.X + word.BoundingBox.Width;
                if (right > trX)
                {
                    trX = right;
                }

                if (word.BoundingBox.TopLeft.Y > trY)
                {
                    trY = word.BoundingBox.TopLeft.Y;
                }
            }

            return new PdfRectangle(blX, blY, trX, trY);
        }

        private PdfRectangle GetBoundingBox180(IReadOnlyList<Word> words)
        {
            var blX = double.MinValue;
            var blY = double.MinValue;
            var trX = double.MaxValue;
            var trY = double.MaxValue;

            for (var i = 0; i < words.Count; i++)
            {
                var word = words[i];
                if (word.BoundingBox.BottomLeft.X > blX)
                {
                    blX = word.BoundingBox.BottomLeft.X;
                }

                if (word.BoundingBox.BottomLeft.Y > blY)
                {
                    blY = word.BoundingBox.BottomLeft.Y;
                }

                var right = word.BoundingBox.BottomLeft.X - word.BoundingBox.Width;
                if (right < trX)
                {
                    trX = right;
                }

                if (word.BoundingBox.TopRight.Y < trY)
                {
                    trY = word.BoundingBox.TopRight.Y;
                }
            }

            return new PdfRectangle(blX, blY, trX, trY);
        }

        private PdfRectangle GetBoundingBox90(IReadOnlyList<Word> words)
        {
            var b = double.MaxValue;
            var r = double.MaxValue;
            var t = double.MinValue;
            var l = double.MinValue;

            for (var i = 0; i < words.Count; i++)
            {
                var word = words[i];
                if (word.BoundingBox.BottomLeft.X < b)
                {
                    b = word.BoundingBox.BottomLeft.X;
                }

                if (word.BoundingBox.BottomRight.Y < r)
                {
                    r = word.BoundingBox.BottomRight.Y;
                }

                var right = word.BoundingBox.BottomLeft.X + word.BoundingBox.Height;
                if (right > t)
                {
                    t = right;
                }

                if (word.BoundingBox.BottomLeft.Y > l)
                {
                    l = word.BoundingBox.BottomLeft.Y;
                }
            }

            return new PdfRectangle(new PdfPoint(b, l), new PdfPoint(t, l),
                                    new PdfPoint(t, r), new PdfPoint(b, r));
        }

        private PdfRectangle GetBoundingBox270(IReadOnlyList<Word> words)
        {
            var t = double.MaxValue;
            var b = double.MinValue;
            var l = double.MaxValue;
            var r = double.MinValue;

            for (var i = 0; i < words.Count; i++)
            {
                var word = words[i];
                if (word.BoundingBox.BottomLeft.X > b)
                {
                    b = word.BoundingBox.BottomLeft.X;
                }

                if (word.BoundingBox.BottomLeft.Y < l)
                {
                    l = word.BoundingBox.BottomLeft.Y;
                }

                var right = word.BoundingBox.BottomLeft.X - word.BoundingBox.Height;
                if (right < t)
                {
                    t = right;
                }

                if (word.BoundingBox.BottomRight.Y > r)
                {
                    r = word.BoundingBox.BottomRight.Y;
                }
            }

            return new PdfRectangle(new PdfPoint(b, l), new PdfPoint(t, l),
                                    new PdfPoint(t, r), new PdfPoint(b, r));
        }

        private static PdfRectangle GetBoundingBoxOther(IReadOnlyList<Word> words)
        {
            var baseLinePoints = words.SelectMany(r => new[]
            {
                r.BoundingBox.BottomLeft,
                r.BoundingBox.BottomRight,
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

            double cos = 0;
            double sin = 1;
            if (sumDiffSquaredX > 1e-3)
            {
                // not a vertical line
                double angleRad = Math.Atan(sumProduct / sumDiffSquaredX); // -π/2 ≤ θ ≤ π/2
                cos = Math.Cos(angleRad);
                sin = Math.Sin(angleRad);
            }

            // Rotate the points to build the axis-aligned bounding box (AABB)
            var inverseRotation = new TransformationMatrix(
                cos, -sin, 0,
                sin, cos, 0,
                0, 0, 1);

            var transformedPoints = words.SelectMany(r => new[]
            {
                    r.BoundingBox.BottomLeft,
                    r.BoundingBox.BottomRight,
                    r.BoundingBox.TopLeft,
                    r.BoundingBox.TopRight
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
            var obb1 = new PdfRectangle(obb.BottomRight, obb.BottomLeft, obb.TopLeft, obb.TopRight);
            var obb2 = new PdfRectangle(obb.TopRight, obb.BottomRight, obb.BottomLeft, obb.TopLeft);
            var obb3 = new PdfRectangle(obb.TopLeft, obb.TopRight, obb.BottomRight, obb.BottomLeft);

            // Find the orientation of the OBB, using the baseline angle
            // Assumes word order is correct
            var firstWord = words[0];
            var lastWord = words[words.Count - 1];

            var baseLineAngle = Distances.Angle(firstWord.BoundingBox.BottomLeft, lastWord.BoundingBox.BottomRight);

            double deltaAngle = Math.Abs(Distances.BoundAngle180(obb.Rotation - baseLineAngle));
            double deltaAngle1 = Math.Abs(Distances.BoundAngle180(obb1.Rotation - baseLineAngle));
            if (deltaAngle1 < deltaAngle)
            {
                deltaAngle = deltaAngle1;
                obb = obb1;
            }

            double deltaAngle2 = Math.Abs(Distances.BoundAngle180(obb2.Rotation - baseLineAngle));
            if (deltaAngle2 < deltaAngle)
            {
                deltaAngle = deltaAngle2;
                obb = obb2;
            }

            double deltaAngle3 = Math.Abs(Distances.BoundAngle180(obb3.Rotation - baseLineAngle));
            if (deltaAngle3 < deltaAngle)
            {
                obb = obb3;
            }

            return obb;
        }
        #endregion

        /// <inheritdoc />
        public override string ToString()
        {
            return Text;
        }
    }
}
