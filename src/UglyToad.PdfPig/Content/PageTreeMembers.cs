namespace UglyToad.PdfPig.Content
{
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
    }
}