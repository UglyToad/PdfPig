namespace UglyToad.PdfPig.Artwork
{
    using Tokens;

    /// <summary>
    /// The background over which 3D artwork is drawn (ISO 32000-2, Table 317). The only background type is
    /// a single opaque colour.
    /// </summary>
    public sealed class Artwork3DBackground
    {
        /// <summary>
        /// The background colour (<c>C</c>). Default white.
        /// </summary>
        public Artwork3DColor Color { get; }

        /// <summary>
        /// Whether the background applies to the entire annotation (<see langword="true"/>) or only to the
        /// annotation's 3D view box (<c>EA</c>). Default <see langword="false"/>.
        /// </summary>
        public bool EntireAnnotation { get; }

        /// <summary>
        /// The colour space entry as written (<c>CS</c>), if any. The only valid value is DeviceRGB;
        /// <see cref="Color"/> is read on that assumption.
        /// </summary>
        public IToken? ColorSpace { get; }

        internal Artwork3DBackground(Artwork3DColor color, bool entireAnnotation, IToken? colorSpace)
        {
            Color = color;
            EntireAnnotation = entireAnnotation;
            ColorSpace = colorSpace;
        }
    }
}
