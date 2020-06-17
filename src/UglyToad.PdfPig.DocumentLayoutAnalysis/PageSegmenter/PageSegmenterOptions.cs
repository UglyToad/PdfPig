namespace UglyToad.PdfPig.DocumentLayoutAnalysis.PageSegmenter
{
    /// <summary>
    /// Abstract page segmenter options.
    /// </summary>
    public abstract class PageSegmenterOptions : DlaOptions
    {
        /// <summary>
        /// Separator used between words when building lines.
        /// <para>Default value is ' ' (space).</para>
        /// </summary>
        public string WordSeparator { get; set; } = " ";

        /// <summary>
        /// Separator used between lines when building paragraphs.
        /// <para>Default value is '\n' (new line).</para>
        /// </summary>
        public string LineSeparator { get; set; } = "\n";
    }
}
