using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Geometry;
using UglyToad.PdfPig.Util;

namespace UglyToad.PdfPig.DocumentLayoutAnalysis
{
    /// <summary>
    /// Algorithm that retrieve blocks that are labelled as decoration (e.g. headers, footers) for each page in the document, using a content and a geometric similarity measure.
    /// <para>Decoration blocks are blocks that contains information such as author names, publication titles, page numbers, etc.
    /// They are printed repeatedly at the border of each page, usually placed inside headers or footers, but sometimes also at the 
    /// left or right edge of the page.</para>
    /// <para>See section 5.1 of 'Unsupervised document structure analysis of digital scientific articles' by S. Klampfl, M. Granitzer, K. Jack, R. Kern.</para>
    /// </summary>
    public static class DecorationTextBlockClassifier
    {
        private static Regex numbersPattern = new Regex(@"(\d+)|(\b([MDCLXVI]+)\b)", RegexOptions.IgnoreCase);
        private static string replacementChar = "@";

        /// <summary>
        /// Get blocks that are labelled as decoration for each page in the document, using a content and a geometric similarity measure.
        /// <para>Decoration blocks are blocks that contains information such as author names, publication titles, page numbers, etc.
        /// They are printed repeatedly at the border of each page, usually placed inside headers or footers, but sometimes also at the 
        /// left or right edge of the page.</para>
        /// </summary>
        /// <param name="pages">The <see cref="Page"/>s in the document. All of them are needed for the algorithm to work.</param>
        /// <param name="wordExtractor"></param>
        /// <param name="pageSegmenter"></param>
        /// <param name="similarityThreshold">Minimum similarity score to decide wether a block is labelled as decoration or not.</param>
        /// <param name="n">Number of blocks in a page to be considered when looking for decoration blocks.</param>
        /// <param name="maxDegreeOfParallelism">Sets the maximum number of concurrent tasks enabled. 
        /// <para>A positive property value limits the number of concurrent operations to the set value. 
        /// If it is -1, there is no limit on the number of concurrently running operations.</para></param>
        public static IReadOnlyList<IReadOnlyList<TextBlock>> Get(IReadOnlyList<Page> pages,
            IWordExtractor wordExtractor, IPageSegmenter pageSegmenter,
            double similarityThreshold = 0.25, int n = 5, int maxDegreeOfParallelism = -1)
        {
            if (pages.Count < 2)
            {
                throw new ArgumentException("The algorithm cannot be used with a document of less than 2 pages.", nameof(pages));
            }

            ConcurrentDictionary<int, IReadOnlyList<TextBlock>> pagesBlocks = new ConcurrentDictionary<int, IReadOnlyList<TextBlock>>();

            ParallelOptions parallelOptions = new ParallelOptions() { MaxDegreeOfParallelism = maxDegreeOfParallelism };

            Parallel.For(0, pages.Count(), parallelOptions, p =>
            {
                var words = pages[p].GetWords(wordExtractor);
                var blocks = pageSegmenter.GetBlocks(words);
                if (!pagesBlocks.TryAdd(p, blocks))
                {
                    throw new ArgumentException("Cannot add element with index " + p + " in ConcurrentDictionary.");
                }
            });

            return Get(pagesBlocks.OrderBy(x => x.Key).Select(x => x.Value).ToList(),
                similarityThreshold,
                n,
                maxDegreeOfParallelism);
        }

