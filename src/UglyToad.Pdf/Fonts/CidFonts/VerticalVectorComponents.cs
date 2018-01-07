namespace UglyToad.Pdf.Fonts.CidFonts
{
    /// <summary>
    /// Equivalent to the DW2 array in the font dictionary for vertical fonts.
    /// </summary>
    internal struct VerticalVectorComponents
    {
        public decimal Position { get; }

        public decimal Displacement { get; }

        public VerticalVectorComponents(decimal position, decimal displacement)
        {
            Position = position;
            Displacement = displacement;
        }

        public static VerticalVectorComponents Default = new VerticalVectorComponents(800, -1000);
    }
}