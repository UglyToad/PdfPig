namespace UglyToad.PdfPig.Outline
{
    using Destinations;
    using System;
    using System.Collections.Generic;

    /// <inheritdoc />
    /// <summary>
    /// A node in the <see cref="Bookmarks" /> of a PDF document which corresponds
    /// to a location in an external file.
    /// </summary>
    public class ExternalBookmarkNode : BookmarkNode
    {
        /// <summary>
        /// The name of the file containing this bookmark.
        /// </summary>
        public string FileName { get; }

        /// <inheritdoc />
        /// <summary>
        /// Create a new <see cref="ExternalBookmarkNode" />.
        /// </summary>
        public ExternalBookmarkNode(string title, int level, ExplicitDestination destination, string fileName, IReadOnlyList<BookmarkNode> children) : base(title, level, destination, children)
        {
            FileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"file '{FileName}', {Level}, {Title}";
        }
    }
}