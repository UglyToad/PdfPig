namespace UglyToad.PdfPig.Fonts.TrueType.Tables.Kerning
{
    using System;

    /// <summary>
    /// The type of kerning covered by this table.
    /// </summary>
    [Flags]
    internal enum KernCoverage
    {
        /// <summary>
        /// The table is horizontal kerning data.
        /// </summary>
        Horizontal = 1,
        /// <summary>
        /// The table has minimum values rather than kerning values.
        /// </summary>
        Minimum = 1 << 1,
        /// <summary>
        /// Kerning is perpendicular to the flow of text.
        /// If text is horizontal kerning will be in the up/down direction.
        /// </summary>
        CrossStream = 1 << 2,
        /// <summary>
        /// The value in this sub table should replace the currently accumulated value.
        /// </summary>
        Override = 1 << 3
    }
}
