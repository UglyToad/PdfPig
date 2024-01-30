namespace UglyToad.PdfPig.Graphics.Operations
{
    using System.IO;

    /// <summary>
    /// Set RGB color for stroking operations.
    /// </summary>
    public class SetStrokeColorDeviceRgb : IGraphicsStateOperation
    {
        /// <summary>
        /// The symbol for this operation in a stream.
        /// </summary>
        public const string Symbol = "RG";

        /// <inheritdoc />
        public string Operator => Symbol;

        /// <summary>
        /// The red level between 0 and 1.
        /// </summary>
        public double R { get; }

        /// <summary>
        /// The green level between 0 and 1.
        /// </summary>
        public double G { get; }

        /// <summary>
        /// The blue level between 0 and 1.
        /// </summary>
        public double B { get; }

        /// <summary>
        /// Create a new <see cref="SetStrokeColorDeviceRgb"/>.
        /// </summary>
        /// <param name="r">The red level.</param>
        /// <param name="g">The green level.</param>
        /// <param name="b">The blue level.</param>
        public SetStrokeColorDeviceRgb(double r, double g, double b)
        {
            R = r;
            G = g;
            B = b;
        }

        /// <inheritdoc />
        public void Run(IOperationContext operationContext)
        {
            operationContext.GetCurrentState().ColorSpaceContext.SetStrokingColorRgb(R, G, B);
        }

        /// <inheritdoc />
        public void Write(Stream stream)
        {
            stream.WriteDouble(R);
            stream.WriteWhiteSpace();
            stream.WriteDouble(G);
            stream.WriteWhiteSpace();
            stream.WriteDouble(B);
            stream.WriteWhiteSpace();
            stream.WriteText(Symbol);
            stream.WriteNewLine();
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{R} {G} {B} {Symbol}";
        }
    }
}
