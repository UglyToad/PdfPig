namespace UglyToad.PdfPig.Filters.Jbig2
{
    using System.Collections.Generic;

    /// <summary>
    /// This class represents a fixed size huffman table.
    /// </summary>
    internal sealed class FixedSizeTable : HuffmanTable
    {
        public FixedSizeTable(List<Code> runCodeTable)
        {
            InitTree(runCodeTable);
        }
    }
}
