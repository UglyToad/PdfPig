using System;
using System.Collections.Generic;

namespace UglyToad.PdfPig.Tests
{
    internal class DecimalComparer : IEqualityComparer<decimal>
    {
        private readonly decimal precision;

        public DecimalComparer(decimal precision)
        {
            this.precision = precision;
        }

        public bool Equals(decimal x, decimal y)
        {
            return Math.Abs(x - y) < precision;
        }

        public int GetHashCode(decimal obj)
        {
            return obj.GetHashCode();
        }
    }
}
