namespace UglyToad.PdfPig.Graphics.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Util.JetBrains.Annotations;

    /// <summary>
    /// The line dash pattern controls the pattern of dashes and gaps used to stroke paths.
    /// It is specified by a dash array and a dash phase.
    /// </summary>
    public readonly struct LineDashPattern
    {
        /// <summary>
        /// The distance into the dash pattern at which to start the dash.
        /// </summary>
        public int Phase { get; }

        /// <summary>
        /// The numbers that specify the lengths of alternating dashes and gaps.
        /// </summary>
        [NotNull]
        public IReadOnlyList<decimal> Array { get; }

        /// <summary>
        /// Create a new <see cref="LineDashPattern"/>.
        /// </summary>
        /// <param name="phase">The phase. <see cref="Phase"/>.</param>
        /// <param name="array">The array. <see cref="Array"/>.</param>
        public LineDashPattern(int phase, [NotNull]IReadOnlyList<decimal> array)
        {
            Phase = phase;
            Array = array ?? throw new ArgumentNullException(nameof(array));
        }

        /// <summary>
        /// The default solid line.
        /// </summary>
        public static LineDashPattern Solid { get; }
            = new LineDashPattern(0, new decimal[0]);

        /// <inheritdoc />
        public override string ToString()
        {
            var arrayStr = string.Join(" ", Array.Select(x => x.ToString("N")));
            return $"[{arrayStr}] {Phase}.";
        }
    }
}
