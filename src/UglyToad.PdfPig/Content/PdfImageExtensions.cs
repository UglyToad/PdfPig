namespace UglyToad.PdfPig.Content
{
    /// <summary>
    /// Pdf image extensions.
    /// </summary>
    public static class PdfImageExtensions
    {
        /// <summary>
        /// <c>true</c> if the image colors needs to be reversed based on the Decode array and color space. <c>false</c> otherwise.
        /// </summary>
        public static bool NeedsReverseDecode(this IPdfImage pdfImage)
        {
            if (pdfImage.ColorSpaceDetails?.IsStencil == true)
            {
                // Stencil color space already takes care of reversing.
                return false;
            }

            return pdfImage.Decode.Count >= 2 && pdfImage.Decode[0] == 1 && pdfImage.Decode[1] == 0;
        }
    }
}
