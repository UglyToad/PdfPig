namespace UglyToad.PdfPig.Writer
{
    /// <summary>
    /// The relationship of an embedded file to the document, which PDF/A-3 requires for each embedded file.
    /// </summary>
    public enum AssociatedFileRelationship
    {
        /// <summary>
        /// The relationship is not known or cannot be described by the other values.
        /// </summary>
        Unspecified = 0,
        /// <summary>
        /// The file is the original source material of the document.
        /// </summary>
        Source = 1,
        /// <summary>
        /// The file holds the data from which a visual presentation in the document is derived, such as a table or a graph.
        /// </summary>
        Data = 2,
        /// <summary>
        /// The file is an alternative representation of the content, such as the XML of an invoice.
        /// </summary>
        Alternative = 3,
        /// <summary>
        /// The file is a supplemental representation of the source or data that may be easier to process, such as MathML for an equation.
        /// </summary>
        Supplement = 4
    }
}
