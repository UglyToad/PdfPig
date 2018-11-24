namespace UglyToad.PdfPig.Content
{
    using System.Collections.Generic;
    using System.Linq;
    using Geometry;

    /// <summary>
    /// The corresponding named size of the <see cref="Page"/>.
    /// </summary>
    public enum PageSize
    {
        /// <summary>
        /// Unknown page size, did not match a defined page size.
        /// </summary>
        Custom = 0,
        /// <summary>
        /// The ISO 216 A0 page size.
        /// </summary>
        A0 = 3,
        /// <summary>
        /// The ISO 216 A1 page size.
        /// </summary>
        A1 = 4,
        /// <summary>
        /// The ISO 216 A2 page size.
        /// </summary>
        A2 = 5,
        /// <summary>
        /// The ISO 216 A3 page size.
        /// </summary>
        A3 = 6,
        /// <summary>
        /// The ISO 216 A4 page size.
        /// </summary>
        A4 = 7,
        /// <summary>
        /// The ISO 216 A5 page size.
        /// </summary>
        A5 = 8,
        /// <summary>
        /// The ISO 216 A6 page size.
        /// </summary>
        A6 = 9,
        /// <summary>
        /// The ISO 216 A7 page size.
        /// </summary>
        A7 = 10,
        /// <summary>
        /// The ISO 216 A8 page size.
        /// </summary>
        A8 = 11,
        /// <summary>
        /// The ISO 216 A9 page size.
        /// </summary>
        A9 = 12,
        /// <summary>
        /// The ISO 216 A10 page size.
        /// </summary>
        A10 = 13,
        /// <summary>
        /// The North American Letter page size.
        /// </summary>
        Letter = 14,
        /// <summary>
        /// The North American Legal page size.
        /// </summary>
        Legal = 15,
        /// <summary>
        /// The North American Ledger page size.
        /// </summary>
        Ledger = 16,
        /// <summary>
        /// The North American Tabloid page size.
        /// </summary>
        Tabloid = 17,
        /// <summary>
        /// The North American Executive page size.
        /// </summary>
        Executive = 18
    }

    internal static class PageSizeExtensions
    {
        private static readonly Dictionary<WidthHeight, PageSize> Lookup = new Dictionary<WidthHeight, PageSize>
        {
            {new WidthHeight(2384, 3370), PageSize.A0},
            {new WidthHeight(1684, 2384), PageSize.A1},
            // Seems there is some disagreement 1190/1191
            {new WidthHeight(1190, 1684), PageSize.A2},
            {new WidthHeight(1191, 1684), PageSize.A2},
            // Seems there is some disagreement 1190/1191
            {new WidthHeight(842, 1190), PageSize.A3},
            {new WidthHeight(842, 1191), PageSize.A3},
            // Possibly some kind of rounding mix-up here
            {new WidthHeight(595, 842), PageSize.A4},
            {new WidthHeight(595, 841), PageSize.A4},
            {new WidthHeight(420, 595), PageSize.A5},
            {new WidthHeight(298, 420), PageSize.A6},
            {new WidthHeight(210, 298), PageSize.A7},
            {new WidthHeight(147, 210), PageSize.A8},
            {new WidthHeight(105, 147), PageSize.A9},
            {new WidthHeight(74, 105), PageSize.A10},
            {new WidthHeight(612, 792), PageSize.Letter},
            {new WidthHeight(612, 1008), PageSize.Legal},
            {new WidthHeight(1224, 792), PageSize.Ledger},
            {new WidthHeight(792, 1224), PageSize.Tabloid},
            // Again there is disagreement here
            {new WidthHeight(540, 720), PageSize.Executive},
            {new WidthHeight(522, 756), PageSize.Executive},
        };

        public static PageSize GetPageSize(this PdfRectangle rectangle)
        {
            if (!Lookup.TryGetValue(new WidthHeight(rectangle.Width, rectangle.Height), out var size))
            {
                return PageSize.Custom;
            }

            return size;
        }

        public static bool TryGetPdfRectangle(this PageSize size, out PdfRectangle rectangle)
        {
            rectangle = default(PdfRectangle);

            var match = Lookup.FirstOrDefault(x => x.Value == size);

            if (match.Key.Width > 0)
            {
                rectangle = new PdfRectangle(0, 0, match.Key.Width, match.Key.Height);
            }

            return match.Key.Width > 0;
        }

        private struct WidthHeight
        {
            public decimal Width { get; }

            public decimal Height { get; }

            public WidthHeight(decimal width, decimal height)
            {
                Width = width;
                Height = height;
            }

            public override bool Equals(object obj)
            {
                return obj is WidthHeight height &&
                       Width == height.Width &&
                       Height == height.Height;
            }

            public override int GetHashCode()
            {
                var hashCode = 859600377;
                hashCode = hashCode * -1521134295 + Width.GetHashCode();
                hashCode = hashCode * -1521134295 + Height.GetHashCode();
                return hashCode;
            }
        }
    }
}
