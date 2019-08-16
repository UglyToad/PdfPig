namespace UglyToad.PdfPig.Graphics.Operations
{
    using System.IO;

    /// <inheritdoc />
    /// <summary>
    /// Set the gray level for non-stroking operations.
    /// </summary>
    public class SetNonStrokeColorDeviceGray : IGraphicsStateOperation
    {
        /// <summary>
        /// The symbol for this operation in a stream.
        /// </summary>
        public const string Symbol = "g";

        /// <inheritdoc />
        public string Operator => Symbol;

        /// <summary>
        /// The gray level between 0 (black) and 1 (white).
        /// </summary>
        public decimal Gray { get; }

        /// <summary>
        /// Create a new <see cref="SetNonStrokeColorDeviceGray"/>.
        /// </summary>
        /// <param name="gray">The gray level.</param>
        public SetNonStrokeColorDeviceGray(decimal gray)
        {
            Gray = gray;
        }

        /// <inheritdoc />
        public void Run(IOperationContext operationContext)
        {
            operationContext.ColorSpaceContext.SetNonStrokingColorGray(Gray);
        }

        /// <inheritdoc />
        public void Write(Stream stream)
        {
            stream.WriteNumberText(Gray, Symbol);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{Gray} {Symbol}";
        }
    }
}