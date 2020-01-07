namespace UglyToad.PdfPig.Core
{
    /// <summary>
    /// NET 4.5 compatible Array.Empty.
    /// </summary>
    public static class EmptyArray<T>
    {
        /// <summary>
        /// An empty array.
        /// </summary>
        public static T[] Instance { get; } = new T[0];
    }
}
