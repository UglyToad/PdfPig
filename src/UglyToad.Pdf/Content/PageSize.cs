namespace UglyToad.Pdf.Content
{
    using System.Collections.Generic;
    using Geometry;

    public enum PageSize
    {
        Custom = 0,
        A0 = 3,
        A1 = 4,
        A2 = 5,
        A3 = 6,
        A4 = 7,
        A5 = 8,
        A6 = 9,
        A7 = 10,
        A8 = 11,
        A9 = 12,
        A10 = 13,
        Letter = 14,
        Legal = 15,
        Ledger = 16,
        Tabloid = 17,
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
            {new WidthHeight(595, 842), PageSize.A4},
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
