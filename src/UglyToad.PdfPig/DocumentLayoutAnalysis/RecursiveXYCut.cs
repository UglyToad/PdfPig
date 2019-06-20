using System;
using System.Collections.Generic;
using System.Linq;
using UglyToad.PdfPig.Content;

namespace UglyToad.PdfPig.DocumentLayoutAnalysis
{
    /// <summary>
    /// The recursive X-Y cut is a top-down page segmentation technique that decomposes a document 
    /// recursively into a set of rectangular blocks. This implementation leverages bounding boxes.
    /// https://en.wikipedia.org/wiki/Recursive_X-Y_cut
    /// <para>See 'Recursive X-Y Cut using Bounding Boxes of Connected Components' by Jaekyu Ha, Robert M.Haralick and Ihsin T. Phillips</para>
    /// </summary>
    public class RecursiveXYCut
    {
        /// <summary>
        /// Get the blocks.
        /// </summary>
        /// <param name="pageWords">The words in the page.</param>
        /// <param name="minimumWidth">The minimum width for a block.</param>
        public static XYNode GetBlocks(IEnumerable<Word> pageWords, decimal minimumWidth = 0)
        {
            return GetBlocks(pageWords, minimumWidth, k => Math.Round(k.Mode(), 3), k => Math.Round(k.Mode() * 1.5m, 3));
        }

        /// <summary>
        /// Get the blocks.
        /// </summary>
        /// <param name="pageWords">The words in the page.</param>
        /// <param name="minimumWidth">The minimum width for a block.</param>
        /// <param name="dominantFontWidth">The dominant font width.</param>
        /// <param name="dominantFontHeight">The dominant font height.</param>
        public static XYNode GetBlocks(IEnumerable<Word> pageWords, decimal minimumWidth,
            decimal dominantFontWidth, decimal dominantFontHeight)
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
        public static XYNode GetBlocks(IEnumerable<Word> pageWords, decimal minimumWidth,
            Func<IEnumerable<decimal>, decimal> dominantFontWidthFunc,
            Func<IEnumerable<decimal>, decimal> dominantFontHeightFunc)
        {
            var root = new XYLeaf(pageWords); // Create a root node.
            return VerticalCut(root, minimumWidth, dominantFontWidthFunc, dominantFontHeightFunc);
        }

