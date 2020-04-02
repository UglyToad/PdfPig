namespace UglyToad.PdfPig.Graphics.Operations.PathConstruction
{
    using System.IO;
    using PdfPig.Core;

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
        public decimal X1 { get; }

        /// <summary>
        /// First control point y.
        /// </summary>
        public decimal Y1 { get; }

        /// <summary>
        /// Second control point x.
        /// </summary>
        public decimal X2 { get; }

        /// <summary>
        /// Second control point y.
        /// </summary>
        public decimal Y2 { get; }

        /// <summary>
        /// End point x.
        /// </summary>
        public decimal X3 { get; }

        /// <summary>
        /// End point y.
        /// </summary>
        public decimal Y3 { get; }

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
            var controlPoint1 = new PdfPoint(X1, Y1);
            var controlPoint2 = new PdfPoint(X2, Y2);
            var end = new PdfPoint(X3, Y3);

            var controlPoint1Transform = operationContext.CurrentTransformationMatrix.Transform(controlPoint1);
            var controlPoint2Transform = operationContext.CurrentTransformationMatrix.Transform(controlPoint2);
            var endTransform = operationContext.CurrentTransformationMatrix.Transform(end);
            operationContext.CurrentSubpath.BezierCurveTo(controlPoint1Transform.X, controlPoint1Transform.Y,
                controlPoint2Transform.X, controlPoint2Transform.Y,
                endTransform.X, endTransform.Y);
            operationContext.CurrentPosition = endTransform;
        }

        /// <inheritdoc />
        public void Write(Stream stream)
        {
            stream.WriteDecimal(X1);
            stream.WriteWhiteSpace();
            stream.WriteDecimal(Y1);
            stream.WriteWhiteSpace();
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
            return $"{X1} {Y1} {X2} {Y2} {X3} {Y3} {Symbol}";
        }
    }
}