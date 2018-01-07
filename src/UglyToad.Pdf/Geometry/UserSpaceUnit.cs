namespace UglyToad.Pdf.Geometry
{
    using System;

    /// <summary>
    /// By default user space units correspond to 1/72nd of an inch (a typographic point).
    /// The UserUnit entry in a page dictionary can define the space units as a different multiple of 1/72 (1 point).
    /// </summary>
    internal struct UserSpaceUnit
    {
        public static readonly UserSpaceUnit Default = new UserSpaceUnit(1);

        /// <summary>
        /// The number of points (1/72nd of an inch) corresponding to a single unit in user space.
        /// </summary>
        public int PointMultiples { get; }

        /// <summary>
        /// Create a new unit specification for a page.
        /// </summary>
        public UserSpaceUnit(int pointMultiples)
        {
            if (pointMultiples <= 0)
            {
                throw new ArgumentOutOfRangeException("Cannot have a zero or negative value of point multiples: " + pointMultiples);
            }

            PointMultiples = pointMultiples;
        }

        public override string ToString()
        {
            return PointMultiples.ToString();
        }
    }
}
