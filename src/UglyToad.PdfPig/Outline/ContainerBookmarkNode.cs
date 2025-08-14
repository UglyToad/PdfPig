namespace UglyToad.PdfPig.Outline;

/// <summary>
/// represents a pure container bookmark node: it has a title and child nodes but no destination or action.
/// <para>This is used to handle the common "grouping" bookmarks in PDFs.</para> 
/// </summary>
public class ContainerBookmarkNode : BookmarkNode
{
    /// <summary>
    /// create a container bookmark node.
    /// </summary>
    public ContainerBookmarkNode(string title, int level, IReadOnlyList<BookmarkNode> children)
        : base(title, level, children)
    {
    }
}