namespace UglyToad.PdfPig.Artwork
{
    /// <summary>
    /// How projected 3D artwork is scaled to the annotation's 3D view box (the <c>PS</c> and <c>OB</c> entries, ISO 32000-2, Table 316).
    /// </summary>
    public enum Artwork3DScaling : byte
    {
        /// <summary>
        /// Fit the width of the view box (<c>W</c>).
        /// </summary>
        Width = 0,

        /// <summary>
        /// Fit the height of the view box (<c>H</c>).
        /// </summary>
        Height = 1,

        /// <summary>
        /// Fit the lesser of width and height (<c>Min</c>).
        /// </summary>
        Min = 2,

        /// <summary>
        /// Fit the greater of width and height (<c>Max</c>).
        /// </summary>
        Max = 3,

        /// <summary>
        /// No scaling due to binding (<c>Absolute</c>, orthographic projections only).
        /// </summary>
        Absolute = 4,

        /// <summary>
        /// An explicit number is given (perspective projections only).
        /// </summary>
        Value = 5
    }
}
