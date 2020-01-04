namespace UglyToad.PdfPig.DocumentLayoutAnalysis
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Content;
    using Core;

    /// <summary>
    /// A Leaf node used in the <see cref="RecursiveXYCut"/> algorithm, i.e. a block.
    /// </summary>
    internal class XYLeaf : XYNode
    {
        /// <summary>
        /// Returns true if this node is a leaf, false otherwise.
        /// </summary>
        public override bool IsLeaf => true;

        /// <summary>
        /// The words in the leaf.
        /// </summary>
        public IReadOnlyList<Word> Words { get; }

        /// <summary>
        /// The number of words in the leaf.
        /// </summary>
        public override int CountWords() => Words?.Count ?? 0;

        /// <summary>
        /// Returns null as a leaf doesn't have leafs.
        /// </summary>
        public override List<XYLeaf> GetLeafs()
        {
            return null;
        }

        /// <summary>
        /// Gets the lines of the leaf.
        /// </summary>
        public IReadOnlyList<TextLine> GetLines()
        {
            return Words.GroupBy(x => x.BoundingBox.Bottom).OrderByDescending(x => x.Key)
                .Select(x => new TextLine(x.ToList())).ToArray();
        }

        /// <summary>
        /// Create a new <see cref="XYLeaf"/>.
        /// </summary>
        /// <param name="words">The words contained in the leaf.</param>
        public XYLeaf(params Word[] words) : this(words == null ? null : words.ToList())
        {

        }

        /// <summary>
        /// Create a new <see cref="XYLeaf"/>.
        /// </summary>
        /// <param name="words">The words contained in the leaf.</param>
        public XYLeaf(IEnumerable<Word> words) : base(null)
        {
            if (words == null)
            {
                throw new ArgumentException("XYLeaf(): The words contained in the leaf cannot be null.", nameof(words));
            }

            double left = words.Min(b => b.BoundingBox.Left);
            double right = words.Max(b => b.BoundingBox.Right);

            double bottom = words.Min(b => b.BoundingBox.Bottom);
            double top = words.Max(b => b.BoundingBox.Top);

            BoundingBox = new PdfRectangle(left, bottom, right, top);
            Words = words.ToArray();
        }
    }
}
