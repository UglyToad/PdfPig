namespace UglyToad.PdfPig.AcroForms
{
    using System;

    /// <summary>
    /// Specifies document level characteristics for any signature fields in the document's <see cref="AcroForm"/>.
    /// </summary>
    [Flags]
    internal enum SignatureFlags
    {
        /// <summary>
        /// The document contains at least one signature field.
        /// </summary>
        SignaturesExist = 1 << 0,
        /// <summary>
        /// The document contains signatures which may be invalidated if the file is saved
        /// in a way which alters its previous content rather than simply appending new content.
        /// </summary>
        AppendOnly = 1 << 1
    }
}