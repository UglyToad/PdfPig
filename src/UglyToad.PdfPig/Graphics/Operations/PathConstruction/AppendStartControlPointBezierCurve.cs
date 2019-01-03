namespace UglyToad.PdfPig.Graphics.Operations.PathConstruction
{
    using System.IO;
    using Geometry;

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
        /// The second control point.
        /// </summary>
        public PdfPoint ControlPoint2 { get; }

        /// <summary>
        /// The last point on the curve.
        /// </summary>
        public PdfPoint End { get; }

        /// <summary>
        /// Create a new <see cref="AppendStartControlPointBezierCurve"/>.
        /// </summary>
        /// <param name="x2">The x coordinate of the second control point.</param>
        /// <param name="y2">The y coordinate of the second control point.</param>
        /// <param name="x3">The x coordinate of the end point.</param>
        /// <param name="y3">The y coordinate of the end point.</param>
        public AppendStartControlPointBezierCurve(decimal x2, decimal y2, decimal x3, decimal y3)
        {
            ControlPoint2 = new PdfPoint(x2, y2);
            End = new PdfPoint(x3, y3);
        }

        /// <inheritdoc />
        public void Run(IOperationContext operationContext)
        {
            operationContext.CurrentPath.BezierCurveTo(operationContext.CurrentPosition.X,
                operationContext.CurrentPosition.Y,
                ControlPoint2.X,
                ControlPoint2.Y,
                End.X,
                End.Y);
            operationContext.CurrentPosition = End;
        }

        /// <inheritdoc />
        public void Write(Stream stream)
        {
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
            return $"{ControlPoint2.X} {ControlPoint2.Y} {End.X} {End.Y} {Symbol}";
        }
    }
}