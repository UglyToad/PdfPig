namespace UglyToad.PdfPig.Graphics.Operations.PathPainting
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
        /// The instance of the <see cref="FillPathNonZeroWindingCompatibility"/> operation.
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
            operationContext.FillPath(PdfPig.Core.FillingRule.NonZeroWinding, false);
        }

        /// <inheritdoc />
        public void Write(Stream stream)
        {
            // Although PDF reader applications shall be able to accept this operator, PDF writer applications should use f instead.
            stream.WriteText(FillPathNonZeroWinding.Symbol); 
            stream.WriteNewLine();
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return Symbol;
        }
    }
}