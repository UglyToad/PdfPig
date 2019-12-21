using System;

namespace UglyToad.PdfPig.Geometry
{
    internal struct PdfVector
    {
        public double X { get; }

        public double Y { get; }

        public PdfVector(double x, double y)
        {
            X = x;
            Y = y;
        }

        public PdfVector Scale(double scale)
        {
            return new PdfVector(X * scale, Y * scale);
        }

        public double GetMagnitude()
        {
            var doubleX = X;
            var doubleY = Y;

            return Math.Sqrt(doubleX * doubleX + doubleY * doubleY);
        }

        public PdfVector Subtract(PdfVector vector)
        {
            return new PdfVector(X - vector.X, Y - vector.Y);
        }

        public PdfPoint ToPoint()
        {
            return new PdfPoint(X, Y);
        }

        public override string ToString()
        {
            return $"({X}, {Y})";
        }
    }
}
