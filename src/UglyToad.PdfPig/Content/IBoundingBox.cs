namespace UglyToad.PdfPig.Content
{
    using UglyToad.PdfPig.Core;

    /// <summary>
    /// Interface for classes with a bounding box
    /// </summary>
    public interface IBoundingBox
    {
        /// <summary>
        /// Gets the Bounding Box: The rectangle completely containing this object
        /// </summary>
        PdfRectangle BoundingBox { get; }
    }
}
