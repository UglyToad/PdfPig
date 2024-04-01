namespace UglyToad.PdfPig.DocumentLayoutAnalysis
{
    using Content;
    using Core;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Text edges extractor. Text edges are where words have either their BoundingBox's left, right or mid coordinates aligned on the same vertical line.
    /// <para>Useful to detect text columns, tables, justified text, lists, etc.</para>
    /// </summary>
    public static class TextEdgesExtractor
    {
        /// <summary>
        /// Functions used to define left, middle and right edges.
        /// </summary>
        private static readonly Tuple<EdgeType, Func<PdfRectangle, double>>[] edgesFuncs =
        [
            Tuple.Create<EdgeType, Func<PdfRectangle, double>>(EdgeType.Left,   x => Math.Round(x.Left, 0)),                // use BoundingBox's left coordinate
            Tuple.Create<EdgeType, Func<PdfRectangle, double>>(EdgeType.Mid, x => Math.Round(x.Left + x.Width / 2, 0)),     // use BoundingBox's mid coordinate
            Tuple.Create<EdgeType, Func<PdfRectangle, double>>(EdgeType.Right,  x => Math.Round(x.Right, 0))                // use BoundingBox's right coordinate
        ];

        /// <summary>
        /// Get the text edges.
        /// </summary>
        /// <param name="pageWords">The words in the page.</param>
        /// <param name="minimumElements">The minimum number of elements to define a text edge.</param>
        /// <param name="maxDegreeOfParallelism">Sets the maximum number of concurrent tasks enabled.
        /// <para>A positive property value limits the number of concurrent operations to the set value.
        /// If it is -1, there is no limit on the number of concurrently running operations.</para></param>
        public static IReadOnlyDictionary<EdgeType, List<PdfLine>> GetEdges(IEnumerable<Word> pageWords, int minimumElements = 4,
            int maxDegreeOfParallelism = -1)
        {
            if (minimumElements < 0)
            {
                throw new ArgumentException("TextEdgesExtractor.GetEdges(): The minimum number of elements should be positive.", nameof(minimumElements));
            }

            var cleanWords = pageWords.Where(x => !string.IsNullOrWhiteSpace(x.Text.Trim()));

            ConcurrentDictionary<EdgeType, List<PdfLine>> dictionary = new ConcurrentDictionary<EdgeType, List<PdfLine>>();

            ParallelOptions parallelOptions = new ParallelOptions() { MaxDegreeOfParallelism = maxDegreeOfParallelism };

            Parallel.ForEach(edgesFuncs, parallelOptions, f => dictionary.TryAdd(f.Item1, GetVerticalEdges(cleanWords, f.Item2, minimumElements)));
            return dictionary.ToDictionary(x => x.Key, x => x.Value);
        }

        private static List<PdfLine> GetVerticalEdges(IEnumerable<Word> pageWords, Func<PdfRectangle, double> func, int minimumElements)
        {
            Dictionary<double, List<Word>> edges = pageWords.GroupBy(x => func(x.BoundingBox))
                .Where(x => x.Count() >= minimumElements).ToDictionary(gdc => gdc.Key, gdc => gdc.ToList());
            Dictionary<double, List<List<Word>>> cleanEdges = new Dictionary<double, List<List<Word>>>();

            foreach (var edge in edges)
            {
                var sortedEdges = edge.Value.OrderBy(x => x.BoundingBox.Bottom).ToList();
                cleanEdges.Add(edge.Key, new List<List<Word>>());

                var cuttings = pageWords.Except(edge.Value) // remove selected words
                                                            // words that cut the vertical line
                    .Where(x => x.BoundingBox.Left < edge.Key && x.BoundingBox.Right > edge.Key)
                    // and that are within the boundaries of the edge
                    .Where(k => k.BoundingBox.Bottom > edge.Value.Min(z => z.BoundingBox.Bottom)
                        && k.BoundingBox.Top < edge.Value.Max(z => z.BoundingBox.Top))
                    .OrderBy(x => x.BoundingBox.Bottom).ToList();

                if (cuttings.Count > 0)
                {
                    foreach (var cut in cuttings)
                    {
                        var group1 = sortedEdges.Where(x => x.BoundingBox.Top < cut.BoundingBox.Bottom).ToList();
                        if (group1.Count >= minimumElements) cleanEdges[edge.Key].Add(group1);
                        sortedEdges = sortedEdges.Except(group1).ToList();
                    }
                    if (sortedEdges.Count >= minimumElements) cleanEdges[edge.Key].Add(sortedEdges);
                }
                else
                {
                    cleanEdges[edge.Key].Add(sortedEdges);
                }
            }

            return cleanEdges.SelectMany(x => x.Value.Select(y => new PdfLine(x.Key, y.Min(w => w.BoundingBox.Bottom), x.Key, y.Max(w => w.BoundingBox.Top)))).ToList();
        }
    }

    /// <summary>
    /// The type of text edge.
    /// </summary>
    public enum EdgeType
    {
        /// <summary>
        /// Text edges where words have their BoundingBox's left coordinate aligned on the same vertical line.
        /// </summary>
        Left = 0,

        /// <summary>
        /// Text edges where words have their BoundingBox's mid coordinate aligned on the same vertical line.
        /// </summary>
        Mid = 1,

        /// <summary>
        /// Text edges where words have their BoundingBox's right coordinate aligned on the same vertical line.
        /// </summary>
        Right = 2
    }
}
