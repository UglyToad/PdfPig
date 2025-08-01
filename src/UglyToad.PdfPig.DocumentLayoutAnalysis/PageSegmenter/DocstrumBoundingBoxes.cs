﻿namespace UglyToad.PdfPig.DocumentLayoutAnalysis.PageSegmenter
{
    using Content;
    using Core;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using UglyToad.PdfPig.DocumentLayoutAnalysis.ReadingOrderDetector;

    /// <summary>
    /// The Document Spectrum (Docstrum) algorithm is a bottom-up page segmentation technique based on nearest-neighbourhood
    /// clustering of connected components extracted from the document.
    /// This implementation leverages bounding boxes and does not exactly replicates the original algorithm.
    /// <para>See 'The document spectrum for page layout analysis.' by L. O'Gorman.</para>
    /// </summary>
    public class DocstrumBoundingBoxes : IPageSegmenter
    {
        private readonly DocstrumBoundingBoxesOptions options;

        /// <summary>
        /// Create an instance of Docstrum for bounding boxes page segmenter, <see cref="DocstrumBoundingBoxes"/>.
        /// </summary>
        public static DocstrumBoundingBoxes Instance { get; } = new DocstrumBoundingBoxes();

        /// <summary>
        /// Create an instance of Docstrum for bounding boxes page segmenter using default options values.
        /// </summary>
        public DocstrumBoundingBoxes() : this(new DocstrumBoundingBoxesOptions())
        {
        }

        /// <summary>
        /// Create an instance of Docstrum for bounding boxes page segmenter using options values.
        /// </summary>
        /// <param name="options">The <see cref="DocstrumBoundingBoxesOptions"/> to use.</param>
        /// <exception cref="ArgumentException"></exception>
        public DocstrumBoundingBoxes(DocstrumBoundingBoxesOptions options)
        {
            this.options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// Get the blocks.
        /// </summary>
        /// <param name="words">The page's words to segment into <see cref="TextBlock"/>s.</param>
        /// <returns>The <see cref="TextBlock"/>s generated by the document spectrum method.</returns>
        public IReadOnlyList<TextBlock> GetBlocks(IEnumerable<Word> words)
        {
            if (words is null)
            {
                return Array.Empty<TextBlock>();
            }

            // Avoid multiple enumeration and unnecessary ToArray() if already a list
            var wordList = words as IReadOnlyList<Word> ?? words.ToArray();
            if (wordList.Count == 0)
            {
                return Array.Empty<TextBlock>();
            }

            return GetBlocks(wordList,
                options.WithinLineBounds, options.WithinLineMultiplier, options.WithinLineBinSize,
                options.BetweenLineBounds, options.BetweenLineMultiplier, options.BetweenLineBinSize,
                options.AngularDifferenceBounds,
                options.Epsilon,
                options.WordSeparator, options.LineSeparator,
                options.MaxDegreeOfParallelism);
        }

        /// <summary>
        /// Get the blocks. See original paper for more information.
        /// </summary>
        /// <param name="words">The words to segment into <see cref="TextBlock"/>s.</param>
        /// <param name="wlBounds">Angle bounds for words to be considered as neighbours on the same line.</param>
        /// <param name="wlMultiplier">Multiplier that gives the maximum euclidian distance between words for building lines.
        /// Maximum distance will be this number times the within-line distance found by the analysis.</param>
        /// <param name="wlBinSize">The bin size used when building the within-line distances distribution.</param>
        /// <param name="blBounds">Angle bounds for words to be considered as neighbours on separate lines.</param>
        /// <param name="blMultiplier">Multiplier that gives the maximum perpendicular distance between
        /// text lines for blocking. Maximum distance will be this number times the between-line
        /// distance found by the analysis.</param>
        /// <param name="blBinSize">The bin size used when building the between-line distances distribution.</param>
        /// <param name="angularDifferenceBounds">The angular difference bounds between two lines to be considered in the same block. This defines if two lines are parallel enough.</param>
        /// <param name="epsilon">Precision when testing equalities.</param>
        /// <param name="wordSeparator">Separator used between words when building lines.</param>
        /// <param name="lineSeparator">Separator used between lines when building paragraphs.</param>
        /// <param name="maxDegreeOfParallelism">Sets the maximum number of concurrent tasks enabled.
        /// <para>A positive property value limits the number of concurrent operations to the set value.
        /// If it is -1, there is no limit on the number of concurrently running operations.</para></param>
        /// <returns>The <see cref="TextBlock"/>s generated by the document spectrum method.</returns>
        private IReadOnlyList<TextBlock> GetBlocks(IReadOnlyList<Word> words,
            AngleBounds wlBounds, double wlMultiplier, int wlBinSize,
            AngleBounds blBounds, double blMultiplier, int blBinSize,
            AngleBounds angularDifferenceBounds,
            double epsilon,
            string wordSeparator, string lineSeparator,
            int maxDegreeOfParallelism)
        {
            // Filter out white spaces
            words = words.Where(w => !string.IsNullOrWhiteSpace(w.Text)).ToList();
            if (words.Count == 0)
            {
                return Array.Empty<TextBlock>();
            }

            // 1. Estimate within line and between line spacing
            if (!GetSpacingEstimation(words, wlBounds, wlBinSize, blBounds, blBinSize,
                maxDegreeOfParallelism,
                out double withinLineDistance, out double betweenLineDistance))
            {
                if (double.IsNaN(withinLineDistance))
                {
                    withinLineDistance = 0;
                }

                if (double.IsNaN(betweenLineDistance))
                {
                    betweenLineDistance = 0;
                }
            }

            // 2. Determination of Text Lines
            double maxWithinLineDistance = wlMultiplier * withinLineDistance;
            var lines = GetLines(words, maxWithinLineDistance, wlBounds, wordSeparator, maxDegreeOfParallelism).ToArray();

            // 3. Structural Block Determination
            double maxBetweenLineDistance = blMultiplier * betweenLineDistance;
            return GetStructuralBlocks(lines, maxBetweenLineDistance, angularDifferenceBounds, epsilon, lineSeparator, maxDegreeOfParallelism).ToList();
        }

        #region Spacing Estimation
        /// <summary>
        /// Estimation of within-line and between-line spacing.
        /// <para>This is the Docstrum algorithm's 1st step.</para>
        /// </summary>
        /// <param name="words">The list of words.</param>
        /// <param name="wlBounds">Angle bounds for words to be considered as neighbours on the same line.</param>
        /// <param name="wlBinSize">The bin size used when building the within-line distances distribution.</param>
        /// <param name="blBounds">Angle bounds for words to be considered as neighbours on separate lines.</param>
        /// <param name="blBinSize">The bin size used when building the between-line distances distribution.</param>
        /// <param name="maxDegreeOfParallelism">Sets the maximum number of concurrent tasks enabled.
        /// <para>A positive property value limits the number of concurrent operations to the set value.
        /// If it is -1, there is no limit on the number of concurrently running operations.</para></param>
        /// <param name="withinLineDistance">The estimated within-line distance. Computed as the average peak value of distribution.</param>
        /// <param name="betweenLineDistance">The estimated between-line distance. Computed as the average peak value of distribution.</param>
        /// <returns>False if either 'withinLineDistance' or 'betweenLineDistance' is <see cref="double.NaN"/>.</returns>
        public static bool GetSpacingEstimation(IReadOnlyList<Word> words,
            AngleBounds wlBounds, int wlBinSize,
            AngleBounds blBounds, int blBinSize,
            int maxDegreeOfParallelism,
            out double withinLineDistance, out double betweenLineDistance)
        {
            ParallelOptions parallelOptions = new ParallelOptions() { MaxDegreeOfParallelism = maxDegreeOfParallelism };

            var withinLineDistList = new ConcurrentBag<double>();
            var betweenLineDistList = new ConcurrentBag<double>();

            // 1. Estimate within line and between line spacing
            KdTree<Word> kdTreeBottomLeft = new KdTree<Word>(words, w => w.BoundingBox.BottomLeft);

            Parallel.For(0, words.Count, parallelOptions, i =>
            {
                var word = words[i];

                // Within-line distance
                // 1.1.1 Find the 2 closest neighbours words to the candidate, using euclidean distance.
                foreach (var n in kdTreeBottomLeft.FindNearestNeighbours(word, 2, w => w.BoundingBox.BottomRight, Distances.Euclidean))
                {
                    // 1.1.2 Check if the neighbour word is within the angle of the candidate 
                    if (wlBounds.Contains(AngleWL(word, n.Item1)))
                    {
                        withinLineDistList.Add(Distances.Euclidean(word.BoundingBox.BottomRight, n.Item1.BoundingBox.BottomLeft));
                    }
                }

                // Between-line distance
                // 1.2.1 Find the 2 closest neighbours words to the candidate, using euclidean distance.
                foreach (var n in kdTreeBottomLeft.FindNearestNeighbours(word, 2, w => w.BoundingBox.TopLeft, Distances.Euclidean))
                {
                    // 1.2.2 Check if the candidate words is within the angle
                    var angle = AngleBL(word, n.Item1);
                    if (blBounds.Contains(angle))
                    {
                        // 1.2.3 Compute the vertical (between-line) distance between the candidate
                        // and the neighbour and add it to the between-line distances list
                        double hypotenuse = Distances.Euclidean(word.BoundingBox.Centroid, n.Item1.BoundingBox.Centroid);

                        // Angle is kept within [-90, 90] 
                        if (angle > 90)
                        {
                            angle -= 180;
                        }

                        var dist = Math.Abs(hypotenuse * Math.Cos((90 - angle) * Math.PI / 180))
                            - word.BoundingBox.Height / 2.0 - n.Item1.BoundingBox.Height / 2.0;

                        // The perpendicular distance can be negative because of the subtractions.
                        // Could occur when words are overlapping, we ignore that.
                        if (dist >= 0)
                        {
                            betweenLineDistList.Add(dist);
                        }
                    }
                }
            });

            // Compute average peak value of distribution
            double? withinLinePeak = GetPeakAverageDistance(withinLineDistList, wlBinSize);
            double? betweenLinePeak = GetPeakAverageDistance(betweenLineDistList, blBinSize);

            withinLineDistance = withinLinePeak ?? double.NaN;
            betweenLineDistance = betweenLinePeak ?? double.NaN;

            return withinLinePeak.HasValue && betweenLinePeak.HasValue;
        }

        /// <summary>
        /// Get the average distance value of the peak bucket of the histogram.
        /// </summary>
        /// <param name="distances">The set of distances to average.</param>
        /// <param name="binLength"></param>
        private static double? GetPeakAverageDistance(IEnumerable<double> distances, int binLength = 1)
        {
            if (!distances.Any())
            {
                return null;
            }

            if (binLength <= 0)
            {
                throw new ArgumentException("DocstrumBoundingBoxes: the bin length must be positive when commputing peak average distance.", nameof(binLength));
            }

            double maxDbl = Math.Ceiling(distances.Max());
            if (maxDbl > int.MaxValue)
            {
                throw new OverflowException($"Error while casting maximum distance of {maxDbl} to integer.");
            }

            int max = (int)maxDbl;
            if (max == 0)
            {
                max = binLength;
            }
            else
            {
                binLength = binLength > max ? max : binLength;
            }

            var bins = Enumerable.Range(0, (int)Math.Ceiling(max / (double)binLength) + 1)
                .Select(x => x * binLength)
                .ToDictionary(x => x, _ => new List<double>());

            foreach (var distance in distances)
            {
                int bin = (int)Math.Floor(distance / binLength);
                if (bin < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(bin), "DocstrumBoundingBoxes: Negative distance found while commputing peak average distance.");
                }
                bins[bins.Keys.ElementAt(bin)].Add(distance);
            }

            var best = default(List<double>);
            foreach (var bin in bins)
            {
                if (best == null || bin.Value.Count > best.Count)
                {
                    best = bin.Value;
                }
            }

            return best?.Average();
        }
        #endregion

        #region Text Lines
        /// <summary>
        /// Get the <see cref="TextLine"/>s by grouping words using nearest neighbours.
        /// <para>This is the Docstrum algorithm's 2nd step.</para>
        /// </summary>
        /// <param name="words">The words to segment into <see cref="TextLine"/>s.</param>
        /// <param name="maxWLDistance">The maximum within-line distance. Computed as the estimated within-line spacing times the within-line multiplier in the default implementation.</param>
        /// <param name="wlBounds">Angle bounds for words to be considered as neighbours on the same line.</param>
        /// <param name="wordSeparator">Separator used between words when building lines.</param>
        /// <param name="maxDegreeOfParallelism">Sets the maximum number of concurrent tasks enabled.
        /// <para>A positive property value limits the number of concurrent operations to the set value.
        /// If it is -1, there is no limit on the number of concurrently running operations.</para></param>
        /// <returns>The <see cref="TextLine"/>s built.</returns>
        public static IEnumerable<TextLine> GetLines(IReadOnlyList<Word> words, double maxWLDistance, AngleBounds wlBounds,
            string wordSeparator, int maxDegreeOfParallelism)
        {
            var groupedWords = Clustering.NearestNeighbours(words,
                2,
                Distances.Euclidean,
                (_, __) => maxWLDistance,
                pivot => pivot.BoundingBox.BottomRight,
                candidate => candidate.BoundingBox.BottomLeft,
                _ => true,
                (pivot, candidate) => wlBounds.Contains(AngleWL(pivot, candidate)),
                maxDegreeOfParallelism).ToList();

            foreach (var g in groupedWords)
            {
                yield return new TextLine(g.OrderByReadingOrder(), wordSeparator);
            }
        }

        /// <summary>
        /// Helper function to compute the within line angle between the pivot's bottom
        /// right and the candidate's bottom left points, taking in account the pivot's rotation.
        /// <para>-90 ≤ θ ≤ 90.</para>
        /// </summary>
        private static double AngleWL(Word pivot, Word candidate)
        {
            var angle = Distances.BoundAngle180(Distances.Angle(pivot.BoundingBox.BottomRight, candidate.BoundingBox.BottomLeft) - pivot.BoundingBox.Rotation);

            // Angle is kept within [-90;90] degree to handle overlapping words
            if (angle > 90)
            {
                angle -= 180;
            }
            else if (angle < -90)
            {
                angle += 180;
            }

            return angle;
        }
        #endregion

        #region Blocking
        /// <summary>
        /// Get the <see cref="TextBlock"/>s.
        /// <para>This is the Docstrum algorithm's 3rd and final step.</para>
        /// <para>
        /// Method: We want to measure the distance between two lines using the following method:
        /// <br>- We check if two lines are overlapping horizontally and compute the perpendicular distance.</br>
        /// <br>- We check if the angle between the two line is within 'angularDifference'.</br>
        /// <br>- If the two lines are not overlapping or the angle is too wide, the distance is set to the infinity.</br>
        /// <para>If two text lines are approximately parallel, close in perpendicular distance, and they either overlap to some specified degree or are separated by only a small distance in parallel distance, then they are said to meet the criteria to belong to the same structural block.</para>
        /// </para>
        /// </summary>
        /// <param name="lines">The lines to segment into <see cref="TextBlock"/>s.</param>
        /// <param name="maxBLDistance">The maximum between-line distance. Computed as the estimated between-line spacing times the between-line multiplier in the default implementation.</param>
        /// <param name="angularDifferenceBounds">The angular difference bounds between two lines to be considered in the same block. This defines if two lines are parallel enough.</param>
        /// <param name="epsilon">Precision when testing equalities.</param>
        /// <param name="lineSeparator">Separator used between lines when building paragraphs.</param>
        /// <param name="maxDegreeOfParallelism">Sets the maximum number of concurrent tasks enabled.
        /// <para>A positive property value limits the number of concurrent operations to the set value.
        /// If it is -1, there is no limit on the number of concurrently running operations.</para></param>
        /// <returns>The <see cref="TextBlock"/>s built.</returns>
        public static IEnumerable<TextBlock> GetStructuralBlocks(IReadOnlyList<TextLine> lines,
            double maxBLDistance, AngleBounds angularDifferenceBounds, double epsilon, string lineSeparator, int maxDegreeOfParallelism)
        {
            /******************************************************************************************************
             * We want to measure the distance between two lines using the following method:
             *  We check if two lines are overlapping horizontally and compute the perpendicular distance.
             *  We check if the angle between the two line is within 'angularDifference'.
             *  If the two lines are not overlapping or the angle is too wide, the distance is set to the infinity.
             *  
             *  If two text lines are approximately parallel, close in perpendicular distance, and they either 
             *  overlap to some specified degree or are separated by only a small distance in parallel distance, 
             *  then they are said to meet the criteria to belong to the same structural block.
             ******************************************************************************************************/

            var groupedLines = Clustering.NearestNeighbours(
                lines,
                (l1, l2) => PerpendicularOverlappingDistance(l1, l2, angularDifferenceBounds, epsilon),
                (_, __) => maxBLDistance,
                pivot => new PdfLine(pivot.BoundingBox.BottomLeft, pivot.BoundingBox.BottomRight),
                candidate => new PdfLine(candidate.BoundingBox.TopLeft, candidate.BoundingBox.TopRight),
                _ => true,
                (_, __) => true,
                maxDegreeOfParallelism).ToList();

            foreach (var g in groupedLines)
            {
                yield return new TextBlock(g.OrderByReadingOrder(), lineSeparator);
            }
        }

        /// <summary>
        /// Perpendicular overlapping distance.
        /// </summary>
        /// <param name="line1"></param>
        /// <param name="line2"></param>
        /// <param name="angularDifferenceBounds"></param>
        /// <param name="epsilon"></param>
        private static double PerpendicularOverlappingDistance(PdfLine line1, PdfLine line2, AngleBounds angularDifferenceBounds, double epsilon)
        {
            if (GetStructuralBlockingParameters(line1, line2, epsilon, out double theta, out _, out double ed))
            {
                // Angle is kept within [-90;90]
                if (theta > 90)
                {
                    theta -= 180;
                }
                else if (theta < -90)
                {
                    theta += 180;
                }

                if (!angularDifferenceBounds.Contains(theta))
                {
                    // exclude because not parallel enough
                    return double.PositiveInfinity;
                }

                return Math.Abs(ed);
            }
            else
            {
                // nonoverlapped
                return double.PositiveInfinity;
            }
        }

        private sealed class PdfPointXYComparer : IComparer<PdfPoint>
        {
            public static readonly PdfPointXYComparer Instance = new();

            public int Compare(PdfPoint p1, PdfPoint p2)
            {
                int comp = p1.X.CompareTo(p2.X);
                return comp == 0 ? p1.Y.CompareTo(p2.Y) : comp;
            }
        }

        private sealed class PdfPointYComparer : IComparer<PdfPoint>
        {
            public static readonly PdfPointYComparer Instance = new();

            public int Compare(PdfPoint p1, PdfPoint p2)
            {
                return p1.Y.CompareTo(p2.Y);
            }
        }

        /// <summary>
        /// Get the structural blocking parameters.
        /// </summary>
        /// <param name="i"></param>
        /// <param name="j"></param>
        /// <param name="epsilon"></param>
        /// <param name="angularDifference">The angle between the 2 lines.<para>-180 ≤ θ ≤ 180</para></param>
        /// <param name="normalisedOverlap">Overlap of segment i onto j. Positive value if overlapped, negative value if nonoverlapped.<para>[-1, 1]?</para></param>
        /// <param name="perpendicularDistance">Signed perpendicular distance.</param>
        /// <returns>Return true if overlapped, false if nonoverlapped.</returns>
        public static bool GetStructuralBlockingParameters(PdfLine i, PdfLine j, double epsilon,
            out double angularDifference, out double normalisedOverlap, out double perpendicularDistance)
        {
            if (AlmostEquals(i, j, epsilon))
            {
                angularDifference = 0;
                normalisedOverlap = 1;
                perpendicularDistance = 0;
                return true;
            }

            double dXi = i.Point2.X - i.Point1.X;
            double dYi = i.Point2.Y - i.Point1.Y;
            double dXj = j.Point2.X - j.Point1.X;
            double dYj = j.Point2.Y - j.Point1.Y;

            angularDifference = Distances.BoundAngle180((Math.Atan2(dYj, dXj) - Math.Atan2(dYi, dXi)) * 180 / Math.PI);

            PdfPoint? Aj = GetTranslatedPoint(i.Point1.X, i.Point1.Y, j.Point1.X, j.Point1.Y, dXi, dYi, dXj, dYj, epsilon);
            PdfPoint? Bj = GetTranslatedPoint(i.Point2.X, i.Point2.Y, j.Point2.X, j.Point2.Y, dXi, dYi, dXj, dYj, epsilon);

            if (!Aj.HasValue || !Bj.HasValue)
            {
                // Might happen because lines are perpendicular
                // or have too small lengths
                normalisedOverlap = double.NaN;
                perpendicularDistance = double.NaN;
                return false;
            }

            // Get middle points

#if NET6_0_OR_GREATER
            Span<PdfPoint> ps = stackalloc PdfPoint[] { j.Point1, j.Point2, Aj.Value, Bj.Value };

            if (dXj != 0)
            {
                ps.Sort(PdfPointXYComparer.Instance);
            }
            else if (dYj != 0)
            {
                ps.Sort(PdfPointYComparer.Instance);
            }
#else
            PdfPoint[] ps = [j.Point1, j.Point2, Aj.Value, Bj.Value];

            if (dXj != 0)
            {
                Array.Sort(ps, PdfPointXYComparer.Instance);
            }
            else if (dYj != 0)
            {
                Array.Sort(ps, PdfPointYComparer.Instance);
            }
#endif

            PdfPoint Cj = ps[1];
            PdfPoint Dj = ps[2];

            // Cj and Dj should be contained within both j and [Aj,Bj] if overlapped
            bool overlap = !(!PointInLine(j.Point1, j.Point2, Cj) || !PointInLine(j.Point1, j.Point2, Dj) ||
                             !PointInLine(Aj.Value, Bj.Value, Cj) || !PointInLine(Aj.Value, Bj.Value, Dj));

            double pj = Distances.Euclidean(Cj, Dj);

            normalisedOverlap = (overlap ? pj : -pj) / j.Length;

            double xMj = (Cj.X + Dj.X) / 2.0;
            double yMj = (Cj.Y + Dj.Y) / 2.0;

            if (!dXi.AlmostEqualsToZero(epsilon) && !dYi.AlmostEqualsToZero(epsilon))
            {
                perpendicularDistance = ((xMj - i.Point1.X) - (yMj - i.Point1.Y) * dXi / dYi) / Math.Sqrt(dXi * dXi / (dYi * dYi) + 1);
            }
            else if (dXi.AlmostEqualsToZero(epsilon))
            {
                perpendicularDistance = xMj - i.Point1.X;
            }
            else
            {
                perpendicularDistance = yMj - i.Point1.Y;
            }

            return overlap;
        }

        private static PdfPoint? GetTranslatedPoint(double xPi, double yPi, double xPj, double yPj, double dXi, double dYi, double dXj, double dYj, double epsilon)
        {
            double dYidYj = dYi * dYj;
            double dXidXj = dXi * dXj;
            double denominator = dYidYj + dXidXj;

            if (denominator.AlmostEqualsToZero(epsilon))
            {
                // The denominator is 0 when translating points, meaning the lines are perpendicular.
                return null;
            }

            double xAj;
            double yAj;

            if (!dXj.AlmostEqualsToZero(epsilon)) // dXj != 0
            {
                xAj = (xPi * dXidXj + xPj * dYidYj + dXj * dYi * (yPi - yPj)) / denominator;
                yAj = dYj / dXj * (xAj - xPj) + yPj;
            }
            else // If dXj = 0, then yAj is calculated first, and xAj is calculated from that.
            {
                yAj = (yPi * dYidYj + yPj * dXidXj + dYj * dXi * (xPi - xPj)) / denominator;
                xAj = xPj;
            }

            return new PdfPoint(xAj, yAj);
        }

        /// <summary>
        /// Helper function to check if the point belongs to the line./>
        /// </summary>
        /// <param name="pl1">Line's first point.</param>
        /// <param name="pl2">Line's second point.</param>
        /// <param name="point">The point to check.</param>
        private static bool PointInLine(PdfPoint pl1, PdfPoint pl2, PdfPoint point)
        {
            // /!\ Assuming the points are aligned (be careful)
            double ax = point.X - pl1.X;
            double ay = point.Y - pl1.Y;
            double bx = pl2.X - pl1.X;
            double by = pl2.Y - pl1.Y;

            double dotProd1 = ax * bx + ay * by;
            return dotProd1 >= 0 && dotProd1 <= (bx * bx + by * by);
        }

        /// <summary>
        /// Helper function to check if 2 lines are equal.
        /// </summary>
        /// <param name="line1"></param>
        /// <param name="line2"></param>
        /// <param name="epsilon"></param>
        private static bool AlmostEquals(PdfLine line1, PdfLine line2, double epsilon)
        {
            return (line1.Point1.X - line2.Point1.X).AlmostEqualsToZero(epsilon) &&
                   (line1.Point1.Y - line2.Point1.Y).AlmostEqualsToZero(epsilon) &&
                   (line1.Point2.X - line2.Point2.X).AlmostEqualsToZero(epsilon) &&
                   (line1.Point2.Y - line2.Point2.Y).AlmostEqualsToZero(epsilon);
        }

        /// <summary>
        /// Helper function to compute the between line angle between the pivot's
        /// and the candidate's centroid points, taking in account the pivot's rotation.
        /// <para>0 ≤ θ ≤ 180.</para>
        /// </summary>
        private static double AngleBL(Word pivot, Word candidate)
        {
            var angle = Distances.BoundAngle180(Distances.Angle(pivot.BoundingBox.Centroid, candidate.BoundingBox.Centroid) - pivot.BoundingBox.Rotation);

            // Angle is kept within [0, 180] for the check
            if (angle < 0)
            {
                angle += 180;
            }

            return angle;
        }
        #endregion

        /// <summary>
        /// The bounds for the angle between two words for them to have a certain type of relationship.
        /// </summary>
        public readonly struct AngleBounds
        {
            /// <summary>
            /// The lower bound in degrees.
            /// </summary>
            public double Lower { get; }

            /// <summary>
            /// The upper bound in degrees.
            /// </summary>
            public double Upper { get; }

            /// <summary>
            /// Create a new <see cref="AngleBounds"/>.
            /// </summary>
            public AngleBounds(double lowerBound, double upperBound)
            {
                if (lowerBound >= upperBound)
                {
                    throw new ArgumentException("The lower bound should be smaller than the upper bound.");
                }

                Lower = lowerBound;
                Upper = upperBound;
            }

            /// <summary>
            /// Whether the bounds contain the angle.
            /// </summary>
            public bool Contains(double angle)
            {
                return angle >= Lower && angle <= Upper;
            }
        }

        /// <summary>
        /// Docstrum bounding boxes page segmenter options.
        /// </summary>
        public class DocstrumBoundingBoxesOptions : IPageSegmenterOptions
        {
            /// <summary>
            /// <inheritdoc/>
            /// Default value is -1.
            /// </summary>
            public int MaxDegreeOfParallelism { get; set; } = -1;

            /// <summary>
            /// <inheritdoc/>
            /// <para>Default value is ' ' (space).</para>
            /// </summary>
            public string WordSeparator { get; set; } = " ";

            /// <summary>
            /// <inheritdoc/>
            /// <para>Default value is '\n' (new line).</para>
            /// </summary>
            public string LineSeparator { get; set; } = "\n";

            /// <summary>
            /// Precision when testing equalities.
            /// <para>Default value is 1e-3.</para>
            /// </summary>
            public double Epsilon { get; set; } = 1e-3;

            /// <summary>
            /// Angle bounds for words to be considered as neighbours on the same line.
            /// <para>Default value is -30 ≤ θ ≤ 30.</para>
            /// </summary>
            public AngleBounds WithinLineBounds { get; set; } = new AngleBounds(-30, 30);

            /// <summary>
            /// Multiplier that gives the maximum euclidean distance between
            /// words for building lines. Maximum distance will be this number times the within-line
            /// distance found by the analysis.
            /// <para>Default value is 3.</para>
            /// </summary>
            public double WithinLineMultiplier { get; set; } = 3.0;

            /// <summary>
            /// The bin size used when building the within-line distances distribution.
            /// <para>Default value is 10.</para>
            /// </summary>
            public int WithinLineBinSize { get; set; } = 10;

            /// <summary>
            /// Angle bounds for words to be considered as neighbours on separate lines.
            /// <para>Default value is 45 ≤ θ ≤ 135.</para>
            /// </summary>
            public AngleBounds BetweenLineBounds { get; set; } = new AngleBounds(45, 135);

            /// <summary>
            /// Multiplier that gives the maximum perpendicular distance between
            /// text lines for blocking. Maximum distance will be this number times the between-line
            /// distance found by the analysis.
            /// <para>Default value is 1.3.</para>
            /// </summary>
            public double BetweenLineMultiplier { get; set; } = 1.3;

            /// <summary>
            /// The bin size used when building the between-line distances distribution.
            /// <para>Default value is 10.</para>
            /// </summary>
            public int BetweenLineBinSize { get; set; } = 10;

            /// <summary>
            /// The angular difference bounds between two lines to be considered in the same block.
            /// This defines if two lines are parallel enough.
            /// <para>Default value is -30 ≤ θ ≤ 30.</para>
            /// </summary>
            public AngleBounds AngularDifferenceBounds { get; set; } = new AngleBounds(-30, 30);
        }
    }
}