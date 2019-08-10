using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Geometry;

namespace UglyToad.PdfPig.DocumentLayoutAnalysis
{
    /// <summary>
    /// The Docstrum algorithm is a bottom-up page segmentation technique based on nearest-neighborhood 
    /// clustering of connected components extracted from the document. 
    /// This implementation leverages bounding boxes and does not exactly replicates the original algorithm.
    /// <para>See 'The document spectrum for page layout analysis.' by L. O’Gorman.</para>
    /// </summary>
    public class DocstrumBB : IPageSegmenter
    {
        /// <summary>
        /// Create an instance of Docstrum for bounding boxes page segmenter, <see cref="DocstrumBB"/>.
        /// </summary>
        public static DocstrumBB Instance { get; } = new DocstrumBB();

        /// <summary>
        /// Get the blocks.
        /// <para>Uses wlAngleLB = -30, wlAngleUB = 30, blAngleLB = -135, blAngleUB = -45, blMulti = 1.3.</para>
        /// </summary>
        /// <param name="pageWords"></param>
        /// <returns></returns>
        public IReadOnlyList<TextBlock> GetBlocks(IEnumerable<Word> pageWords)
        {
            return GetBlocks(pageWords, -30, 30, -135, -45, 1.3);
        }

        /// <summary>
        /// Get the blocks. See original paper for more information.
        /// </summary>
        /// <param name="pageWords"></param>
        /// <param name="wlAngleLB">Within-line lower bound angle.</param>
        /// <param name="wlAngleUB">Within-line upper bound angle.</param>
        /// <param name="blAngleLB">Between-line lower bound angle.</param>
        /// <param name="blAngleUB">Between-line upper bound angle.</param>
        /// <param name="blMultiplier">Multiplier that gives the maximum perpendicular distance between 
        /// text lines for blocking. Maximum distance will be this number times the between-line 
        /// distance found by the analysis.</param>
        /// <returns></returns>
        public IReadOnlyList<TextBlock> GetBlocks(IEnumerable<Word> pageWords, double wlAngleLB, double wlAngleUB,
            double blAngleLB, double blAngleUB, double blMultiplier)
        {
            var pageWordsArr = pageWords.Where(w => !string.IsNullOrWhiteSpace(w.Text)).ToArray(); // remove white spaces

            var withinLineDistList = new ConcurrentBag<double[]>();
            var betweenLineDistList = new ConcurrentBag<double[]>();

            // 1. Estimate in line and between line spacing
            Parallel.For(0, pageWordsArr.Length, i =>
            {
                var word = pageWordsArr[i];

                // Within-line distance
                var pointWL = GetNearestPointData(pageWordsArr, word,
                    bb => bb.BottomRight, bb => bb.BottomRight,
                    bb => bb.BottomLeft, bb => bb.BottomLeft,
                    wlAngleLB, wlAngleUB, Distances.Horizontal);
                if (pointWL != null) withinLineDistList.Add(pointWL);

                // Between-line distance
                var pointBL = GetNearestPointData(pageWordsArr, word,
                    bb => bb.BottomLeft, bb => bb.Centroid,
                    bb => bb.TopLeft, bb => bb.Centroid,
                    blAngleLB, blAngleUB, Distances.Vertical);
                if (pointBL != null) betweenLineDistList.Add(pointBL);
            });

            double withinLineDistance = GetPeakAverageDistance(withinLineDistList);
            double betweenLineDistance = GetPeakAverageDistance(betweenLineDistList);

            // 2. Find lines of text
            double maxDistWL = Math.Min(3 * withinLineDistance, Math.Sqrt(2) * betweenLineDistance);
            var lines = GetLines(pageWordsArr, maxDistWL).ToArray();

            // 3. Find blocks of text
            double maxDistBL = blMultiplier * betweenLineDistance;
            return GetLinesGroups(lines, maxDistBL).ToList();
        }

