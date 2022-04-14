namespace UglyToad.PdfPig.DocumentLayoutAnalysis.PageSegmenter
{
    /// <summary>
    /// Page segmenter options interface.
    /// </summary>
    public interface IPageSegmenterOptions : IDlaOptions
    {
        /// <summary>
        /// Separator used between words when building lines.
        /// </summary>
        string WordSeparator { get; set; }

        /// <summary>
        /// Separator used between lines when building paragraphs.
        /// </summary>
        string LineSeparator { get; set; }
    }
}
