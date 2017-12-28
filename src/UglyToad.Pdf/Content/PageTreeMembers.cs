namespace UglyToad.Pdf.Content
{
    using System;

    /// <summary>
    /// Contains the values inherited from the Page Tree for this page.
    /// </summary>
    public class PageTreeMembers
    {
        public MediaBox GetMediaBox()
        {
            // TODO: tree inheritance
            throw new NotImplementedException("Track inherited members");
        }

        public CropBox GetCropBox()
        {
            return null;
        }
    }
}