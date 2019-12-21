using System;
using System.Collections.Generic;

namespace UglyToad.PdfPig.Tests
{
    internal class DoubleComparer : IEqualityComparer<double>
    {
        private readonly double precision;

        public DoubleComparer(double precision)
        {
            this.precision = precision;
        }

        public bool Equals(double x, double y)
        {
            return Math.Abs(x - y) < precision;
        }

        public int GetHashCode(double obj)
        {
            return obj.GetHashCode();
        }
    }
}
