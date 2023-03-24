namespace UglyToad.PdfPig.Outline
{
    using System;
    using System.Collections.Generic;

    /// <inheritdoc />
    /// <summary>
    /// A node in the <see cref="Bookmarks" /> of a PDF document which corresponds
    /// to a uniform resource identifier on the Internet.
    /// </summary>
    public class UriBookmarkNode : BookmarkNode
    {
        /// <summary>
        /// The uniform resource identifier to resolve.
        /// </summary>
        public string Uri { get; }

        /// <inheritdoc />
        /// <summary>
        /// Create a new <see cref="ExternalBookmarkNode" />.
        /// </summary>
        public UriBookmarkNode(string title, int level, string uri, IReadOnlyList<BookmarkNode> children) : base(title, level, children)
        {
            Uri = uri ?? throw new ArgumentNullException(nameof(uri));
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"URI '{Uri}', {Level}, {Title}";
        }
    }
}