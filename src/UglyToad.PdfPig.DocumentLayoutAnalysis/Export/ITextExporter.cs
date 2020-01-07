namespace UglyToad.PdfPig.DocumentLayoutAnalysis.Export
{
    using Content;

    /// <summary>
    /// Exports the page's text into the desired format.
    /// </summary>
    public interface ITextExporter
    {
        /// <summary>
        /// Get the text representation of a page in a desired format.
        /// </summary>
        /// <param name="page">The page to convert to the format.</param>
        /// <returns>The <see langword="string"/> containing the page contents represented in a compatible format.</returns>
        string Get(Page page);
    }
}
