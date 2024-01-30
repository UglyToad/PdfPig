namespace UglyToad.PdfPig.Graphics.Operations.PathConstruction
{
    using System.IO;

    /// <inheritdoc />
    /// <summary>
    /// Append a cubic Bezier curve to the current path. 
    /// The curve extends from the current point to the point (x3, y3), using (x1, y1) and (x2, y2) as the Bezier control points 
    /// </summary>
    public class AppendDualControlPointBezierCurve : IGraphicsStateOperation
    {
        /// <summary>
        /// The symbol for this operation in a stream.
        /// </summary>
        public const string Symbol = "c";

        /// <inheritdoc />
        public string Operator => Symbol;

        /// <summary>
        /// First control point x.
        /// </summary>
        public double X1 { get; }

        /// <summary>
        /// First control point y.
        /// </summary>
        public double Y1 { get; }

        /// <summary>
        /// Second control point x.
        /// </summary>
        public double X2 { get; }

        /// <summary>
        /// Second control point y.
        /// </summary>
        public double Y2 { get; }

        /// <summary>
        /// End point x.
        /// </summary>
        public double X3 { get; }

        /// <summary>
        /// End point y.
        /// </summary>
        public double Y3 { get; }

        /// <summary>
        /// Create a new <see cref="AppendDualControlPointBezierCurve"/>.
        /// </summary>
        /// <param name="x1">Control point 1 x coordinate.</param>
        /// <param name="y1">Control point 1 y coordinate.</param>
        /// <param name="x2">Control point 2 x coordinate.</param>
        /// <param name="y2">Control point 2 y coordinate.</param>
        /// <param name="x3">End point x coordinate.</param>
        /// <param name="y3">End point y coordinate.</param>
        public AppendDualControlPointBezierCurve(double x1, double y1, double x2, double y2, double x3, double y3)
        {
            X1 = x1;
            Y1 = y1;
            X2 = x2;
            Y2 = y2;
            X3 = x3;
            Y3 = y3;
        }

        /// <inheritdoc />
        public void Run(IOperationContext operationContext)
        {
            operationContext.BezierCurveTo(X1, Y1, X2, Y2, X3, Y3);
        }

        /// <inheritdoc />
        public void Write(Stream stream)
        {
            stream.WriteDouble(X1);
            stream.WriteWhiteSpace();
            stream.WriteDouble(Y1);
            stream.WriteWhiteSpace();
            stream.WriteDouble(X2);
            stream.WriteWhiteSpace();
            stream.WriteDouble(Y2);
            stream.WriteWhiteSpace();
            stream.WriteDouble(X3);
            stream.WriteWhiteSpace();
            stream.WriteDouble(Y3);
            stream.WriteWhiteSpace();
            stream.WriteText(Symbol);
            stream.WriteNewLine();
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{X1} {Y1} {X2} {Y2} {X3} {Y3} {Symbol}";
        }
    }
}