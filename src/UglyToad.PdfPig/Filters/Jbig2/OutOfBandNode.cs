namespace UglyToad.PdfPig.Filters.Jbig2
{
    using static HuffmanTable;

    /// <summary>
    /// Represents a out of band node in a Huffman tree.
    /// </summary>
    internal class OutOfBandNode : Node
    {
        public OutOfBandNode(Code c)
        {
        }

        public override sealed long Decode(IImageInputStream iis)
        {
            return long.MaxValue;
        }

    }

}
