namespace UglyToad.PdfPig.Graphics.Operations
{
    using System.IO;

    /// <inheritdoc />
    /// <summary>
    /// Set the gray level for stroking operations.
    /// </summary>
    public class SetStrokeColorDeviceGray : IGraphicsStateOperation
    {
        /// <summary>
        /// The symbol for this operation in a stream.
        /// </summary>
        public const string Symbol = "G";

        /// <inheritdoc />
        public string Operator => Symbol;

        /// <summary>
        /// The gray level between 0 (black) and 1 (white).
        /// </summary>
        public double Gray { get; }

        /// <summary>
        /// Create a new <see cref="SetStrokeColorDeviceGray"/>.
        /// </summary>
        /// <param name="gray">The gray level.</param>
        public SetStrokeColorDeviceGray(double gray)
        {
            Gray = gray;
        }

        /// <inheritdoc />
        public void Run(IOperationContext operationContext)
        {
            operationContext.GetCurrentState().ColorSpaceContext.SetStrokingColorGray(Gray);
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
