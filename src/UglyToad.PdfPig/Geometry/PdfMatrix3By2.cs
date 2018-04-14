namespace UglyToad.PdfPig.Geometry
{
    internal struct PdfMatrix3By2
    {
        private readonly decimal r0c0;
        private readonly decimal r0c1;
        private readonly decimal r1c0;
        private readonly decimal r1c1;
        private readonly decimal r2c0;
        private readonly decimal r2c1;

        public PdfMatrix3By2(decimal r0C0, decimal r0C1, decimal r1C0, decimal r1C1, decimal r2C0, decimal r2C1)
        {
            r0c0 = r0C0;
            r0c1 = r0C1;
            r1c0 = r1C0;
            r1c1 = r1C1;
            r2c0 = r2C0;
            r2c1 = r2C1;
        }

        public static PdfMatrix3By2 Identity { get; } = new PdfMatrix3By2(1, 0, 0, 1, 0, 0);
        public static PdfMatrix3By2 CreateTranslation(PdfVector vector) => new PdfMatrix3By2(1, 0, 0, 1, vector.X, vector.Y);
        public static PdfMatrix3By2 CreateTranslation(decimal x, decimal y) => new PdfMatrix3By2(1, 0, 0, 1, x, y);

        public PdfMatrix3By2 WithTranslation(decimal x, decimal y)
        {
            return new PdfMatrix3By2(r0c0, r0c1, r1c0, r1c1, x, y);
        }
    }
}
