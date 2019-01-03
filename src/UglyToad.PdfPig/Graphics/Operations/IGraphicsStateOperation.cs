namespace UglyToad.PdfPig.Graphics.Operations
{
    using System.IO;

    /// <summary>
    /// An operation with associated data from a content stream.
    /// </summary>
    public interface IGraphicsStateOperation
    {
        /// <summary>
        /// The symbol representing the operator in the content stream.
        /// </summary>
        string Operator { get; }

        /// <summary>
        /// Writes the operator and any operands as valid PDF content to the stream.
        /// </summary>
        /// <param name="stream">The stream to write to.</param>
        void Write(Stream stream);

        /// <summary>
        /// Applies the operation to the current context with the provided resources.
        /// </summary>
        /// <param name="operationContext"></param>
        void Run(IOperationContext operationContext);
    }
}