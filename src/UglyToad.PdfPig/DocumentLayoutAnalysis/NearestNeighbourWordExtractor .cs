using System;
using System.Collections.Generic;
using System.Linq;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Geometry;
using UglyToad.PdfPig.Util;

namespace UglyToad.PdfPig.DocumentLayoutAnalysis
{
    /// <summary>
    /// Nearest Neighbour Word Extractor, using the <see cref="Distances.Manhattan"/> distance.
    /// This implementation leverages bounding boxes.
    /// </summary>
    public class NearestNeighbourWordExtractor : IWordExtractor
    {
        /// <summary>
        /// Create an instance of Nearest Neighbour Word Extractor, <see cref="NearestNeighbourWordExtractor"/>.
        /// </summary>
        public static IWordExtractor Instance { get; } = new NearestNeighbourWordExtractor();

        /// <summary>
        /// Gets the words.
        /// </summary>
        /// <param name="letters">The letters in the page.</param>
        public IEnumerable<Word> GetWords(IReadOnlyList<Letter> letters)
        {
            List<Word> wordsH = GetWords(
                letters.Where(l => l.TextDirection == TextDirection.Horizontal),
                l => l.GlyphRectangle.Width, Distances.Manhattan)
                .OrderByDescending(x => x.BoundingBox.Bottom)
                .ThenBy(x => x.BoundingBox.Left).ToList();

            List<Word> words180 = GetWords(
                letters.Where(l => l.TextDirection == TextDirection.Rotate180),
                l => l.GlyphRectangle.Width, Distances.Manhattan)
                .OrderBy(x => x.BoundingBox.Top)
                .ThenByDescending(x => x.BoundingBox.Right).ToList();
            wordsH.AddRange(words180);

            List<Word> words90 = GetWords(
                letters.Where(l => l.TextDirection == TextDirection.Rotate90),
                l => l.GlyphRectangle.Height, Distances.Manhattan)
                .OrderByDescending(x => x.BoundingBox.Left)
                .ThenBy(x => x.BoundingBox.Top).ToList();
            wordsH.AddRange(words90);

            List<Word> words270 = GetWords(
                letters.Where(l => l.TextDirection == TextDirection.Rotate270),
                l => l.GlyphRectangle.Height, Distances.Manhattan)
                .OrderBy(x => x.BoundingBox.Right)
                .ThenByDescending(x => x.BoundingBox.Bottom).ToList();
            wordsH.AddRange(words270);

            List<Word> wordsU = GetWords(
                letters.Where(l => l.TextDirection == TextDirection.Unknown),
                l => l.GlyphRectangle.Width, Distances.Manhattan)
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
        /// <param name="metric">The letter's metric to use in the minimum distance
        /// between 2 letters, e.g. GlyphRectangle.Width or GlyphRectangle.Height.</param>
        /// <param name="distMeasure">The distance measure between two start and end base line points,
        /// e.g. the Manhattan distance.</param>
        private List<Word> GetWords(IEnumerable<Letter> pageLetters,
            Func<Letter, decimal> metric, Func<PdfPoint, PdfPoint, double> distMeasure)
        {
            if (pageLetters == null || pageLetters.Count() == 0) return new List<Word>();
            TextDirection textDirection = pageLetters.ElementAt(0).TextDirection;

            if (pageLetters.Any(x => textDirection != x.TextDirection))
            {
                throw new ArgumentException("NNWordExtractor.GetWords(): Mixed Text Direction.");
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

            var groupedIndexes = ClusteringAlgorithms.SimpleTransitiveClosure(letters,
                distMeasure,
                (l1, l2) => Math.Max((double)metric(l1), (double)metric(l2)) * 0.60,
                l => l.EndBaseLine, l => l.StartBaseLine,
                l => !string.IsNullOrWhiteSpace(l.Value),
                (l1, l2) => string.Equals(l1.FontName, l2.FontName, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(l2.Value)).ToList();

            List<Word> words = new List<Word>();
            for (int a = 0; a < groupedIndexes.Count(); a++)
            {
                words.Add(new Word(orderFunc(groupedIndexes[a].Select(i => letters[i]))));
            }

            return words;
        }
    }
}
