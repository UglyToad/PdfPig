namespace UglyToad.PdfPig.Filters.Jbig2
{
    using System;
    using static HuffmanTable;

    /// <summary>
    /// Represents a value node in a Huffman tree. It is a leaf of a tree.
    /// </summary>
    internal class ValueNode : Node
    {
        private readonly int rangeLength;
        private readonly int rangeLow;
        private readonly bool isLowerRange;

        public ValueNode(Code c)
        {
            rangeLength = c.RangeLength;
            rangeLow = c.RangeLow;
            isLowerRange = c.IsLowerRange;
        }

        public override sealed long Decode(IImageInputStream iis)
        {
            if (isLowerRange)
            {
                // B.4 4)
                return (rangeLow - iis.ReadBits(rangeLength));
            }
            else
            {
                // B.4 5)
                return rangeLow + iis.ReadBits(rangeLength);
            }
        }

        internal static string bitPattern(int v, int len)
        {
            var result = new char[len];
            for (int i = 1; i <= len; i++)
                result[i - 1] = (v >> (len - i) & 1) != 0 ? '1' : '0';

            return new String(result);
        }
    }
}
