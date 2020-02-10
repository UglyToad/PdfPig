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
            // order words left to right
            var words = leaf.Words.Where(w => !string.IsNullOrWhiteSpace(w.Text)).OrderBy(w => w.BoundingBox.Normalise().Left).ToArray();

            if (!words.Any())
            {
                return new XYNode(null);
            }
            else
            {
                //Create new leaf with non-whitespace words.
                leaf = new XYLeaf(words);
            }

            if (leaf.CountWords() <= 1 || leaf.BoundingBox.Width <= minimumWidth)
            {
                // we stop cutting if 
                // - only one word remains
                // - width is too small
                return leaf;
            }

            // determine dominantFontWidth
            double domFontWidth = dominantFontWidthFunc(words.SelectMany(x => x.Letters)
                .Select(x => Math.Abs(x.GlyphRectangle.Normalise().Width)));

            List<double[]> projectionProfile = new List<double[]>();

            var firstWordBound = words[0].BoundingBox.Normalise();
            double[] currentProj = new double[2] { firstWordBound.Left, firstWordBound.Right };
            int wordsCount = words.Count();

            for (int i = 1; i < wordsCount; i++)
            {
                var currentWordBound = words[i].BoundingBox.Normalise();

                if ((currentWordBound.Left >= currentProj[0] && currentWordBound.Left <= currentProj[1])
                    || (currentWordBound.Right >= currentProj[0] && currentWordBound.Right <= currentProj[1]))
                {
                    // it is overlapping 
                    if (currentWordBound.Left >= currentProj[0]
                        && currentWordBound.Left <= currentProj[1]
                        && currentWordBound.Right > currentProj[1])
                    {
                        // |____|
                        //    |____|
                        // |_______|    <- updated
                        currentProj[1] = currentWordBound.Right;
                    }

                    // we ignore the following cases:
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
                    // no overlap
                    if (currentWordBound.Left - currentProj[1] <= domFontWidth)
                    {
                        // if gap too small -> don't cut
                        // |____| |____|
                        currentProj[1] = currentWordBound.Right;
                    }
                    else if (currentProj[1] - currentProj[0] < minimumWidth)
                    {
                        // still too small
                        currentProj[1] = currentWordBound.Right;
                    }
                    else
                    {
                        // if gap big enough -> cut!
                        // |____|   |   |____|
                        if (i != wordsCount - 1) // will always add the last one after
                        {
                            projectionProfile.Add(currentProj);
                            currentProj = new double[2] { currentWordBound.Left, currentWordBound.Right };
                        }
                    }
                }
                if (i == wordsCount - 1) projectionProfile.Add(currentProj);
            }

            var newLeafsEnums = projectionProfile.Select(p => leaf.Words.Where(w =>
            {
                var normalisedBB = w.BoundingBox.Normalise();
                return normalisedBB.Left >= p[0] && normalisedBB.Right <= p[1];
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
            var words = leaf.Words.Where(w => !string.IsNullOrWhiteSpace(w.Text)).OrderBy(w => w.BoundingBox.Normalise().Bottom).ToArray(); // order bottom to top

            if (!words.Any())
            {
                return new XYNode(null);
            }

            //Create new leaf with non-whitespace words.
            leaf = new XYLeaf(words);

            if (leaf.CountWords() <= 1)
            {
                // we stop cutting if 
                // - only one word remains
                return leaf;
            }

            // determine dominantFontHeight
            double domFontHeight = dominantFontHeightFunc(words.SelectMany(x => x.Letters)
                .Select(x => Math.Abs(x.GlyphRectangle.Normalise().Height)));

            List<double[]> projectionProfile = new List<double[]>();

            var firstWordBound = words[0].BoundingBox.Normalise();
            double[] currentProj = new double[2] { firstWordBound.Bottom, firstWordBound.Top };
            int wordsCount = words.Count();

            for (int i = 1; i < wordsCount; i++)
            {
                var currentWordBound = words[i].BoundingBox.Normalise();

                if ((currentWordBound.Bottom >= currentProj[0] && currentWordBound.Bottom <= currentProj[1])
                    || (currentWordBound.Top >= currentProj[0] && currentWordBound.Top <= currentProj[1]))
                {
                    // it is overlapping 
                    if (currentWordBound.Bottom >= currentProj[0]
                        && currentWordBound.Bottom <= currentProj[1]
                        && currentWordBound.Top > currentProj[1])
                    {
                        currentProj[1] = currentWordBound.Top;
                    }
                }
                else
                {
                    // no overlap
                    if (currentWordBound.Bottom - currentProj[1] <= domFontHeight)
                    {
                        // if gap too small -> don't cut
                        // |____| |____|
                        currentProj[1] = currentWordBound.Top;
                    }
                    else
                    {
                        // if gap big enough -> cut!
                        // |____|   |   |____|
                        if (i != wordsCount - 1) // will always add the last one after
                        {
                            projectionProfile.Add(currentProj);
                            currentProj = new double[2] { currentWordBound.Bottom, currentWordBound.Top };
                        }
                    }
                }
                if (i == wordsCount - 1) projectionProfile.Add(currentProj);
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
                var normalisedBB = w.BoundingBox.Normalise();
                return normalisedBB.Bottom >= p[0] && normalisedBB.Top <= p[1];
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
    }
}
