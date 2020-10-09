namespace UglyToad.PdfPig.Geometry
{
    using System;
    using Core;

    /// <summary>
    /// PdfVector
    /// </summary>
    public struct PdfVector
    {
        /// <summary>
        /// X
        /// </summary>
        public double X { get; }

        /// <summary>
        /// Y
        /// </summary>
        public double Y { get; }

        /// <summary>
        /// PdfVector
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public PdfVector(double x, double y)
        {
            X = x;
            Y = y;
        }

        /// <summary>
        /// PdfVector
        /// </summary>
        /// <param name="scale"></param>
        public PdfVector Scale(double scale)
        {
            return new PdfVector(X * scale, Y * scale);
        }

        /// <summary>
        /// GetMagnitude
        /// </summary>
        public double GetMagnitude()
        {
            var doubleX = X;
            var doubleY = Y;

            return Math.Sqrt(doubleX * doubleX + doubleY * doubleY);
        }

        /// <summary>
        /// Subtract
        /// </summary>
        /// <param name="vector"></param>
        public PdfVector Subtract(PdfVector vector)
        {
            return new PdfVector(X - vector.X, Y - vector.Y);
        }

        /// <summary>
        /// ToPoint
        /// </summary>
        public PdfPoint ToPoint()
        {
            return new PdfPoint(X, Y);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"({X}, {Y})";
        }
    }
}
