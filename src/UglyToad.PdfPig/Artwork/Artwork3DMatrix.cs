namespace UglyToad.PdfPig.Artwork
{
    using System;
    using System.Globalization;

    /// <summary>
    /// A 3D transformation matrix as stored in a PDF file: twelve numbers
    /// <c>[a b c d e f g h i tx ty tz]</c> (ISO 32000-2, 13.6.5).
    /// <para>
    /// The numbers are the first three columns of a 4-by-4 matrix whose fourth column is <c>0 0 0 1</c>.
    /// Points are row vectors and transform as <c>[x' y' z' 1] = [x y z 1] × M</c>, that is
    /// <c>x' = a·x + d·y + g·z + tx</c>, <c>y' = b·x + e·y + h·z + ty</c>, <c>z' = c·x + f·y + i·z + tz</c>.
    /// </para>
    /// <para>
    /// For a camera-to-world matrix the camera sits at the origin of camera space looking along the positive
    /// z axis, with positive x to the right and positive y up. This type converts nothing; consumers decide
    /// how to map it to their own conventions.
    /// </para>
    /// </summary>
    public readonly struct Artwork3DMatrix : IEquatable<Artwork3DMatrix>
    {
        /// <summary>
        /// The identity matrix.
        /// </summary>
        public static Artwork3DMatrix Identity { get; } = new Artwork3DMatrix(1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0);

        /// <summary>First row, first column.</summary>
        public double A { get; }

        /// <summary>First row, second column.</summary>
        public double B { get; }

        /// <summary>First row, third column.</summary>
        public double C { get; }

        /// <summary>Second row, first column.</summary>
        public double D { get; }

        /// <summary>Second row, second column.</summary>
        public double E { get; }

        /// <summary>Second row, third column.</summary>
        public double F { get; }

        /// <summary>Third row, first column.</summary>
        public double G { get; }

        /// <summary>Third row, second column.</summary>
        public double H { get; }

        /// <summary>Third row, third column.</summary>
        public double I { get; }

        /// <summary>Translation along x.</summary>
        public double Tx { get; }

        /// <summary>Translation along y.</summary>
        public double Ty { get; }

        /// <summary>Translation along z.</summary>
        public double Tz { get; }

        /// <summary>
        /// Create a new <see cref="Artwork3DMatrix"/> from its twelve numbers in file order.
        /// </summary>
        public Artwork3DMatrix(double a, double b, double c, double d, double e, double f,
            double g, double h, double i, double tx, double ty, double tz)
        {
            A = a;
            B = b;
            C = c;
            D = d;
            E = e;
            F = f;
            G = g;
            H = h;
            I = i;
            Tx = tx;
            Ty = ty;
            Tz = tz;
        }

        /// <summary>
        /// The number at the given position in file order, 0 to 11.
        /// </summary>
        public double this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0: return A;
                    case 1: return B;
                    case 2: return C;
                    case 3: return D;
                    case 4: return E;
                    case 5: return F;
                    case 6: return G;
                    case 7: return H;
                    case 8: return I;
                    case 9: return Tx;
                    case 10: return Ty;
                    case 11: return Tz;
                    default: throw new ArgumentOutOfRangeException(nameof(index), index, "The index must be between 0 and 11.");
                }
            }
        }

        /// <inheritdoc />
        public bool Equals(Artwork3DMatrix other)
        {
            return A.Equals(other.A) && B.Equals(other.B) && C.Equals(other.C)
                   && D.Equals(other.D) && E.Equals(other.E) && F.Equals(other.F)
                   && G.Equals(other.G) && H.Equals(other.H) && I.Equals(other.I)
                   && Tx.Equals(other.Tx) && Ty.Equals(other.Ty) && Tz.Equals(other.Tz);
        }

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is Artwork3DMatrix other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = A.GetHashCode();
                for (var i = 1; i < 12; i++)
                {
                    hash = (hash * 397) ^ this[i].GetHashCode();
                }

                return hash;
            }
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "[{0} {1} {2} {3} {4} {5} {6} {7} {8} {9} {10} {11}]",
                A, B, C, D, E, F, G, H, I, Tx, Ty, Tz);
        }
    }
}