        /// <summary>
        /// Get information on the nearest point, filtered for angle.
        /// </summary>
        /// <param name="words"></param>
        /// <param name="pivot"></param>
        /// <param name="funcPivotDist"></param>
        /// <param name="funcPivotAngle"></param>
        /// <param name="funcPointsDist"></param>
        /// <param name="funcPointsAngle"></param>
        /// <param name="angleStart"></param>
        /// <param name="angleEnd"></param>
        /// <param name="finalDistMEasure"></param>
        /// <returns></returns>
        private double[] GetNearestPointData(Word[] words, Word pivot, Func<PdfRectangle,
            PdfPoint> funcPivotDist, Func<PdfRectangle, PdfPoint> funcPivotAngle,
            Func<PdfRectangle, PdfPoint> funcPointsDist, Func<PdfRectangle, PdfPoint> funcPointsAngle,
            double angleStart, double angleEnd,
            Func<PdfPoint, PdfPoint, double> finalDistMEasure)
        {
            var pointR = funcPivotDist(pivot.BoundingBox);
            var filtered = words.Where(w =>
            {
                var angleWL = Distances.Angle(funcPivotAngle(pivot.BoundingBox), funcPointsAngle(w.BoundingBox));
                return (angleWL >= angleStart && angleWL <= angleEnd);
            }).ToList();
            filtered.Remove(pivot); // remove itself

            if (filtered.Count > 0)
            {
                int index = pointR.FindIndexNearest(
                    filtered.Select(w => funcPointsDist(w.BoundingBox)).ToList(),
                    Distances.Euclidean, out double distWL);

                if (index >= 0)
                {
                    var matchWL = filtered[index];
                    return new double[]
                    {
                        (double)pivot.Letters.Select(l => l.FontSize).Mode(),
                        finalDistMEasure(pointR, funcPointsDist(matchWL.BoundingBox))
                    };
                }
            }
            return null;
        }

        /// <summary>
        /// Build lines via transitive closure.
        /// </summary>
        /// <param name="words"></param>
        /// <param name="maxDist"></param>
        /// <returns></returns>
        private IEnumerable<TextLine> GetLines(Word[] words, double maxDist)
        {
            TextDirection textDirection = words[0].TextDirection;
            var groupedIndexes = ClusteringAlgorithms.SimpleTransitiveClosure(words, Distances.Euclidean,
                (w1, w2) => maxDist,
                w => w.BoundingBox.BottomRight, w => w.BoundingBox.BottomLeft,
                w => true,
                (w1, w2) =>
                {
                    var angleWL = Distances.Angle(w1.BoundingBox.BottomRight, w2.BoundingBox.BottomLeft); // compare bottom right with bottom left for angle
                    return (angleWL >= -30 && angleWL <= 30);
                }).ToList();

            Func<IEnumerable<Word>, IReadOnlyList<Word>> orderFunc = l => l.OrderBy(x => x.BoundingBox.Left).ToList();
            if (textDirection == TextDirection.Rotate180)
            {
                orderFunc = l => l.OrderByDescending(x => x.BoundingBox.Right).ToList();
            }
            else if (textDirection == TextDirection.Rotate90)
            {
                orderFunc = l => l.OrderByDescending(x => x.BoundingBox.Top).ToList();
            }
            else if (textDirection == TextDirection.Rotate270)
            {
                orderFunc = l => l.OrderBy(x => x.BoundingBox.Bottom).ToList();
            }

            for (int a = 0; a < groupedIndexes.Count(); a++)
            {
                yield return new TextLine(orderFunc(groupedIndexes[a].Select(i => words[i])));
            }
        }

        /// <summary>
        /// Build blocks via transitive closure.
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="maxDist"></param>
        /// <returns></returns>
        private IEnumerable<TextBlock> GetLinesGroups(TextLine[] lines, double maxDist)
        {
            var groupedIndexes = ClusteringAlgorithms.SimpleTransitiveClosure(lines, Distances.Euclidean,
                (l1, l2) => maxDist,
                l => l.BoundingBox.TopLeft, l => l.BoundingBox.BottomLeft,
                l => true, (l1, l2) => true).ToList();

            for (int a = 0; a < groupedIndexes.Count(); a++)
            {
                yield return new TextBlock(groupedIndexes[a].Select(i => lines[i]).ToList());
            }
        }

        /// <summary>
        /// Get the average distance value of the peak bucket of the histogram.
        /// </summary>
        /// <param name="values">array[0]=font size, array[1]=distance</param>
        /// <returns></returns>
        private double GetPeakAverageDistance(IEnumerable<double[]> values)
        {
            int max = (int)values.Max(x => x[1]) + 1;
            int[] distrib = new int[max];

            // Create histogram with buckets of size 1.
            for (int i = 0; i < max; i++)
            {
                distrib[i] = values.Where(x => x[1] > i && x[1] <= i + 1).Count();
            }

            var peakIndex = Array.IndexOf(distrib, distrib.Max());

            return values.Where(v => v[1] > peakIndex && v[1] <= peakIndex + 1).Average(x => x[1]);
        }
    }
}
