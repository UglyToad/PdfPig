namespace UglyToad.PdfPig.Graphics.Operations
{
    using System.IO;

    /// <inheritdoc />
    /// <summary>
    /// Set the stroking color space to DeviceCMYK and set the color to use for stroking operations.
    /// </summary>
    public class SetStrokeColorDeviceCmyk : IGraphicsStateOperation
    {
        /// <summary>
        /// The symbol for this operation in a stream.
        /// </summary>
        public const string Symbol = "K";
        
        /// <inheritdoc />
        public string Operator => Symbol;

        /// <summary>
        /// The cyan level between 0 and 1.
        /// </summary>
        public decimal C { get; }

        /// <summary>
        /// The magenta level between 0 and 1.
        /// </summary>
        public decimal M { get; }

        /// <summary>
        /// The yellow level between 0 and 1.
        /// </summary>
        public decimal Y { get; }

        /// <summary>
        /// The key level between 0 and 1.
        /// </summary>
        public decimal K { get; }

        /// <summary>
        /// Create a new <see cref="SetStrokeColorDeviceCmyk"/>.
        /// </summary>
        /// <param name="c">The cyan level.</param>
        /// <param name="m">The magenta level.</param>
        /// <param name="y">The yellow level.</param>
        /// <param name="k">The key level.</param>
        public SetStrokeColorDeviceCmyk(decimal c, decimal m, decimal y, decimal k)
        {
            C = c;
            M = m;
            Y = y;
            K = k;
        }

        /// <inheritdoc />
        public void Run(IOperationContext operationContext)
        {
            operationContext.GetCurrentState().ColorSpaceContext.SetStrokingColorCmyk(C, M, Y, K);
        }

        /// <inheritdoc />
        public void Write(Stream stream)
        {
            stream.WriteDecimal(C);
            stream.WriteWhiteSpace();
            stream.WriteDecimal(M);
            stream.WriteWhiteSpace();
            stream.WriteDecimal(Y);
            stream.WriteWhiteSpace();
            stream.WriteDecimal(K);
            stream.WriteWhiteSpace();
            stream.WriteText(Symbol);
            stream.WriteNewLine();
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{C} {M} {Y} {K} {Symbol}";
        }
    }
}