        /// <summary>
        /// Get blocks that are labelled as decoration for each page in the document, using a content and a geometric similarity measure.
        /// <para>Decoration blocks are blocks that contains information such as author names, publication titles, page numbers, etc.
        /// They are printed repeatedly at the border of each page, usually placed inside headers or footers, but sometimes also at the 
        /// left or right edge of the page.</para>
        /// </summary>
        /// <param name="pagesTextBlocks">The <see cref="TextBlock"/>s of every pages in the document. All of them are needed for the algorithm to work.</param>
        /// <param name="similarityThreshold">Minimum similarity score to decide wether a block is labelled as decoration or not.</param>
        /// <param name="n">Number of blocks in a page to be considered when looking for decoration blocks.</param>
        /// <param name="maxDegreeOfParallelism">Sets the maximum number of concurrent tasks enabled. 
        /// <para>A positive property value limits the number of concurrent operations to the set value. 
        /// If it is -1, there is no limit on the number of concurrently running operations.</para></param>
        public static IReadOnlyList<IReadOnlyList<TextBlock>> Get(IReadOnlyList<IReadOnlyList<TextBlock>> pagesTextBlocks,
            double similarityThreshold = 0.25, int n = 5, int maxDegreeOfParallelism = -1)
        {
            if (pagesTextBlocks.Count < 2)
            {
                throw new ArgumentException("The algorithm cannot be used with a document of less than 2 pages.", nameof(pagesTextBlocks));
            }

            ConcurrentDictionary<int, List<TextBlock>> pageDecorations = new ConcurrentDictionary<int, List<TextBlock>>();

            ParallelOptions parallelOptions = new ParallelOptions() { MaxDegreeOfParallelism = maxDegreeOfParallelism };

            Parallel.For(0, pagesTextBlocks.Count, parallelOptions, p =>
            {
                if (!pageDecorations.TryAdd(p, new List<TextBlock>()))
                {
                    throw new ArgumentException("Cannot add element with index " + p + " in ConcurrentDictionary.");
                }

                int pMinus1 = GetPreviousPageNumber(p, pagesTextBlocks.Count);
                int pPlus1 = GetNextPageNumber(p, pagesTextBlocks.Count);

                var previousPage = pagesTextBlocks[pMinus1];
                var currentPage = pagesTextBlocks[p];
                var nextPage = pagesTextBlocks[pPlus1];

                int nCurrent = Math.Min(n, currentPage.Count);

                // First, for each page, we sort all blocks on the page in four different orders: 
                // - from top to bottom (based on the minimum y coordinate), 
                // - from bottom to top (maximum y coordinate), 
                // - from left to right (minimum x coordinate), 
                // - from right to left (maximumx coordinate). 

                // From top to bottom (based on the minimum y coordinate)
                previousPage = previousPage.OrderByDescending(b => b.BoundingBox.Bottom).ThenBy(b => b.BoundingBox.Left).ToList();
                currentPage = currentPage.OrderByDescending(b => b.BoundingBox.Bottom).ThenBy(b => b.BoundingBox.Left).ToList();
                nextPage = nextPage.OrderByDescending(b => b.BoundingBox.Bottom).ThenBy(b => b.BoundingBox.Left).ToList();

                for (int i = 0; i < nCurrent; i++)
                {
                    var current = currentPage[i];
                    var score = Score(current, previousPage, nextPage, similarityThreshold, n);
                    if (score >= similarityThreshold)
                    {
                        if (!pageDecorations[p].Contains(current)) pageDecorations[p].Add(current);
                    }
                }

                // From bottom to top (maximum y coordinate)
                previousPage = previousPage.OrderBy(b => b.BoundingBox.Top).ThenBy(b => b.BoundingBox.Left).ToList();
                currentPage = currentPage.OrderBy(b => b.BoundingBox.Top).ThenBy(b => b.BoundingBox.Left).ToList();
                nextPage = nextPage.OrderBy(b => b.BoundingBox.Top).ThenBy(b => b.BoundingBox.Left).ToList();

                for (int i = 0; i < nCurrent; i++)
                {
                    var current = currentPage[i];
                    var score = Score(current, previousPage, nextPage, similarityThreshold, n);
                    if (score >= similarityThreshold)
                    {
                        if (!pageDecorations[p].Contains(current)) pageDecorations[p].Add(current);
                    }
                }

                // From left to right (minimum x coordinate)
                previousPage = previousPage.OrderBy(b => b.BoundingBox.Left).ThenBy(b => b.BoundingBox.Top).ToList();
                currentPage = currentPage.OrderBy(b => b.BoundingBox.Left).ThenBy(b => b.BoundingBox.Top).ToList();
                nextPage = nextPage.OrderBy(b => b.BoundingBox.Left).ThenBy(b => b.BoundingBox.Top).ToList();

                for (int i = 0; i < nCurrent; i++)
                {
                    var current = currentPage[i];
                    var score = Score(current, previousPage, nextPage, similarityThreshold, n);
                    if (score >= similarityThreshold)
                    {
                        if (!pageDecorations[p].Contains(current)) pageDecorations[p].Add(current);
                    }
                }

                // From right to left (maximumx coordinate)
                previousPage = previousPage.OrderByDescending(b => b.BoundingBox.Right).ThenBy(b => b.BoundingBox.Top).ToList();
                currentPage = currentPage.OrderByDescending(b => b.BoundingBox.Right).ThenBy(b => b.BoundingBox.Top).ToList();
                nextPage = nextPage.OrderByDescending(b => b.BoundingBox.Right).ThenBy(b => b.BoundingBox.Top).ToList();

                for (int i = 0; i < nCurrent; i++)
                {
                    var current = currentPage[i];
                    var score = Score(current, previousPage, nextPage, similarityThreshold, n);
                    if (score >= similarityThreshold)
                    {
                        if (!pageDecorations[p].Contains(current)) pageDecorations[p].Add(current);
                    }
                }
            });

            return pageDecorations.OrderBy(x => x.Key).Select(x => x.Value).ToList();
        }

