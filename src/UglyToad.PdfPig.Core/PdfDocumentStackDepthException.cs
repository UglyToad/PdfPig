namespace UglyToad.PdfPig.Core
{
    using System;

    /// <summary>
    /// Represents an exception that is thrown when the stack depth of a PDF document exceeds the allowed limit.
    /// </summary>
    public sealed class PdfDocumentStackDepthException : Exception
    {
        /// <inheritdoc />
        internal PdfDocumentStackDepthException(int maxStackDepth)
            : base($"Exceeded maximum nesting depth of {maxStackDepth}.")
        { }
    }
}
