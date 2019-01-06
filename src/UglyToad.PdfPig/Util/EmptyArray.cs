namespace UglyToad.PdfPig.Util
{
    /// <summary>
    /// NET 4.5 compatible Array.Empty.
    /// </summary>
    internal static class EmptyArray<T>
    {
        /// <summary>
        /// An empty array.
        /// </summary>
        public static T[] Instance { get; } = new T[0];
    }
}
