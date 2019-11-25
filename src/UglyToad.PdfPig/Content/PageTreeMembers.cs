namespace UglyToad.PdfPig.Content
{
    using System.Collections.Generic;
    using Tokens;

    /// <summary>
    /// Contains the values inherited from the Page Tree for this page.
    /// </summary>
    internal class PageTreeMembers
    {
        public CropBox GetCropBox()
        {
            return null;
        }

        public MediaBox MediaBox { get; set; }

        public int Rotation { get; set; }

        public Queue<DictionaryToken> ParentResources { get; } = new Queue<DictionaryToken>();
    }
}