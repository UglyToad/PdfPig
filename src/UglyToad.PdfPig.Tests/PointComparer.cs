namespace UglyToad.PdfPig.Tests
{
    using System;
    using System.Collections.Generic;
    using PdfPig.Geometry;

    internal class PointComparer : IEqualityComparer<PdfPoint>
    {
        private readonly IEqualityComparer<double> doubleComparer;

        public PointComparer(IEqualityComparer<double> doubleComparer)
        {
            this.doubleComparer = doubleComparer ?? throw new ArgumentNullException(nameof(doubleComparer));
        }

        public bool Equals(PdfPoint a, PdfPoint b)
        {
            return doubleComparer.Equals(a.X, b.X)
                   && doubleComparer.Equals(a.Y, b.Y);
        }

        public int GetHashCode(PdfPoint obj)
        {
            return obj.GetHashCode();
        }
    }
}