namespace UglyToad.PdfPig.Outline
{
    using Destinations;

    /// <inheritdoc />
    /// <summary>
    /// A node in the <see cref="Bookmarks" /> of a PDF document which corresponds
    /// to a location in an embedded file.
    /// </summary>
    public class EmbeddedBookmarkNode : DocumentBookmarkNode
    {
        /// <summary>
        /// The file specification for the embedded file
        /// </summary>
        public string FileSpecification { get; }

        /// <inheritdoc />
        /// <summary>
        /// Create a new <see cref="ExternalBookmarkNode" />.
        /// </summary>
        public EmbeddedBookmarkNode(string title, int level, ExplicitDestination destination, IReadOnlyList<BookmarkNode> children, string fileSpecification) : base(title, level, destination, children)
        {
            FileSpecification = fileSpecification ?? throw new ArgumentNullException(nameof(fileSpecification));
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"Embedded file '{FileSpecification}', {Level}, {Title}";
        }
    }
}
