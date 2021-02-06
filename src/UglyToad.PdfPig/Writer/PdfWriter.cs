namespace UglyToad.PdfPig.Writer
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// Type of pdf writer to use.
    /// </summary>
    public enum PdfWriter
    {
        /// <summary>
        /// Default output writer
        /// </summary>
        Default,
        /// <summary>
        /// De-duplicates objects while writing but requires keeping in memory reference.
        /// </summary>
        ObjectInMemoryDedup
    }
}