        private static XYNode VerticalCut(XYLeaf leaf, decimal minimumWidth,
            Func<IEnumerable<decimal>, decimal> dominantFontWidthFunc,
            Func<IEnumerable<decimal>, decimal> dominantFontHeightFunc, int level = 0)
        {
            if (leaf.CountWords() <= 1 || leaf.BoundingBox.Width <= minimumWidth)
            {
                // we stop cutting if 
                // - only one word remains
                // - width is too small
                return leaf;
            }

            // order words left to right
            var words = leaf.Words.Where(w => !string.IsNullOrWhiteSpace(w.Text)).OrderBy(w => w.BoundingBox.Left).ToArray();

            // determine dominantFontWidth and dominantFontHeight
            decimal domFontWidth = dominantFontWidthFunc(words.SelectMany(x => x.Letters)
                .Select(x => Math.Abs(x.GlyphRectangle.Width)));
            decimal domFontHeight = dominantFontHeightFunc(words.SelectMany(x => x.Letters)
                .Select(x => Math.Abs(x.GlyphRectangle.Height)));

            List<decimal[]> projectionProfile = new List<decimal[]>();
            decimal[] currentProj = new decimal[2] { words[0].BoundingBox.Left, words[0].BoundingBox.Right };
            int wordsCount = words.Count();
            for (int i = 1; i < wordsCount; i++)
            {
                if ((words[i].BoundingBox.Left >= currentProj[0] && words[i].BoundingBox.Left <= currentProj[1])
                    || (words[i].BoundingBox.Right >= currentProj[0] && words[i].BoundingBox.Right <= currentProj[1]))
                {
                    // it is overlapping 
                    if (words[i].BoundingBox.Left >= currentProj[0]
                        && words[i].BoundingBox.Left <= currentProj[1]
                        && words[i].BoundingBox.Right > currentProj[1])
                    {
                        // |____|
                        //    |____|
                        // |_______|    <- updated
                        currentProj[1] = words[i].BoundingBox.Right;
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
                    if (words[i].BoundingBox.Left - currentProj[1] <= domFontWidth)
                    {
                        // if gap too small -> don't cut
                        // |____| |____|
                        currentProj[1] = words[i].BoundingBox.Right;
                    }
                    else if (currentProj[1] - currentProj[0] < minimumWidth)
                    {
                        // still too small
                        currentProj[1] = words[i].BoundingBox.Right;
                    }
                    else
                    {
                        // if gap big enough -> cut!
                        // |____|   |   |____|
                        if (i != wordsCount - 1) // will always add the last one after
                        {
                            projectionProfile.Add(currentProj);
                            currentProj = new decimal[2] { words[i].BoundingBox.Left, words[i].BoundingBox.Right };
                        }
                    }
                }
                if (i == wordsCount - 1) projectionProfile.Add(currentProj);
            }

            var newLeafsEnums = projectionProfile.Select(p => leaf.Words.Where(w => w.BoundingBox.Left >= p[0] && w.BoundingBox.Right <= p[1]));
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

        private static XYNode HorizontalCut(XYLeaf leaf, decimal minimumWidth,
            Func<IEnumerable<decimal>, decimal> dominantFontWidthFunc,
            Func<IEnumerable<decimal>, decimal> dominantFontHeightFunc, int level = 0)
        {
            if (leaf.CountWords() <= 1)
            {
                // we stop cutting if 
                // - only one word remains
                return leaf;
            }

            var words = leaf.Words.Where(w => !string.IsNullOrWhiteSpace(w.Text)).OrderBy(w => w.BoundingBox.Bottom).ToArray(); // order bottom to top

            // determine dominantFontWidth and dominantFontHeight
            decimal domFontWidth = dominantFontWidthFunc(words.SelectMany(x => x.Letters)
                .Select(x => Math.Abs(x.GlyphRectangle.Width)));
            decimal domFontHeight = dominantFontHeightFunc(words.SelectMany(x => x.Letters)
                .Select(x => Math.Abs(x.GlyphRectangle.Height)));

            List<decimal[]> projectionProfile = new List<decimal[]>();
            decimal[] currentProj = new decimal[2] { words[0].BoundingBox.Bottom, words[0].BoundingBox.Top };
            int wordsCount = words.Count();
            for (int i = 1; i < wordsCount; i++)
            {
                if ((words[i].BoundingBox.Bottom >= currentProj[0] && words[i].BoundingBox.Bottom <= currentProj[1])
                    || (words[i].BoundingBox.Top >= currentProj[0] && words[i].BoundingBox.Top <= currentProj[1]))
                {
                    // it is overlapping 
                    if (words[i].BoundingBox.Bottom >= currentProj[0]
                        && words[i].BoundingBox.Bottom <= currentProj[1]
                        && words[i].BoundingBox.Top > currentProj[1])
                    {
                        currentProj[1] = words[i].BoundingBox.Top;
                    }
                }
                else
                {
                    // no overlap
                    if (words[i].BoundingBox.Bottom - currentProj[1] <= domFontHeight)
                    {
                        // if gap too small -> don't cut
                        // |____| |____|
                        currentProj[1] = words[i].BoundingBox.Top;
                    }
                    else
                    {
                        // if gap big enough -> cut!
                        // |____|   |   |____|
                        if (i != wordsCount - 1) // will always add the last one after
                        {
                            projectionProfile.Add(currentProj);
                            currentProj = new decimal[2] { words[i].BoundingBox.Bottom, words[i].BoundingBox.Top };
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

            var newLeafsEnums = projectionProfile.Select(p =>
                leaf.Words.Where(w => w.BoundingBox.Bottom >= p[0] && w.BoundingBox.Top <= p[1]));
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
