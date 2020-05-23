namespace UglyToad.PdfPig.DocumentLayoutAnalysis
{
    using Content;
    using Core;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// A block of text.
    /// </summary>
    public class TextBlock
    {
        /// <summary>
        /// The separator used between lines in the block.
        /// </summary>
        public readonly string Separator;

        /// <summary>
        /// The text of the block.
        /// </summary>
        public string Text { get; }

        /// <summary>
        /// The text orientation of the block.
        /// </summary>
        public TextOrientation TextOrientation { get; }

        /// <summary>
        /// The rectangle completely containing the block.
        /// </summary>
        public PdfRectangle BoundingBox { get; }

        /// <summary>
        /// The text lines contained in the block.
        /// </summary>
        public IReadOnlyList<TextLine> TextLines { get; }

        /// <summary>
        /// The reading order index. Starts at 0. A value of -1 means the block is not ordered.
        /// </summary>
        public int ReadingOrder { get; private set; }

        /// <summary>
        /// Create a new <see cref="TextBlock"/>.
        /// </summary>
        /// <param name="lines">The words contained in the line, in the correct order.</param>
        /// <param name="separator">The separator used between lines in the block.</param>
        public TextBlock(IReadOnlyList<TextLine> lines, string separator = "\n")
        {
            if (lines == null)
            {
                throw new ArgumentNullException(nameof(lines));
            }

            if (lines.Count == 0)
            {
                throw new ArgumentException("Empty lines provided.", nameof(lines));
            }

            Separator = separator;

            ReadingOrder = -1;

            TextLines = lines;

            if (lines.Count == 1)
            {
                BoundingBox = lines[0].BoundingBox;
                Text = lines[0].Text;
                TextOrientation = lines[0].TextOrientation;
            }
            else
            {
                var tempTextOrientation = lines[0].TextOrientation;
                if (tempTextOrientation != TextOrientation.Other)
                {
                    foreach (var letter in lines)
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
                        BoundingBox = GetBoundingBoxH(lines);
                        break;

                    case TextOrientation.Rotate180:
                        BoundingBox = GetBoundingBox180(lines);
                        break;

                    case TextOrientation.Rotate90:
                        BoundingBox = GetBoundingBox90(lines);
                        break;

                    case TextOrientation.Rotate270:
                        BoundingBox = GetBoundingBox270(lines);
                        break;

                    case TextOrientation.Other:
                    default:
                        BoundingBox = GetBoundingBoxOther(lines);
                        break;
                }

                Text = string.Join(separator, lines.Select(x => x.Text));
                TextOrientation = tempTextOrientation;
            }
        }

        #region Bounding box
        private PdfRectangle GetBoundingBoxH(IReadOnlyList<TextLine> lines)
        {
            var blX = double.MaxValue;
            var trX = double.MinValue;
            var blY = double.MaxValue;
            var trY = double.MinValue;

            for (var i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                if (line.BoundingBox.BottomLeft.X < blX)
                {
                    blX = line.BoundingBox.BottomLeft.X;
                }

                if (line.BoundingBox.BottomLeft.Y < blY)
                {
                    blY = line.BoundingBox.BottomLeft.Y;
                }

                var right = line.BoundingBox.BottomLeft.X + line.BoundingBox.Width;
                if (right > trX)
                {
                    trX = right;
                }

                if (line.BoundingBox.TopLeft.Y > trY)
                {
                    trY = line.BoundingBox.TopLeft.Y;
                }
            }

            return new PdfRectangle(blX, blY, trX, trY);
        }

        private PdfRectangle GetBoundingBox180(IReadOnlyList<TextLine> lines)
        {
            var blX = double.MinValue;
            var blY = double.MinValue;
            var trX = double.MaxValue;
            var trY = double.MaxValue;

            for (var i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                if (line.BoundingBox.BottomLeft.X > blX)
                {
                    blX = line.BoundingBox.BottomLeft.X;
                }

                if (line.BoundingBox.BottomLeft.Y > blY)
                {
                    blY = line.BoundingBox.BottomLeft.Y;
                }

                var right = line.BoundingBox.BottomLeft.X - line.BoundingBox.Width;
                if (right < trX)
                {
                    trX = right;
                }

                if (line.BoundingBox.TopRight.Y < trY)
                {
                    trY = line.BoundingBox.TopRight.Y;
                }
            }

            return new PdfRectangle(blX, blY, trX, trY);
        }

        private PdfRectangle GetBoundingBox90(IReadOnlyList<TextLine> lines)
        {
            var b = double.MaxValue;
            var r = double.MaxValue;
            var t = double.MinValue;
            var l = double.MinValue;

            for (var i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                if (line.BoundingBox.BottomLeft.X < b)
                {
                    b = line.BoundingBox.BottomLeft.X;
                }

                if (line.BoundingBox.BottomRight.Y < r)
                {
                    r = line.BoundingBox.BottomRight.Y;
                }

                var right = line.BoundingBox.BottomLeft.X + line.BoundingBox.Height;
                if (right > t)
                {
                    t = right;
                }

                if (line.BoundingBox.BottomLeft.Y > l)
                {
                    l = line.BoundingBox.BottomLeft.Y;
                }
            }

            return new PdfRectangle(new PdfPoint(t, l), new PdfPoint(t, r),
                                    new PdfPoint(b, l), new PdfPoint(b, r));
        }

        private PdfRectangle GetBoundingBox270(IReadOnlyList<TextLine> lines)
        {
            var t = double.MaxValue;
            var b = double.MinValue;
            var l = double.MaxValue;
            var r = double.MinValue;

            for (var i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                if (line.BoundingBox.BottomLeft.X > b)
                {
                    b = line.BoundingBox.BottomLeft.X;
                }

                if (line.BoundingBox.BottomLeft.Y < l)
                {
                    l = line.BoundingBox.BottomLeft.Y;
                }

                var right = line.BoundingBox.BottomLeft.X - line.BoundingBox.Height;
                if (right < t)
                {
                    t = right;
                }

                if (line.BoundingBox.BottomRight.Y > r)
                {
                    r = line.BoundingBox.BottomRight.Y;
                }
            }

            return new PdfRectangle(new PdfPoint(t, l), new PdfPoint(t, r),
                                    new PdfPoint(b, l), new PdfPoint(b, r));
        }

        private PdfRectangle GetBoundingBoxOther(IReadOnlyList<TextLine> lines)
        {
            var points = lines.SelectMany(l => new[]
            {
                l.BoundingBox.BottomLeft,
                l.BoundingBox.BottomRight,
                l.BoundingBox.TopLeft,
                l.BoundingBox.TopRight
            });

            // Candidates bounding boxes
            var obb = Geometry.GeometryExtensions.MinimumAreaRectangle(points);
            var obb1 = new PdfRectangle(obb.BottomLeft, obb.TopLeft, obb.BottomRight, obb.TopRight);
            var obb2 = new PdfRectangle(obb.BottomRight, obb.BottomLeft, obb.TopRight, obb.TopLeft);
            var obb3 = new PdfRectangle(obb.TopRight, obb.BottomRight, obb.TopLeft, obb.BottomLeft);

            // Find the orientation of the OBB, using the baseline angle
            // Assumes line order is correct
            var lastLine = lines[lines.Count - 1];

            var baseLineAngle = Distances.BoundAngle180(Distances.Angle(lastLine.BoundingBox.BottomLeft, lastLine.BoundingBox.BottomRight));

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

        /// <summary>
        /// Sets the <see cref="TextBlock"/>'s reading order.
        /// </summary>
        /// <param name="readingOrder"></param>
        public void SetReadingOrder(int readingOrder)
        {
            if (readingOrder < -1)
            {
                throw new ArgumentException("The reading order should be more or equal to -1. A value of -1 means the block is not ordered.", nameof(readingOrder));
            }
            this.ReadingOrder = readingOrder;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return Text;
        }
    }
}
