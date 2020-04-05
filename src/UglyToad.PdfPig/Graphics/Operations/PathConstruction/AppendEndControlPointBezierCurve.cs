namespace UglyToad.PdfPig.Graphics.Operations.PathConstruction
{
    using System.IO;
    using PdfPig.Core;

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
        /// The x coordinate of the first control point.
        /// </summary>
        public decimal X1 { get; }

        /// <summary>
        /// The y coordinate of the first control point.
        /// </summary>
        public decimal Y1 { get; }

        /// <summary>
        /// The x coordinate of the end point.
        /// </summary>
        public decimal X3 { get; }

        /// <summary>
        /// The y coordinate of the end point.
        /// </summary>
        public decimal Y3 { get; }
        
        /// <summary>
        /// Create a new <see cref="AppendEndControlPointBezierCurve"/>.
        /// </summary>
        /// <param name="x1">Control point 1 x coordinate.</param>
        /// <param name="y1">Control point 1 y coordinate.</param>
        /// <param name="x3">Control point 2/End x coordinate.</param>
        /// <param name="y3">Control point 2/End y coordinate.</param>
        public AppendEndControlPointBezierCurve(decimal x1, decimal y1, decimal x3, decimal y3)
        {
            X1 = x1;
            Y1 = y1;
            X3 = x3;
            Y3 = y3;
        }
        
        /// <inheritdoc />
        public void Run(IOperationContext operationContext)
        {
            if (operationContext.CurrentSubpath == null) return;

            var controlPoint1 = operationContext.CurrentTransformationMatrix.Transform(new PdfPoint(X1, Y1));
            var end = operationContext.CurrentTransformationMatrix.Transform(new PdfPoint(X3, Y3));
            operationContext.CurrentSubpath.BezierCurveTo(controlPoint1.X, controlPoint1.Y, end.X, end.Y, end.X, end.Y);
            operationContext.CurrentPosition = end;
        }

        /// <inheritdoc />
        public void Write(Stream stream)
        {
            stream.WriteDecimal(X1);
            stream.WriteWhiteSpace();
            stream.WriteDecimal(Y1);
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
            return $"{X1} {Y1} {X3} {Y3} {Symbol}";
        }
    }
}