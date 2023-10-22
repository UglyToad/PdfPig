namespace UglyToad.PdfPig.Geometry
{
    using System;
    using System.Globalization;

    /// <summary>
    /// By default user space units correspond to 1/72nd of an inch (a typographic point).
    /// The UserUnit entry in a page dictionary can define the space units as a different multiple of 1/72 (1 point).
    /// </summary>
    public readonly struct UserSpaceUnit
    {
        /// <summary>
        /// Default user space units with <see cref="PointMultiples"/> set to <c>1</c>.
        /// </summary>
        public static readonly UserSpaceUnit Default = new UserSpaceUnit(1);

        /// <summary>
        /// The number of points (1/72nd of an inch) corresponding to a single unit in user space.
        /// </summary>
        public int PointMultiples { get; }

        /// <summary>
        /// Create a new unit specification for a page.
        /// </summary>
        internal UserSpaceUnit(int pointMultiples)
        {
            if (pointMultiples <= 0)
            {
                throw new ArgumentOutOfRangeException("Cannot have a zero or negative value of point multiples: " + pointMultiples);
            }

            PointMultiples = pointMultiples;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return PointMultiples.ToString(CultureInfo.InvariantCulture);
        }
    }
}
