namespace UglyToad.PdfPig.DocumentLayoutAnalysis
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Content;
    using Core;

    /// <summary>
    /// A block of text.
    /// </summary>
    public class TextBlock
    {
        /// <summary>
        /// The text of the block.
        /// </summary>
        public string Text { get; }

        /// <summary>
        /// The text direction of the block.
        /// </summary>
        public TextDirection TextDirection { get; }

        /// <summary>
        /// The rectangle completely containing the block.
        /// </summary>
        public PdfRectangle BoundingBox { get; }

        /// <summary>
        /// The text lines contained in the block.
        /// </summary>
        public IReadOnlyList<TextLine> TextLines { get; }

        /// <summary>
        /// Create a new <see cref="TextBlock"/>.
        /// </summary>
        /// <param name="lines"></param>
        public TextBlock(IReadOnlyList<TextLine> lines)
        {
            if (lines == null)
            {
                throw new ArgumentNullException(nameof(lines));
            }

            if (lines.Count == 0)
            {
                throw new ArgumentException("Empty lines provided.", nameof(lines));
            }

            TextLines = lines;

            Text = string.Join(" ", lines.Select(x => x.Text));

            var minX = lines.Min(x => x.BoundingBox.Left);
            var minY = lines.Min(x => x.BoundingBox.Bottom);
            var maxX = lines.Max(x => x.BoundingBox.Right);
            var maxY = lines.Max(x => x.BoundingBox.Top);
            BoundingBox = new PdfRectangle(minX, minY, maxX, maxY);

            TextDirection = lines[0].TextDirection;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return Text;
        }
    }
}
