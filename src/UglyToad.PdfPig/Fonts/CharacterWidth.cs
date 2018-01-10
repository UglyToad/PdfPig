namespace UglyToad.PdfPig.Fonts
{
    /// <summary>
    /// The x and y components of the width vector of the font's characters.
    /// Presence implies that IsFixedPitch is true.
    /// </summary>
    internal class CharacterWidth
    {
        public decimal X { get; }

        public decimal Y { get; }

        public CharacterWidth(decimal x, decimal y)
        {
            X = x;
            Y = y;
        }
    }
}