        /// <summary>
        /// [The content similarity] is calculated from the normalized edit 
        /// distance between the two content strings, where digits are replaced with “@” chars.
        /// A content similarity of 1 is reached when both strings are exactly equal.
        /// </summary>
        private static double ContentSimilarity(TextBlock b1, TextBlock b2)
        {
            double similarity = 1.0 - Distances.MinimumEditDistanceNormalised(
                numbersPattern.Replace(b1.Text, replacementChar),
                numbersPattern.Replace(b2.Text, replacementChar));

            return similarity;
        }

        /// <summary>
        /// The geometric similarity is the area of the intersection between the two boundingbox rectangles divided by the larger of the two boundingboxes.
        /// </summary>
        private static double GeomSimilarity(TextBlock b1, TextBlock b2)
        {
            double similarity = 0;
            var intersect = b1.BoundingBox.Intersect(b2.BoundingBox);
            if (intersect.HasValue)
            {
                similarity = intersect.Value.Area / Math.Max(b1.BoundingBox.Area, b2.BoundingBox.Area);
            }

            return similarity;
        }

        /// <summary>
        /// This similarity score is a value in the range [0,1] and given 
        /// by the product between the content and the geometric similarity.
        /// </summary>
        private static double Similarity(TextBlock b1, TextBlock b2)
        {
            return ContentSimilarity(b1, b2) * GeomSimilarity(b1, b2);
        }

        private static double ScoreI(TextBlock current, TextBlock previous, TextBlock next)
        {
            return 0.5 * (Similarity(current, next) + Similarity(current, previous));
        }

        private static double Score(TextBlock current, IReadOnlyList<TextBlock> previous, IReadOnlyList<TextBlock> next,
            double threshold, int n)
        {
            n = Math.Min(n, Math.Min(previous.Count, next.Count));
            double score = 0;

            for (int i = 0; i < n; i++)
            {
                var s = ScoreI(current, previous[i], next[i]);
                if (s > score) score = s;
                if (score >= threshold) return score;
            }
            return score;
        }

        /// <summary>
        /// If the document has more than three pages, we compare blocks on the next or previous page with an even or odd number,
        /// depending on whether the current page number is even or odd, to account for cases with a two-sided layout.
        /// </summary>
        /// <param name="currentPage">Current page number.</param>
        /// <param name="pagesCount">Total number of pages in the document.</param>
        private static int GetPreviousPageNumber(int currentPage, int pagesCount)
        {
            int pMinus1 = currentPage - 1 >= 0 ? currentPage - 1 : pagesCount - 1;
            if (pagesCount > 3)
            {
                pMinus1 = pMinus1 - 1 >= 0 ? pMinus1 - 1 : pagesCount - 1;
            }
            return pMinus1;
        }

        /// <summary>
        /// If the  document has more than three pages, we compare blocks on the next or previous page with an even or odd number,
        /// depending on whether the current page number is even or odd, to account for cases with a two-sided layout.
        /// </summary>
        /// <param name="currentPage">Current page number.</param>
        /// <param name="pagesCount">Total number of pages in the document.</param>
        private static int GetNextPageNumber(int currentPage, int pagesCount)
        {
            int pPlus1 = currentPage + 1 < pagesCount ? currentPage + 1 : 0;
            if (pagesCount > 3)
            {
                pPlus1 = pPlus1 + 1 < pagesCount ? pPlus1 + 1 : 0;
            }
            return pPlus1;
        }
    }
}
