namespace UglyToad.PdfPig.Graphics.Operations
{
    using System.IO;
    
    /// <inheritdoc />
    /// <summary>
    /// Close, fill, and then stroke the path, using the even-odd rule to determine the region to fill.
    /// </summary>
    public class CloseFillPathEvenOddRuleAndStroke : IGraphicsStateOperation
    {
        /// <summary>
        /// The symbol for this operation in a stream.
        /// </summary>
        public const string Symbol = "b*";

        /// <summary>
        /// The instance of the <see cref="CloseFillPathEvenOddRuleAndStroke"/> operation.
        /// </summary>
        public static readonly CloseFillPathEvenOddRuleAndStroke Value = new CloseFillPathEvenOddRuleAndStroke();

        /// <inheritdoc />
        public string Operator => Symbol;

        private CloseFillPathEvenOddRuleAndStroke()
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