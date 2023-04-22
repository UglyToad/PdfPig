namespace IccProfileNet
{
    internal struct IccXyz
    {
        /// <summary>
        /// X.
        /// </summary>
        public double X { get; }

        /// <summary>
        /// Y.
        /// </summary>
        public double Y { get; }

        /// <summary>
        /// Z.
        /// </summary>
        public double Z { get; }

        public IccXyz(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{X}, {Y}, {Z}";
        }
    }
}
