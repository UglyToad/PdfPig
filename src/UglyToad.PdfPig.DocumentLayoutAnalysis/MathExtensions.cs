namespace UglyToad.PdfPig.DocumentLayoutAnalysis
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Useful math extensions.
    /// </summary>
    public static class MathExtensions
    {
        /// <summary>
        /// Computes the mode of a sequence of float values.
        /// </summary>
        /// <param name="array">The array of floats.</param>
        public static float Mode(this IEnumerable<float> array)
        {
            if (array == null || array.Count() == 0) return float.NaN;
            var sorted = array.GroupBy(v => v).Select(v => (v.Count(), v.Key)).OrderByDescending(g => g.Item1);
            var mode = sorted.First();
            if (mode.Item1 == sorted.ElementAt(1).Item1) return float.NaN;
            return mode.Key;
        }

        /// <summary>
        /// Computes the mode of a sequence of decimal values.
        /// </summary>
        /// <param name="array">The array of decimal.</param>
        public static double Mode(this IEnumerable<double> array)
        {
            if (array == null || array.Count() == 0) return double.NaN;
            var sorted = array.GroupBy(v => v).Select(v => (v.Count(), v.Key)).OrderByDescending(g => g.Item1);
            var mode = sorted.First();
            if (mode.Item1 == sorted.ElementAt(1).Item1) return double.NaN;
            return mode.Key;
        }
    }
}
