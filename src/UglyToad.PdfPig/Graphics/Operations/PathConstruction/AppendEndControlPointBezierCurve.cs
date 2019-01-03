namespace UglyToad.PdfPig.Graphics.Operations.PathConstruction
{
    using System.IO;
    using Geometry;

    /// <inheritdoc />
    /// <summary>
    /// Append a cubic Bezier curve to the current path. 
    /// The curve extends from the current point to the point (x3, y3), using (x1, y1) and (x3, y3) as the Bezier control points 
    /// </summary>
    public class AppendEndControlPointBezierCurve : IGraphicsStateOperation
    {
        /// <summary>
        /// The symbol for this operation in a stream.
        /// </summary>
        public const string Symbol = "y";

        /// <inheritdoc />
        public string Operator => Symbol;

        /// <summary>
        /// The first control point.
        /// </summary>
        public PdfPoint ControlPoint1 { get; }

        /// <summary>
        /// The end point and second control point.
        /// </summary>
        public PdfPoint End { get; }

        /// <summary>
        /// Create a new <see cref="AppendEndControlPointBezierCurve"/>.
        /// </summary>
        /// <param name="x1">Control point 1 x coordinate.</param>
        /// <param name="y1">Control point 1 y coordinate.</param>
        /// <param name="x3">Control point 2/End x coordinate.</param>
        /// <param name="y3">Control point 2/End y coordinate.</param>
        public AppendEndControlPointBezierCurve(decimal x1, decimal y1, decimal x3, decimal y3)
        {
            ControlPoint1 = new PdfPoint(x1, y1);
            End = new PdfPoint(x3, y3);
        }

        /// <inheritdoc />
        public void Run(IOperationContext operationContext)
        {
            operationContext.CurrentPath.BezierCurveTo(ControlPoint1.X, ControlPoint1.Y,
                End.X,
                End.Y,
                End.X,
                End.Y);
            operationContext.CurrentPosition = End;
        }

        /// <inheritdoc />
        public void Write(Stream stream)
        {
            stream.WriteDecimal(ControlPoint1.X);
            stream.WriteWhiteSpace();
            stream.WriteDecimal(ControlPoint1.Y);
            stream.WriteWhiteSpace();
            stream.WriteDecimal(End.X);
            stream.WriteWhiteSpace();
            stream.WriteDecimal(End.Y);
            stream.WriteWhiteSpace();
            stream.WriteText(Symbol);
            stream.WriteNewLine();
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{ControlPoint1.X} {ControlPoint1.Y} {End.X} {End.Y} {Symbol}";
        }
    }
}