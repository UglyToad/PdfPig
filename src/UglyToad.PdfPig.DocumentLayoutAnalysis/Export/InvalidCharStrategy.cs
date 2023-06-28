namespace UglyToad.PdfPig.DocumentLayoutAnalysis.Export
{
    /// <summary>
    /// How to handle invalid characters.
    /// </summary>
    public enum InvalidCharStrategy : byte
    {
        /// <summary>
        /// Custom strategy.
        /// </summary>
        Custom = 0,

        /// <summary>
        /// Do not check invalid character.
        /// </summary>
        DoNotCheck = 1,

        /// <summary>
        /// Remove invalid character.
        /// </summary>
        Remove = 2,

        /// <summary>
        /// Convert invalid character to hexadecimal representation.
        /// </summary>
        ConvertToHexadecimal = 3
    }
}
