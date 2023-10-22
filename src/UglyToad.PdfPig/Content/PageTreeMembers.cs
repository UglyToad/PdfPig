namespace UglyToad.PdfPig.Content
{
    using System.Collections.Generic;
    using Tokens;

    /// <summary>
    /// Contains the values inherited from the Page Tree for this page.
    /// </summary>
    public sealed class PageTreeMembers
    {
        internal CropBox GetCropBox()
        {
            return null;
        }

        /// <summary>
        /// The page media box.
        /// </summary>
        public MediaBox MediaBox { get; set; }

        /// <summary>
        /// The page rotation.
        /// </summary>
        public int Rotation { get; set; }

        /// <summary>
        /// The page parent resources.
        /// </summary>
        public Queue<DictionaryToken> ParentResources { get; } = new Queue<DictionaryToken>();
    }
}