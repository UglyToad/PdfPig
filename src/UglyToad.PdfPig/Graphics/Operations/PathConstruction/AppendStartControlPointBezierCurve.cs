namespace UglyToad.PdfPig.Graphics.Operations.PathConstruction
{
    using System.IO;

    /// <inheritdoc />
    /// <summary>
    /// Append a cubic Bezier curve to the current path. 
    /// The curve extends from the current point to the point (x3, y3), using the current point and (x2, y2) as the Bezier control points 
    /// </summary>
    public class AppendStartControlPointBezierCurve : IGraphicsStateOperation
    {
        /// <summary>
        /// The symbol for this operation in a stream.
        /// </summary>
        public const string Symbol = "v";

        /// <inheritdoc />
        public string Operator => Symbol;
        
        /// <summary>
        /// The x coordinate of the second control point.
        /// </summary>
        public decimal X2 { get; }

        /// <summary>
        /// The y coordinate of the second control point.
        /// </summary>
        public decimal Y2 { get; }

        /// <summary>
        /// The x coordinate of the end point of the curve.
        /// </summary>
        public decimal X3 { get; }

        /// <summary>
        /// The y coordinate of the end point of the curve.
        /// </summary>
        public decimal Y3 { get; }

        /// <summary>
        /// Create a new <see cref="AppendStartControlPointBezierCurve"/>.
        /// </summary>
        /// <param name="x2">The x coordinate of the second control point.</param>
        /// <param name="y2">The y coordinate of the second control point.</param>
        /// <param name="x3">The x coordinate of the end point.</param>
        /// <param name="y3">The y coordinate of the end point.</param>
        public AppendStartControlPointBezierCurve(decimal x2, decimal y2, decimal x3, decimal y3)
        {
            X2 = x2;
            Y2 = y2;
            X3 = x3;
            Y3 = y3;
        }

        /// <inheritdoc />
        public void Run(IOperationContext operationContext)
        {
            operationContext.BezierCurveTo((double)X2, (double)Y2, (double)X3, (double)Y3);
        }

        /// <inheritdoc />
        public void Write(Stream stream)
        {
            stream.WriteDecimal(X2);
            stream.WriteWhiteSpace();
            stream.WriteDecimal(Y2);
            stream.WriteWhiteSpace();
            stream.WriteDecimal(X3);
            stream.WriteWhiteSpace();
            stream.WriteDecimal(Y3);
            stream.WriteWhiteSpace();
            stream.WriteText(Symbol);
            stream.WriteNewLine();
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{X2} {Y2} {X3} {Y3} {Symbol}";
        }
    }
}