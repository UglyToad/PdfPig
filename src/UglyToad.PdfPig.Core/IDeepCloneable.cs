namespace UglyToad.PdfPig.Core
{
    /// <summary>
    /// Indicates the type may be cloned making entirely independent copies.
    /// </summary>
    public interface IDeepCloneable<out T>
    {
        /// <summary>
        /// Clone the type including all referenced data.
        /// </summary>
        T DeepClone();
    }
}
