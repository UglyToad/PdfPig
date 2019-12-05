namespace UglyToad.PdfPig.Outline
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// A node in the <see cref="Bookmarks"/> (also known as outlines) of a PDF document.
    /// </summary>
    public abstract class BookmarkNode
    {
        /// <summary>
        /// The text displayed for this node.
        /// </summary>
        public string Title { get; }

        /// <summary>
        /// The bookmark's sub-bookmarks.
        /// </summary>
        public IReadOnlyList<BookmarkNode> Children { get; }

        /// <summary>
        /// Whether this node is a leaf node (has no children).
        /// </summary>
        public bool IsLeaf { get; }

        /// <summary>
        /// The node's level in the hierarchy.
        /// </summary>
        public int Level { get; }

        /// <summary>
        /// Create a new <see cref="BookmarkNode"/>.
        /// </summary>
        protected BookmarkNode(string title, int level, IReadOnlyList<BookmarkNode> children)
        {
            Title = title;
            Level = level;
            Children = children ?? throw new ArgumentNullException(nameof(children));
            IsLeaf = children.Count == 0;
        }
    }
}
