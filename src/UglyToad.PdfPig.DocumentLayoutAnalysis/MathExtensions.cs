namespace UglyToad.PdfPig.DocumentLayoutAnalysis
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Useful math extensions.
    /// </summary>
    public static class MathExtensions
    {
        /// <summary>
        /// Computes the mode of a sequence of <see cref="float"/> values.
        /// </summary>
        /// <param name="array">The sequence of floats.</param>
        /// <returns>The mode of the sequence. Returns <see cref="float.NaN"/> if the sequence has no mode or if it is not unique.</returns>
        public static float Mode(this IEnumerable<float> array)
        {
            if (array == null || !array.Any()) return float.NaN;
            var sorted = array.GroupBy(v => v).Select(v => (v.Count(), v.Key)).OrderByDescending(g => g.Item1);
            var mode = sorted.First();
            if (sorted.Count() > 1 && mode.Item1 == sorted.ElementAt(1).Item1) return float.NaN;
            return mode.Key;
        }

        /// <summary>
        /// Computes the mode of a sequence of <see cref="double"/> values.
        /// </summary>
        /// <param name="array">The sequence of doubles.</param>
        /// <returns>The mode of the sequence. Returns <see cref="double.NaN"/> if the sequence has no mode or if it is not unique.</returns>
        public static double Mode(this IEnumerable<double> array)
        {
            if (array == null || !array.Any()) return double.NaN;
            var sorted = array.GroupBy(v => v).Select(v => (v.Count(), v.Key)).OrderByDescending(g => g.Item1);
            var mode = sorted.First();
            if (sorted.Count() > 1 && mode.Item1 == sorted.ElementAt(1).Item1) return double.NaN;
            return mode.Key;
        }

        /// <summary>
        /// Test for equality to 0.
        /// </summary>
        /// <param name="number"></param>
        /// <param name="epsilon"></param>
        public static bool AlmostEqualsToZero(this double number, double epsilon = 1e-5)
        {
            return (number > -epsilon) && (number < epsilon);
        }

        /// <summary>
        /// Test for equality.
        /// </summary>
        /// <param name="number"></param>
        /// <param name="other"></param>
        /// <param name="epsilon"></param>
        public static bool AlmostEquals(this double number, double other, double epsilon = 1e-5)
        {
            return AlmostEqualsToZero(number - other, epsilon);
        }

        /// <summary>
        /// Round the number using epsilon.
        /// </summary>
        /// <param name="number"></param>
        /// <param name="epsilon"></param>
        public static double Round(this double number, double epsilon = 1e-5)
        {
            return Math.Round(number / epsilon) * epsilon;
        }
    }
}
