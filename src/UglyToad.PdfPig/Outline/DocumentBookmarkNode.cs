namespace UglyToad.PdfPig.Outline
{
    using System;
    using System.Collections.Generic;
    using Destinations;

    /// <inheritdoc />
    /// <summary>
    /// A node in the <see cref="Bookmarks" /> of a PDF document which corresponds
    /// to a location in the current document.
    /// </summary>
    public class DocumentBookmarkNode : BookmarkNode
    {
        /// <summary>
        /// The page number where the bookmark is located.
        /// </summary>
        public int PageNumber { get; }

        /// <inheritdoc />
        /// <summary>
        /// Create a new <see cref="DocumentBookmarkNode"/>.
        /// </summary>
        public DocumentBookmarkNode(string title, int level, ExplicitDestination destination, IReadOnlyList<BookmarkNode> children)
            : base(title, level, destination, children)
        {
            PageNumber = destination.PageNumber;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"page #{PageNumber}, {Level}, {Title}";
        }
    }
}