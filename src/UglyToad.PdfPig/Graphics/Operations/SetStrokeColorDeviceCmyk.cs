namespace UglyToad.PdfPig.Graphics.Operations
{
    using System.IO;

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
        public double C { get; }

        /// <summary>
        /// The magenta level between 0 and 1.
        /// </summary>
        public double M { get; }

        /// <summary>
        /// The yellow level between 0 and 1.
        /// </summary>
        public double Y { get; }

        /// <summary>
        /// The key level between 0 and 1.
        /// </summary>
        public double K { get; }

        /// <summary>
        /// Create a new <see cref="SetStrokeColorDeviceCmyk"/>.
        /// </summary>
        /// <param name="c">The cyan level.</param>
        /// <param name="m">The magenta level.</param>
        /// <param name="y">The yellow level.</param>
        /// <param name="k">The key level.</param>
        public SetStrokeColorDeviceCmyk(double c, double m, double y, double k)
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
            stream.WriteDouble(C);
            stream.WriteWhiteSpace();
            stream.WriteDouble(M);
            stream.WriteWhiteSpace();
            stream.WriteDouble(Y);
            stream.WriteWhiteSpace();
            stream.WriteDouble(K);
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
