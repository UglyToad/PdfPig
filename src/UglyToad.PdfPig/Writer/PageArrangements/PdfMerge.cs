namespace UglyToad.PdfPig.Writer.PageArrangements
{
    using System.Collections.Generic;
    using System.Linq;

    class PdfMerge : IPdfArrangement
    {
        private readonly IReadOnlyList<IReadOnlyList<int>> pagesBundle;
        public PdfMerge(IReadOnlyList<IReadOnlyList<int>> pagesBundle)
        {
            this.pagesBundle = pagesBundle;
        }

        public IEnumerable<(int FileIndex, IReadOnlyCollection<int> PageIndices)> GetArrangements(Dictionary<int, int> pagesCountPerFileIndex)
        {
            if (pagesBundle is null)
            {
                foreach (var kvp in pagesCountPerFileIndex.OrderBy(d => d.Key))
                {
                    yield return (kvp.Key, Enumerable.Range(1, pagesCountPerFileIndex[kvp.Key]).ToArray());
                }
            }
            else
            {
                for (var i = 0; i < pagesBundle.Count; i++)
                {
                    if (pagesBundle[i] is null)
                    {
                        yield return (i, Enumerable.Range(1, pagesCountPerFileIndex[i]).ToArray());
                    }
                    else
                    {
                        yield return (i, pagesBundle[i]);
                    }
                }
            }
        }
    }
}