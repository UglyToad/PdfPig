using System.IO;
using UglyToad.PdfPig.Content;

namespace UglyToad.PdfPig.Rendering
{
    /// <summary>
    /// Render page as an image.
    /// </summary>
    public interface IPageImageRenderer
    {
        /// <summary>
        /// Render page as an image.
        /// </summary>
        /// <param name="page">The pdf page.</param>
        /// <param name="scale">The scale to apply to the page (i.e. zoom level).</param>
        /// <param name="imageFormat">The output image format, if supported.</param>
        /// <returns>The image as a memory stream.</returns>
        MemoryStream Render(Page page, double scale, PdfRendererImageFormat imageFormat);
    }
}
