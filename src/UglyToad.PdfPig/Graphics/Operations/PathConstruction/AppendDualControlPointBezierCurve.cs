namespace UglyToad.PdfPig.Graphics.Operations.PathConstruction
{
    using System.IO;
    using Geometry;
    
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
        /// The first control point.
        /// </summary>
        public PdfPoint ControlPoint1 { get; }

        /// <summary>
        /// The second control point.
        /// </summary>
        public PdfPoint ControlPoint2 { get; }

        /// <summary>
        /// The end point.
        /// </summary>
        public PdfPoint End { get; }

        /// <summary>
        /// Create a new <see cref="AppendDualControlPointBezierCurve"/>.
        /// </summary>
        /// <param name="x1">Control point 1 x coordinate.</param>
        /// <param name="y1">Control point 1 y coordinate.</param>
        /// <param name="x2">Control point 2 x coordinate.</param>
        /// <param name="y2">Control point 2 y coordinate.</param>
        /// <param name="x3">End point x coordinate.</param>
        /// <param name="y3">End point y coordinate.</param>
        public AppendDualControlPointBezierCurve(decimal x1, decimal y1, decimal x2, decimal y2, decimal x3, decimal y3)
        {
            ControlPoint1 = new PdfPoint(x1, y1);
            ControlPoint2 = new PdfPoint(x2, y2);
            End = new PdfPoint(x3, y3);
        }

        /// <inheritdoc />
        public void Run(IOperationContext operationContext)
        {
            operationContext.CurrentPath.BezierCurveTo(ControlPoint1.X, ControlPoint1.Y,
                ControlPoint2.X, ControlPoint2.Y,
                End.X, End.Y);
            operationContext.CurrentPosition = End;
        }

        /// <inheritdoc />
        public void Write(Stream stream)
        {
            stream.WriteDecimal(ControlPoint1.X);
            stream.WriteWhiteSpace();
            stream.WriteDecimal(ControlPoint1.Y);
            stream.WriteWhiteSpace();
            stream.WriteDecimal(ControlPoint2.X);
            stream.WriteWhiteSpace();
            stream.WriteDecimal(ControlPoint2.Y);
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
            return $"{ControlPoint1.X} {ControlPoint1.Y} {ControlPoint2.X} {ControlPoint2.Y} {End.X} {End.Y} {Symbol}";
        }
    }
}