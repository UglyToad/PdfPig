using System.IO;
namespace UglyToad.PdfPig
{
    using UglyToad.PdfPig.Content;

    /// <summary>
    /// IDrawingSystem
    /// </summary>
    public interface IDrawingProcessor
    {
        /// <summary>
        /// DrawPage
        /// </summary>
        /// <param name="page"></param>
        /// <param name="scale"></param>
        MemoryStream DrawPage(Page page, double scale);
    }
}
