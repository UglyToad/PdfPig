// ReSharper disable InconsistentNaming
namespace UglyToad.PdfPig.Fonts.TrueType.Glyphs
{
    using Core;

    internal readonly struct CompositeTransformMatrix3By2
    {
        private readonly double r0c0;
        private readonly double r0c1;
        private readonly double r1c0;
        private readonly double r1c1;
        private readonly double r2c0;
        private readonly double r2c1;

        public CompositeTransformMatrix3By2(double r0C0, double r0C1, double r1C0, double r1C1, double r2C0, double r2C1)
        {
            r0c0 = r0C0;
            r0c1 = r0C1;
            r1c0 = r1C0;
            r1c1 = r1C1;
            r2c0 = r2C0;
            r2c1 = r2C1;
        }

        public static readonly CompositeTransformMatrix3By2 Identity = new CompositeTransformMatrix3By2(1, 0, 0, 1, 0, 0);
        public static CompositeTransformMatrix3By2 CreateTranslation(double x, double y) => new CompositeTransformMatrix3By2(1, 0, 0, 1, x, y);

        public CompositeTransformMatrix3By2 WithTranslation(double x, double y)
        {
            return new CompositeTransformMatrix3By2(r0c0, r0c1, r1c0, r1c1, x, y);
        }

        public PdfPoint ScaleAndRotate(PdfPoint source)
        {
            var newX = source.X * r0c0 + source.Y * r1c0;
            var newY = source.X * r0c1 + source.Y * r1c1;

            return new PdfPoint(newX, newY);
        }

        public PdfPoint Translate(PdfPoint source)
        {
            return new PdfPoint(source.X + r2c0, source.Y + r2c1);
        }
    }
}
