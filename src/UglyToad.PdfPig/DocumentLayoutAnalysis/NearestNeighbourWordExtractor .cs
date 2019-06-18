using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        private static List<Word> GetWords(IEnumerable<Letter> pageLetters,
            Func<Letter, decimal> metric, Func<PdfPoint, PdfPoint, double> distMeasure)
        {
            if (pageLetters == null || pageLetters.Count() == 0) return new List<Word>();

            if (pageLetters.Any(x => pageLetters.ElementAt(0).TextDirection != x.TextDirection))
            {
                throw new ArgumentException("NNWordExtractor.GetWords(): Mixed Text Direction.");
            }

            Letter[] letters = pageLetters.ToArray();
            int lettersCount = letters.Length;
            List<PdfPoint> startBaseLines = letters.Select(x => x.StartBaseLine).ToList();

            int[] indexes = Enumerable.Repeat((int)-1, lettersCount).ToArray();

            // Find nearest neighbours indexes
            Parallel.For(0, lettersCount, c =>
            {
                var currentLetter = letters[c];
                // only check neighbours if not a white space
                if (!string.IsNullOrWhiteSpace(currentLetter.Value))
                {
                    int index = currentLetter.EndBaseLine.FindIndexNearest(startBaseLines, distMeasure, out double dist);
                    var pairedLetter = letters[index];

                    if (!string.IsNullOrWhiteSpace(pairedLetter.Value) &&
                        string.Equals(currentLetter.FontName, pairedLetter.FontName, StringComparison.OrdinalIgnoreCase))
                    {
                        decimal minDist = Math.Max(Math.Abs(metric(currentLetter)), Math.Abs(metric(pairedLetter))) * 0.60m;
                        if ((decimal)dist < minDist)
                        {
                            indexes[c] = index;
                        }
                    }
                }
            });

            // Group indexes
            List<List<int>> groupedIndexes = new List<List<int>>();
            List<int> indexDone = new List<int>();
            for (int c = 0; c < lettersCount; c++)
            {
                int i = indexes[c];
                if (i == -1) continue;

                bool isDoneC = indexDone.Contains(c);
                bool isDoneI = indexDone.Contains(i);
                if (isDoneC || isDoneI)
                {
                    if (isDoneC && !isDoneI)
                    {
                        foreach (var pair in groupedIndexes.Where(x => x.Contains(c)))
                        {
                            pair.Add(i);
                        }
                        indexDone.Add(i);
                    }
                    else if (!isDoneC && isDoneI)
                    {
                        foreach (var pair in groupedIndexes.Where(x => x.Contains(i)))
                        {
                            pair.Add(c);
                        }
                        indexDone.Add(c);
                    }
                    else
                    {
                        foreach (var pair in groupedIndexes.Where(x => x.Contains(i)))
                        {
                            if (!pair.Contains(c)) pair.Add(c);
                        }

                        foreach (var pair in groupedIndexes.Where(x => x.Contains(c)))
                        {
                            if (!pair.Contains(i)) pair.Add(i);
                        }
                    }
                }
                else
                {
                    List<int> pair = new List<int>() { c, i };
                    groupedIndexes.Add(pair);
                    indexDone.AddRange(pair);
                }
            }

            // Merge lists with common index 
            for (int c = 0; c < lettersCount; c++)
            {
                List<List<int>> candidates = groupedIndexes.Where(x => x.Any(t => t == c)).ToList();
                if (candidates.Count < 2) continue; // only one group with this index

                List<int> merged = candidates.First();
                groupedIndexes.Remove(merged);
                for (int i = 1; i < candidates.Count; i++)
                {
                    var current = candidates[i];
                    merged = merged.Union(current).ToList();
                    groupedIndexes.Remove(current);
                }
                groupedIndexes.Add(merged);
            }

            List<Word> words = new List<Word>();
            for (int a = 0; a < groupedIndexes.Count(); a++)
            {
                List<Letter> groupedLetters = new List<Letter>();
                foreach (int s in groupedIndexes[a])
                {
                    groupedLetters.Add(letters[s]);
                }
                words.Add(new Word(groupedLetters));
            }

            List<int> indexesNotDone = Enumerable.Range(0, lettersCount).Except(groupedIndexes.SelectMany(x => x)).ToList();
            for (int n = 0; n < indexesNotDone.Count(); n++)
            {
                Letter letter = letters[indexesNotDone[n]];
                words.Add(new Word(new Letter[] { letter }));
            }

            return words;
        }
    }
}
