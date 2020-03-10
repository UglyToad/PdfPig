namespace UglyToad.PdfPig.DocumentLayoutAnalysis.WordExtractor
{
    using Content;
    using Core;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Util;

    /// <summary>
    /// Nearest Neighbour Word Extractor, using the <see cref="Distances.Manhattan"/> distance.
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
        /// Gets the words.
        /// </summary>
        /// <param name="letters">The letters in the page.</param>
        public IEnumerable<Word> GetWords(IReadOnlyList<Letter> letters)
        {
            double baseMaxFunc(Letter l1, Letter l2)
            {
                return Math.Max(Math.Max(Math.Max(
                    Math.Abs(l1.GlyphRectangle.Width),
                    Math.Abs(l2.GlyphRectangle.Width)),
                    Math.Abs(l1.Width)),
                    Math.Abs(l2.Width));
            }

            List<Word> wordsH = GetWords(
                letters.Where(l => l.TextDirection == TextDirection.Horizontal).ToList(),
                (l1, l2) => baseMaxFunc(l1, l2) * 0.2,
                Distances.Manhattan, MaxDegreeOfParallelism)
                .OrderByDescending(x => x.BoundingBox.Bottom)
                .ThenBy(x => x.BoundingBox.Left).ToList();

            var words270 = GetWords(
                letters.Where(l => l.TextDirection == TextDirection.Rotate270).ToList(),
                (l1, l2) => baseMaxFunc(l1, l2) * 0.2,
                Distances.Manhattan, MaxDegreeOfParallelism)
                .OrderBy(x => x.BoundingBox.Right)
                .ThenByDescending(x => x.BoundingBox.Bottom);
            wordsH.AddRange(words270);

            var words180 = GetWords(
                letters.Where(l => l.TextDirection == TextDirection.Rotate180).ToList(),
                (l1, l2) => baseMaxFunc(l1, l2) * 0.2,
                Distances.Manhattan, MaxDegreeOfParallelism)
                .OrderBy(x => x.BoundingBox.Top)
                .ThenByDescending(x => x.BoundingBox.Right);
            wordsH.AddRange(words180);

            var words90 = GetWords(
                letters.Where(l => l.TextDirection == TextDirection.Rotate90).ToList(),
                (l1, l2) => baseMaxFunc(l1, l2) * 0.2,
                Distances.Manhattan, MaxDegreeOfParallelism)
                .OrderByDescending(x => x.BoundingBox.Left)
                .ThenBy(x => x.BoundingBox.Top);
            wordsH.AddRange(words90);

            var wordsU = GetWords(
                letters.Where(l => l.TextDirection == TextDirection.Other).ToList(),
                (l1, l2) => baseMaxFunc(l1, l2) * 0.5,
                Distances.Euclidean, MaxDegreeOfParallelism)
                .OrderByDescending(x => x.BoundingBox.Bottom)
                .ThenBy(x => x.BoundingBox.Left);
            wordsH.AddRange(wordsU);

            return wordsH;
        }

        /// <summary>
        /// Private method to get the words.
        /// </summary>
        /// <param name="pageLetters">The letters in the page, they must have
        /// the same text directions.</param>
        /// <param name="maxDistanceFunction">The function that determines the maximum distance between two Letters,
        /// e.g. Max(GlyphRectangle.Width) x 20%.</param>
        /// <param name="distMeasure">The distance measure between two start and end base line points,
        /// e.g. the Manhattan distance.</param>
        /// <param name="maxDegreeOfParallelism">Sets the maximum number of concurrent tasks enabled. 
        /// <para>A positive property value limits the number of concurrent operations to the set value. 
        /// If it is -1, there is no limit on the number of concurrently running operations.</para></param>
        public List<Word> GetWords(IReadOnlyList<Letter> pageLetters,
            Func<Letter, Letter, double> maxDistanceFunction, Func<PdfPoint, PdfPoint, double> distMeasure,
            int maxDegreeOfParallelism)
        {
            if (pageLetters == null || pageLetters.Count == 0) return new List<Word>();
            TextDirection textDirection = pageLetters[0].TextDirection;

            if (pageLetters.Any(x => textDirection != x.TextDirection))
            {
                throw new ArgumentException("NearestNeighbourWordExtractor.GetWords(): Mixed Text Direction.");
            }

            var groupedIndexes = ClusteringAlgorithms.ClusterNearestNeighbours(pageLetters,
                distMeasure, maxDistanceFunction,
                l => l.EndBaseLine, l => l.StartBaseLine,
                l => !string.IsNullOrWhiteSpace(l.Value),
                (l1, l2) => !string.IsNullOrWhiteSpace(l2.Value),
                maxDegreeOfParallelism).ToList();

            List<Word> words = new List<Word>();
            for (int a = 0; a < groupedIndexes.Count; a++)
            {
                words.Add(new Word(groupedIndexes[a].Select(i => pageLetters[i]).ToList()));
            }

            return words;
        }
    }
}
