namespace UglyToad.PdfPig.Content
{
    using UglyToad.PdfPig.Core;

    /// <summary>
    /// Interface for classes with a bounding box
    /// </summary>
    public interface IBlock
    {
        /// <summary>
        /// Gets the Bounding Box: The rectangle completely containing this object
        /// </summary>
        PdfRectangle BoundingBox { get; }
    }

    /// <summary>
    /// Interface for classes with a bounding box and text
    /// </summary>
    public interface ILettersBlock : IBlock
    {
        /// <summary>
        /// The text of the block
        /// </summary>
        string Text { get; }

        /// <summary>
        /// Text orientation of the block.
        /// </summary>
        TextOrientation TextOrientation { get; }

        /// <summary>
        /// The letters contained in the Block
        /// </summary>
        IReadOnlyList<Letter> Letters { get; }
    }
}
