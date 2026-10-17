namespace UglyToad.PdfPig.Artwork
{
    /// <summary>
    /// The style in which 3D artwork is rendered (ISO 32000-2, Table 318).
    /// </summary>
    public sealed class Artwork3DRenderMode
    {
        /// <summary>
        /// The render mode (<c>Subtype</c>). <see cref="Artwork3DRenderModeType.Unknown"/> when the name is
        /// missing or not one this library recognises, in which case the dictionary should be ignored.
        /// </summary>
        public Artwork3DRenderModeType Type { get; }

        /// <summary>
        /// The render mode name as written, if any.
        /// </summary>
        public string? Name { get; }

        /// <summary>
        /// The auxiliary colour, often used for points or edges (<c>AC</c>). <see langword="null"/> when
        /// absent or given in a colour space other than DeviceRGB; the specification's default is black.
        /// </summary>
        public Artwork3DColor? AuxiliaryColor { get; }

        /// <summary>
        /// The face colour, relevant only to <see cref="Artwork3DRenderModeType.Illustration"/>, when given
        /// as a DeviceRGB colour (<c>FC</c>).
        /// </summary>
        public Artwork3DColor? FaceColor { get; }

        /// <summary>
        /// Whether the face colour is given as the name <c>BG</c>: the background colour currently in use.
        /// This is also the specification's default when <c>FC</c> is absent.
        /// </summary>
        public bool FaceColorIsBackground { get; }

        /// <summary>
        /// The opacity of the transparency added by some render modes (<c>O</c>).
        /// <see langword="null"/> when absent.
        /// </summary>
        public double? Opacity { get; }

        /// <summary>
        /// The crease angle in degrees used by the render modes that draw silhouette edges (<c>CV</c>).
        /// <see langword="null"/> when absent.
        /// </summary>
        public double? CreaseValue { get; }

        internal Artwork3DRenderMode(
            Artwork3DRenderModeType type,
            string? name,
            Artwork3DColor? auxiliaryColor,
            Artwork3DColor? faceColor,
            bool faceColorIsBackground,
            double? opacity,
            double? creaseValue)
        {
            Type = type;
            Name = name;
            AuxiliaryColor = auxiliaryColor;
            FaceColor = faceColor;
            FaceColorIsBackground = faceColorIsBackground;
            Opacity = opacity;
            CreaseValue = creaseValue;
        }

        /// <inheritdoc />
        public override string ToString() => Name ?? Type.ToString();
    }
}
