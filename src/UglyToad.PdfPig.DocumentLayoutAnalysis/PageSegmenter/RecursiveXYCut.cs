namespace UglyToad.PdfPig.DocumentLayoutAnalysis.PageSegmenter
{
    using Content;
    using Core;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UglyToad.PdfPig.Geometry;

    /// <summary>
    /// The recursive X-Y cut is a top-down page segmentation technique that decomposes a document 
    /// recursively into a set of rectangular blocks. This implementation leverages bounding boxes.
    /// https://en.wikipedia.org/wiki/Recursive_X-Y_cut
    /// <para>See 'Recursive X-Y Cut using Bounding Boxes of Connected Components' by Jaekyu Ha, Robert M.Haralick and Ihsin T. Phillips</para>
    /// </summary>
    public class RecursiveXYCut : IPageSegmenter
    {
        /// <summary>
        /// Create an instance of Recursive X-Y Cut page segmenter, <see cref="RecursiveXYCut"/>.
        /// </summary>
        public static RecursiveXYCut Instance { get; } = new RecursiveXYCut();

        /// <summary>
        /// Get the blocks.
        /// <para>Uses 'minimumWidth' = 0, 'dominantFontWidthFunc' = Mode(Width), 'dominantFontHeightFunc' = 1.5 x Mode(Height)</para>
        /// </summary>
        /// <param name="pageWords">The words in the page.</param>
        /// <returns></returns>
        public IReadOnlyList<TextBlock> GetBlocks(IEnumerable<Word> pageWords)
        {
            return GetBlocks(pageWords, 0);
        }

        /// <summary>
        /// Get the blocks.
        /// <para>Uses 'dominantFontWidthFunc' = Mode(Width), 'dominantFontHeightFunc' = 1.5 x Mode(Height)</para>
        /// </summary>
        /// <param name="pageWords">The words in the page.</param>
        /// <param name="minimumWidth">The minimum width for a block.</param>
        public IReadOnlyList<TextBlock> GetBlocks(IEnumerable<Word> pageWords, double minimumWidth)
        {
            return GetBlocks(pageWords, minimumWidth, k => Math.Round(k.Mode(), 3), k => Math.Round(k.Mode() * 1.5, 3));
        }

        /// <summary>
        /// Get the blocks.
        /// </summary>
        /// <param name="pageWords">The words in the page.</param>
        /// <param name="minimumWidth">The minimum width for a block.</param>
        /// <param name="dominantFontWidth">The dominant font width.</param>
        /// <param name="dominantFontHeight">The dominant font height.</param>
        public IReadOnlyList<TextBlock> GetBlocks(IEnumerable<Word> pageWords, double minimumWidth,
            double dominantFontWidth, double dominantFontHeight)
        {
            return GetBlocks(pageWords, minimumWidth, k => dominantFontWidth, k => dominantFontHeight);
        }

        /// <summary>
        /// Get the blocks.
        /// </summary>
        /// <param name="pageWords">The words in the page.</param>
        /// <param name="minimumWidth">The minimum width for a block.</param>
        /// <param name="dominantFontWidthFunc">The function that determines the dominant font width.</param>
        /// <param name="dominantFontHeightFunc">The function that determines the dominant font height.</param>
        public IReadOnlyList<TextBlock> GetBlocks(IEnumerable<Word> pageWords, double minimumWidth,
            Func<IEnumerable<double>, double> dominantFontWidthFunc,
            Func<IEnumerable<double>, double> dominantFontHeightFunc)
        {
            if (pageWords.Count() == 0) return EmptyArray<TextBlock>.Instance;

            XYLeaf root = new XYLeaf(pageWords); // Create a root node.
            XYNode node = VerticalCut(root, minimumWidth, dominantFontWidthFunc, dominantFontHeightFunc);

            if (node.IsLeaf)
            {
                return new List<TextBlock> { new TextBlock((node as XYLeaf).GetLines()) };
            }
            else
            {
                var leafs = node.GetLeafs();

                if (leafs.Count > 0)
                {
                    return leafs.Select(l => new TextBlock(l.GetLines())).ToList();
                }
            }

            return new List<TextBlock>();
        }

        private XYNode VerticalCut(XYLeaf leaf, double minimumWidth,
            Func<IEnumerable<double>, double> dominantFontWidthFunc,
            Func<IEnumerable<double>, double> dominantFontHeightFunc, int level = 0)
        {
            // Order words left to right
            var words = leaf.Words.Where(w => !string.IsNullOrWhiteSpace(w.Text)).OrderBy(w => w.BoundingBox.Normalise().Left).ToArray();

            if (!words.Any())
            {
                return new XYNode(null);
            }

            // Create new leaf with non-whitespace words.
            leaf = new XYLeaf(words);
            
            if (leaf.CountWords() <= 1 || leaf.BoundingBox.Width <= minimumWidth)
            {
                // We stop cutting if 
                // - only one word remains
                // - width is too small
                return leaf;
            }

            // Determine dominant font width
            double dominantFontWidth = dominantFontWidthFunc(words.SelectMany(x => x.Letters)
                .Select(x => x.GlyphRectangle.Normalise().Width));

            List<Projection> projectionProfile = new List<Projection>();

            var firstWordBound = words[0].BoundingBox.Normalise();
            Projection currentProjection = new Projection(firstWordBound.Left, firstWordBound.Right);
            int wordsCount = words.Count();

            for (int i = 1; i < wordsCount; i++)
            {
                var currentWordBound = words[i].BoundingBox.Normalise();

                if (currentProjection.Contains(currentWordBound.Left) || currentProjection.Contains(currentWordBound.Right))
                {
                    // It is overlapping 
                    if (currentWordBound.Left >= currentProjection.LowerBound
                        && currentWordBound.Left <= currentProjection.UpperBound
                        && currentWordBound.Right > currentProjection.UpperBound)
                    {
                        // |____|
                        //    |____|
                        // |_______|    <- updated
                        currentProjection.UpperBound = currentWordBound.Right;
                    }

                    // We ignore the following cases:
                    //    |____|
                    // |____|          (not possible because of OrderBy)
                    // 
                    //    |____|
                    //|___________|    (not possible because of OrderBy)
                    //
                    //  |____|
                    //   |_|
                }
                else
                {
                    // No overlap
                    if (currentWordBound.Left - currentProjection.UpperBound <= dominantFontWidth)
                    {
                        // If gap too small -> don't cut
                        // |____| |____|
                        currentProjection.UpperBound = currentWordBound.Right;
                    }
                    else if (currentProjection.UpperBound - currentProjection.LowerBound < minimumWidth)
                    {
                        // Still too small
                        currentProjection.UpperBound = currentWordBound.Right;
                    }
                    else
                    {
                        // If gap big enough -> cut!
                        // |____|   |   |____|
                        if (i != wordsCount - 1) // Will always add the last one after
                        {
                            projectionProfile.Add(currentProjection);
                            currentProjection = new Projection(currentWordBound.Left, currentWordBound.Right);
                        }
                    }
                }
                if (i == wordsCount - 1) projectionProfile.Add(currentProjection);
            }
            
            var newLeafsEnums = projectionProfile.Select(p => leaf.Words.Where(w =>
            {
                // Get words that are contained in each projection profiles
                var normalisedBB = w.BoundingBox.Normalise();
                return normalisedBB.Left >= p.LowerBound && normalisedBB.Right <= p.UpperBound;
            }));

            var newLeafs = newLeafsEnums.Where(e => e.Count() > 0).Select(e => new XYLeaf(e));
            var newNodes = newLeafs.Select(l => HorizontalCut(l, minimumWidth,
                dominantFontWidthFunc, dominantFontHeightFunc, level)).ToList();

            var lost = leaf.Words.Except(newLeafsEnums.SelectMany(x => x)).Where(x => !string.IsNullOrWhiteSpace(x.Text)).ToList();
            if (lost.Count > 0)
            {
                newNodes.AddRange(lost.Select(w => new XYLeaf(w)));
            }

            return new XYNode(newNodes);
        }

        private XYNode HorizontalCut(XYLeaf leaf, double minimumWidth,
            Func<IEnumerable<double>, double> dominantFontWidthFunc,
            Func<IEnumerable<double>, double> dominantFontHeightFunc, int level = 0)
        {
            // Order words bottom to top
            var words = leaf.Words.Where(w => !string.IsNullOrWhiteSpace(w.Text)).OrderBy(w => w.BoundingBox.Normalise().Bottom).ToArray();

            if (!words.Any())
            {
                return new XYNode(null);
            }

            // Create new leaf with non-whitespace words.
            leaf = new XYLeaf(words);

            if (leaf.CountWords() <= 1)
            {
                // We stop cutting if 
                // - only one word remains
                return leaf;
            }

            // Determine dominant font height
            double dominantFontHeight = dominantFontHeightFunc(words.SelectMany(x => x.Letters)
                .Select(x => x.GlyphRectangle.Normalise().Height));

            List<Projection> projectionProfile = new List<Projection>();

            var firstWordBound = words[0].BoundingBox.Normalise();
            Projection currentProjection = new Projection(firstWordBound.Bottom, firstWordBound.Top);
            int wordsCount = words.Count();

            for (int i = 1; i < wordsCount; i++)
            {
                var currentWordBound = words[i].BoundingBox.Normalise();

                if (currentProjection.Contains(currentWordBound.Bottom) || currentProjection.Contains(currentWordBound.Top))
                {
                    // It is overlapping 
                    if (currentWordBound.Bottom >= currentProjection.LowerBound
                        && currentWordBound.Bottom <= currentProjection.UpperBound
                        && currentWordBound.Top > currentProjection.UpperBound)
                    {
                        currentProjection.UpperBound = currentWordBound.Top;
                    }
                }
                else
                {
                    // No overlap
                    if (currentWordBound.Bottom - currentProjection.UpperBound <= dominantFontHeight)
                    {
                        // If gap too small -> don't cut
                        // |____| |____|
                        currentProjection.UpperBound = currentWordBound.Top;
                    }
                    else
                    {
                        // If gap big enough -> cut!
                        // |____|   |   |____|
                        if (i != wordsCount - 1) // Will always add the last one after
                        {
                            projectionProfile.Add(currentProjection);
                            currentProjection = new Projection(currentWordBound.Bottom, currentWordBound.Top);
                        }
                    }
                }
                if (i == wordsCount - 1) projectionProfile.Add(currentProjection);
            }

            if (projectionProfile.Count == 1)
            {
                if (level >= 1)
                {
                    return leaf;
                }
                else
                {
                    level++;
                }
            }

            var newLeafsEnums = projectionProfile.Select(p => leaf.Words.Where(w =>
            {
                // Get words that are contained in each projection profiles
                var normalisedBB = w.BoundingBox.Normalise();
                return normalisedBB.Bottom >= p.LowerBound && normalisedBB.Top <= p.UpperBound;
            }));

            var newLeafs = newLeafsEnums.Where(e => e.Count() > 0).Select(e => new XYLeaf(e));
            var newNodes = newLeafs.Select(l => VerticalCut(l, minimumWidth,
                dominantFontWidthFunc, dominantFontHeightFunc, level)).ToList();

            var lost = leaf.Words.Except(newLeafsEnums.SelectMany(x => x)).Where(x => !string.IsNullOrWhiteSpace(x.Text)).ToList();
            if (lost.Count > 0)
            {
                newNodes.AddRange(lost.Select(w => new XYLeaf(w)));
            }
            return new XYNode(newNodes);
        }

        private struct Projection
        {
            public double UpperBound { get; set; }
            public double LowerBound { get; set; }

            public Projection(double lowerBound, double upperBound)
            {
                UpperBound = upperBound;
                LowerBound = lowerBound;
            }

            /// <summary>
            /// Returns true if the value is greater or equal to the lower bound and smaller or equal to the upper bound.
            /// </summary>
            /// <param name="value">The value to test.</param>
            public bool Contains(double value)
            {
                return value >= LowerBound && value <= UpperBound;
            }
        }
    }
}
