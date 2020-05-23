namespace UglyToad.PdfPig.DocumentLayoutAnalysis.WordExtractor
{
    using Content;
    using Core;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Util;

    /// <summary>
    /// Nearest Neighbour Word Extractor.
    /// This implementation leverages bounding boxes.
    /// </summary>
    public class NearestNeighbourWordExtractor : IWordExtractor
    {
        /// <summary>
        /// Create an instance of Nearest Neighbour Word Extractor, <see cref="NearestNeighbourWordExtractor"/>.
        /// </summary>
        public static NearestNeighbourWordExtractor Instance { get; } = new NearestNeighbourWordExtractor();

        /// <summary>
        /// Gets or sets the maximum number of concurrent tasks enabled. Default value is -1.
        /// <para>A positive property value limits the number of concurrent operations to the set value. 
        /// If it is -1, there is no limit on the number of concurrently running operations.</para>
        /// </summary>
        public int MaxDegreeOfParallelism { get; set; } = -1;

        /// <summary>
        /// Gets the words, using the <see cref="Distances.Manhattan"/> distance.
        /// </summary>
        /// <param name="letters">The letters in the page.</param>
        public IEnumerable<Word> GetWords(IReadOnlyList<Letter> letters)
        {
            double maxDistFunc(Letter l1, Letter l2)
            {
                return Math.Max(Math.Max(Math.Max(Math.Max(Math.Max(
                    Math.Abs(l1.GlyphRectangle.Width),
                    Math.Abs(l2.GlyphRectangle.Width)),
                    Math.Abs(l1.Width)),
                    Math.Abs(l2.Width)),
                    l1.PointSize), l2.PointSize) * 0.2;
            }

            bool filterFunc(Letter l1, Letter l2)
            {
                return !string.IsNullOrWhiteSpace(l2.Value);
            }

            List<Word> wordsH = GetWords(
                letters.Where(l => l.TextOrientation == TextOrientation.Horizontal).ToList(),
                (l1, l2) => maxDistFunc(l1, l2),
                Distances.Manhattan, filterFunc, MaxDegreeOfParallelism)
                .OrderByDescending(x => x.BoundingBox.Bottom)
                .ThenBy(x => x.BoundingBox.Left).ToList();

            var words270 = GetWords(
                letters.Where(l => l.TextOrientation == TextOrientation.Rotate270).ToList(),
                (l1, l2) => maxDistFunc(l1, l2),
                Distances.Manhattan, filterFunc, MaxDegreeOfParallelism)
                .OrderBy(x => x.BoundingBox.Right)
                .ThenByDescending(x => x.BoundingBox.Bottom);
            wordsH.AddRange(words270);

            var words180 = GetWords(
                letters.Where(l => l.TextOrientation == TextOrientation.Rotate180).ToList(),
                (l1, l2) => maxDistFunc(l1, l2),
                Distances.Manhattan, filterFunc, MaxDegreeOfParallelism)
                .OrderBy(x => x.BoundingBox.Top)
                .ThenByDescending(x => x.BoundingBox.Right);
            wordsH.AddRange(words180);

            var words90 = GetWords(
                letters.Where(l => l.TextOrientation == TextOrientation.Rotate90).ToList(),
                (l1, l2) => maxDistFunc(l1, l2),
                Distances.Manhattan, filterFunc, MaxDegreeOfParallelism)
                .OrderByDescending(x => x.BoundingBox.Left)
                .ThenBy(x => x.BoundingBox.Top);
            wordsH.AddRange(words90);

            var wordsU = GetWords(
                letters.Where(l => l.TextOrientation == TextOrientation.Other).ToList(),
                (l1, l2) => maxDistFunc(l1, l2) * 2.0, // allow twice the distance for oriented text
                Distances.Euclidean, filterFunc, MaxDegreeOfParallelism)
                .OrderByDescending(x => x.BoundingBox.Bottom)
                .ThenBy(x => x.BoundingBox.Left);
            wordsH.AddRange(wordsU);

            return wordsH;
        }

        /// <summary>
        /// Gets the words.
        /// </summary>
        /// <param name="pageLetters">The letters in the page.</param>
        /// <param name="maxDistanceFunction">The function that determines the maximum distance between two letters (start and end base line points),
        /// e.g. Max(GlyphRectangle.Width) x 20%. 
        /// <para>If the distance between the two letters is greater, a new word will be created.</para></param>
        /// <param name="distMeasure">The distance measure between two letters (start and end base line points),
        /// e.g. the Manhattan distance.</param>
        /// <param name="filterFunction">Function used to filter out connection between letters, e.g. check if the letters have the same color.
        /// <para>If the function returns false, a new word will be created.</para></param>
        /// <param name="maxDegreeOfParallelism">Sets the maximum number of concurrent tasks enabled. 
        /// <para>A positive property value limits the number of concurrent operations to the set value. 
        /// If it is -1, there is no limit on the number of concurrently running operations.</para></param>
        public List<Word> GetWords(IReadOnlyList<Letter> pageLetters,
            Func<Letter, Letter, double> maxDistanceFunction, Func<PdfPoint, PdfPoint, double> distMeasure,
            Func<Letter, Letter, bool> filterFunction, int maxDegreeOfParallelism)
        {
            if (pageLetters == null || pageLetters.Count == 0) return new List<Word>();

            var groupedIndexes = Clustering.NearestNeighbours(pageLetters,
                distMeasure, maxDistanceFunction,
                l => l.EndBaseLine, l => l.StartBaseLine,
                l => !string.IsNullOrWhiteSpace(l.Value),
                filterFunction,
                maxDegreeOfParallelism).ToList();

            List<Word> words = new List<Word>();
            for (int a = 0; a < groupedIndexes.Count; a++)
            {
                words.Add(new Word(groupedIndexes[a]));
            }

            return words;
        }
    }
}
