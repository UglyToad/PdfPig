namespace UglyToad.PdfPig.Images.Jpg.Parts
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    internal class HuffmanTree
    {
        byte[] qtab;
        private HuffmanTreeNode[] nodes;
        internal HuffmanTree()
        {
            nodes = new HuffmanTreeNode[65536];
            qtab = new byte[64];                           
        }
    }
}
