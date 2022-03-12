namespace UglyToad.PdfPig.DocumentLayoutAnalysis
{
    /// <summary>
    /// Interface that stores options that configure the operation of methods of the document layout analysis algorithm.
    /// </summary>
    public interface IDlaOptions
    {
        /// <summary>
        /// Gets or sets the maximum number of concurrent tasks enabled.
        /// <para>A positive property value limits the number of concurrent operations to the set value.
        /// If it is -1, there is no limit on the number of concurrently running operations.</para>
        /// </summary>
        int MaxDegreeOfParallelism { get; set; }
    }
}
