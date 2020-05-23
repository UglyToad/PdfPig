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

            Text = string.Join(" ", lines.Select(x => x.Text));

            var minX = lines.Min(x => x.BoundingBox.Left);
            var minY = lines.Min(x => x.BoundingBox.Bottom);
            var maxX = lines.Max(x => x.BoundingBox.Right);
            var maxY = lines.Max(x => x.BoundingBox.Top);
            BoundingBox = new PdfRectangle(minX, minY, maxX, maxY);

            TextOrientation = lines[0].TextOrientation;
        }

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
