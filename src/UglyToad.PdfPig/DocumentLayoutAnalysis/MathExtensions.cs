using System.Collections.Generic;
using System.Linq;

namespace UglyToad.PdfPig.DocumentLayoutAnalysis
{
    /// <summary>
    /// Useful math extensions.
    /// </summary>
    public static class MathExtensions
    {
        /// <summary>
        /// Computes the mode of a sequence of float values.
        /// </summary>
        /// <param name="array"></param>
        /// <returns></returns>
        public static float Mode(this IEnumerable<float> array)
        {
            if (array == null || array.Count() == 0) return float.NaN;
            return array.GroupBy(v => v).OrderByDescending(g => g.Count()).First().Key;
        }

        /// <summary>
        /// Computes the mode of a sequence of decimal values.
        /// </summary>
        /// <param name="array"></param>
        /// <returns></returns>
        public static decimal Mode(this IEnumerable<decimal> array)
        {
            return array.GroupBy(v => v).OrderByDescending(g => g.Count()).First().Key;
        }
    }
}
