namespace UglyToad.PdfPig.Graphics.Operations
{
    using System.IO;

    /// <inheritdoc />
    /// <summary>
    /// Fill and then stroke the path, using the even-odd rule to determine the region to fill.
    /// </summary>
    internal class FillPathEvenOddRuleAndStroke : IGraphicsStateOperation
    {
        /// <summary>
        /// The symbol for this operation in a stream.
        /// </summary>
        public const string Symbol = "B*";

        /// <summary>
        /// The instance of the <see cref="FillPathEvenOddRuleAndStroke"/> operation.
        /// </summary>
        public static readonly FillPathEvenOddRuleAndStroke Value = new FillPathEvenOddRuleAndStroke();

        /// <inheritdoc />
        public string Operator => Symbol;

        private FillPathEvenOddRuleAndStroke()
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