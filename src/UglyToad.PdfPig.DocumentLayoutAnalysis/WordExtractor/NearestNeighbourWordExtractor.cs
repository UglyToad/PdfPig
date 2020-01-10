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
            List<Word> wordsH = GetWords(
                letters.Where(l => l.TextDirection == TextDirection.Horizontal),
                (l1, l2) => Math.Max(l1.GlyphRectangle.Width, l2.GlyphRectangle.Width) * 0.2,
                Distances.Manhattan, MaxDegreeOfParallelism)
                .OrderByDescending(x => x.BoundingBox.Bottom)
                .ThenBy(x => x.BoundingBox.Left).ToList();

            List<Word> words180 = GetWords(
                letters.Where(l => l.TextDirection == TextDirection.Rotate180),
                (l1, l2) => Math.Max(l1.GlyphRectangle.Width, l2.GlyphRectangle.Width) * 0.2,
                Distances.Manhattan, MaxDegreeOfParallelism)
                .OrderBy(x => x.BoundingBox.Top)
                .ThenByDescending(x => x.BoundingBox.Right).ToList();
            wordsH.AddRange(words180);

            List<Word> words90 = GetWords(
                letters.Where(l => l.TextDirection == TextDirection.Rotate90),
                (l1, l2) => Math.Max(l1.GlyphRectangle.Height, l2.GlyphRectangle.Height) * 0.2,
                Distances.Manhattan, MaxDegreeOfParallelism)
                .OrderByDescending(x => x.BoundingBox.Left)
                .ThenBy(x => x.BoundingBox.Top).ToList();
            wordsH.AddRange(words90);

            List<Word> words270 = GetWords(
                letters.Where(l => l.TextDirection == TextDirection.Rotate270),
                (l1, l2) => Math.Max(l1.GlyphRectangle.Height, l2.GlyphRectangle.Height) * 0.2,
                Distances.Manhattan, MaxDegreeOfParallelism)
                .OrderBy(x => x.BoundingBox.Right)
                .ThenByDescending(x => x.BoundingBox.Bottom).ToList();
            wordsH.AddRange(words270);

            List<Word> wordsU = GetWords(
                letters.Where(l => l.TextDirection == TextDirection.Unknown),
                (l1, l2) => Math.Max(l1.GlyphRectangle.Width, l2.GlyphRectangle.Width) * 0.2,
                Distances.Manhattan, MaxDegreeOfParallelism)
                .OrderByDescending(x => x.BoundingBox.Bottom)
                .ThenBy(x => x.BoundingBox.Left).ToList();
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
        private List<Word> GetWords(IEnumerable<Letter> pageLetters,
            Func<Letter, Letter, double> maxDistanceFunction, Func<PdfPoint, PdfPoint, double> distMeasure,
            int maxDegreeOfParallelism)
        {
            if (pageLetters == null || pageLetters.Count() == 0) return new List<Word>();
            TextDirection textDirection = pageLetters.ElementAt(0).TextDirection;

            if (pageLetters.Any(x => textDirection != x.TextDirection))
            {
                throw new ArgumentException("NearestNeighbourWordExtractor.GetWords(): Mixed Text Direction.");
            }

            Func<IEnumerable<Letter>, IReadOnlyList<Letter>> orderFunc = l => l.OrderBy(x => x.GlyphRectangle.Left).ToList();
            if (textDirection == TextDirection.Rotate180)
            {
                orderFunc = l => l.OrderByDescending(x => x.GlyphRectangle.Right).ToList();
            }
            else if (textDirection == TextDirection.Rotate90)
            {
                orderFunc = l => l.OrderByDescending(x => x.GlyphRectangle.Top).ToList();
            }
            else if (textDirection == TextDirection.Rotate270)
            {
                orderFunc = l => l.OrderBy(x => x.GlyphRectangle.Bottom).ToList();
            }

            Letter[] letters = pageLetters.ToArray();

            var groupedIndexes = ClusteringAlgorithms.ClusterNearestNeighbours(letters,
                distMeasure, maxDistanceFunction,
                l => l.EndBaseLine, l => l.StartBaseLine,
                l => !string.IsNullOrWhiteSpace(l.Value),
                (l1, l2) => !string.IsNullOrWhiteSpace(l2.Value),
                maxDegreeOfParallelism).ToList();

            List<Word> words = new List<Word>();
            for (int a = 0; a < groupedIndexes.Count(); a++)
            {
                words.Add(new Word(orderFunc(groupedIndexes[a].Select(i => letters[i]))));
            }

            return words;
        }
    }
}
