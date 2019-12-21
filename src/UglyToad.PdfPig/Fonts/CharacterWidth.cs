namespace UglyToad.PdfPig.Fonts
{
    /// <summary>
    /// The x and y components of the width vector of the font's characters.
    /// Presence implies that IsFixedPitch is true.
    /// </summary>
    internal class CharacterWidth
    {
        public double X { get; }

        public double Y { get; }

        public CharacterWidth(double x, double y)
        {
            X = x;
            Y = y;
        }
    }
}