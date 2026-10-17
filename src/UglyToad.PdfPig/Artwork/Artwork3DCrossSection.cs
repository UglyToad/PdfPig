namespace UglyToad.PdfPig.Artwork
{
    using System.Collections.Generic;
    using Tokens;

    /// <summary>
    /// A cross section: a cutting plane applied to 3D artwork while a view is in use
    /// (ISO 32000-2, Table 322). Entries the document omits are <see langword="null"/>; the specification
    /// gives each a default that consumers apply.
    /// </summary>
    public sealed class Artwork3DCrossSection
    {
        /// <summary>
        /// The underlying cross section dictionary.
        /// </summary>
        public DictionaryToken Dictionary { get; }

        /// <summary>
        /// The centre of rotation on the cutting plane, in world coordinates (<c>C</c>): three numbers.
        /// Specification default: the origin.
        /// </summary>
        public IReadOnlyList<double>? Center { get; }

        /// <summary>
        /// The orientation of the cutting plane in world space (<c>O</c>): three entries for the X, Y and Z
        /// axes. Exactly one should be <see langword="null"/>: the plane starts perpendicular to that axis,
        /// clipping geometry on its positive side. The other two are rotations of the plane, in degrees,
        /// around their axes, applied in array order. Specification default: <c>[null 0 0]</c>.
        /// </summary>
        public IReadOnlyList<double?>? Orientation { get; }

        /// <summary>
        /// The opacity of the cutting plane, from 0 to 1 (<c>PO</c>). Specification default: 0.5.
        /// </summary>
        public double? PlaneOpacity { get; }

        /// <summary>
        /// The colour of the cutting plane (<c>PC</c>). Specification default: white.
        /// </summary>
        public Artwork3DColor? PlaneColor { get; }

        /// <summary>
        /// Whether the intersection of the cutting plane with the artwork is shown (<c>IV</c>).
        /// Specification default: <see langword="false"/>.
        /// </summary>
        public bool? IntersectionVisible { get; }

        /// <summary>
        /// The colour of the intersection of the cutting plane with the artwork (<c>IC</c>).
        /// Specification default: green.
        /// </summary>
        public Artwork3DColor? IntersectionColor { get; }

        /// <summary>
        /// Whether the part of the model on the clipped side of the plane is shown transparently
        /// (<c>ST</c>, PDF 2.0).
        /// </summary>
        public bool? ShowTransparent { get; }

        /// <summary>
        /// Whether the section is capped (<c>SC</c>, PDF 2.0).
        /// </summary>
        public bool? SectionCapping { get; }

        internal Artwork3DCrossSection(
            DictionaryToken dictionary,
            IReadOnlyList<double>? center,
            IReadOnlyList<double?>? orientation,
            double? planeOpacity,
            Artwork3DColor? planeColor,
            bool? intersectionVisible,
            Artwork3DColor? intersectionColor,
            bool? showTransparent,
            bool? sectionCapping)
        {
            Dictionary = dictionary;
            Center = center;
            Orientation = orientation;
            PlaneOpacity = planeOpacity;
            PlaneColor = planeColor;
            IntersectionVisible = intersectionVisible;
            IntersectionColor = intersectionColor;
            ShowTransparent = showTransparent;
            SectionCapping = sectionCapping;
        }
    }
}
