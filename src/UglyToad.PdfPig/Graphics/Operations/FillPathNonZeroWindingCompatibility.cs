namespace UglyToad.PdfPig.Graphics.Operations
{
    using System.IO;

    /// <inheritdoc />
    /// <summary>
    /// Equivalent to <see cref="FillPathNonZeroWinding"/> included only for compatibility. 
    /// Although PDF consumer applications must be able to accept this operator, PDF producer applications should use <see cref="FillPathNonZeroWinding"/> instead.
    /// </summary>
    public class FillPathNonZeroWindingCompatibility : IGraphicsStateOperation
    {
        /// <summary>
        /// The symbol for this operation in a stream.
        /// </summary>
        public const string Symbol = "F";

        /// <summary>
        /// The instance of the <see cref="FillPathEvenOddRuleAndStroke"/> operation.
        /// </summary>
        public static readonly FillPathNonZeroWindingCompatibility Value = new FillPathNonZeroWindingCompatibility();

        /// <inheritdoc />
        public string Operator => Symbol;

        private FillPathNonZeroWindingCompatibility()
        {
        }

        /// <inheritdoc />
        public void Run(IOperationContext operationContext)
        {
        }

        /// <inheritdoc />
        public void Write(Stream stream)
        {
            stream.WriteText(Symbol);
            stream.WriteNewLine();
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return Symbol;
        }
    }
}