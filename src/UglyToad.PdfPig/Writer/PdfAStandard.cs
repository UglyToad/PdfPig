namespace UglyToad.PdfPig.Writer
{
    /// <summary>
    /// The standard of PDF/A compliance for generated documents.
    /// </summary>
    public enum PdfAStandard
    {
        /// <summary>
        /// No PDF/A compliance.
        /// </summary>
        None = 0,
        /// <summary>
        /// Compliance with PDF/A1-B. Level B (basic) conformance are standards necessary for the reliable reproduction of a document's visual appearance.
        /// </summary>
        A1B = 1
    }
}