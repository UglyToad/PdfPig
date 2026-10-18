namespace UglyToad.PdfPig.Artwork
{
    /// <summary>
    /// The lighting applied to 3D artwork by a view (ISO 32000-2, Table 320).
    /// </summary>
    public sealed class Artwork3DLightingScheme
    {
        /// <summary>
        /// The lighting style (<c>Subtype</c>). <see cref="Artwork3DLightingStyle.Unknown"/> when the name
        /// is missing or not one this library recognises.
        /// </summary>
        public Artwork3DLightingStyle Style { get; }

        /// <summary>
        /// The lighting style name as written, if any.
        /// </summary>
        public string? Name { get; }

        internal Artwork3DLightingScheme(Artwork3DLightingStyle style, string? name)
        {
            Style = style;
            Name = name;
        }

        /// <inheritdoc />
        public override string ToString() => Name ?? Style.ToString();
    }
}
