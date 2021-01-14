namespace UglyToad.PdfPig.Writer
{
    using System.Collections.Generic;

    /// <summary>
    /// A class able to provides instructions for a pdf rearrangement
    /// </summary>
    public interface IPdfArrangement
    {
        /// <summary>
        /// Provides instructions for a pdf rearrangement
        /// </summary>
        /// <param name="pagesCountPerFileIndex"></param>
        /// <returns></returns>
        IEnumerable<(int FileIndex, IReadOnlyCollection<int> PageIndices)> GetArrangements(Dictionary<int, int> pagesCountPerFileIndex);
    }
}