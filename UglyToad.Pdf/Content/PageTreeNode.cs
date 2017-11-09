namespace UglyToad.Pdf.Content
{
    using System;
    using Cos;

    public class PageTreeNode
    {
        private readonly CosDictionary dictionary;

        public int Count { get; set; }

        public PageTreeNode Parent { get; }

        internal PageTreeNode(CosDictionary pageTreeNode, PageTreeNode parent)
        {
            dictionary = pageTreeNode ?? throw new ArgumentNullException(nameof(pageTreeNode));

            Parent = parent;

        }
    }
}
