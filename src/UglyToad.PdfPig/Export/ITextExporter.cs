using UglyToad.PdfPig.Content;

namespace UglyToad.PdfPig.Export
{
    /// <summary>
    /// Exports the page's text into the desired format.
    /// </summary>
    public interface ITextExporter
    {
        /// <summary>
        /// Get the text representation.
        /// </summary>
        /// <param name="page"></param>
        /// <returns></returns>
        string Get(Page page);
    }
}
