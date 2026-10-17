namespace UglyToad.PdfPig.Artwork
{
    using System.Collections.Generic;
    using Tokens;

    /// <summary>
    /// A 3D view (ISO 32000-2, 13.6.4): a named camera position together with the settings used to render
    /// the 3D artwork from it.
    /// </summary>
    public sealed class Artwork3DView
    {
        /// <summary>
        /// The underlying view dictionary.
        /// </summary>
        public DictionaryToken ViewDictionary { get; }

        /// <summary>
        /// The external name of the view, suitable for presentation in a user interface (<c>XN</c>).
        /// Required by the specification; <see langword="null"/> if the document omits it.
        /// </summary>
        public string? ExternalName { get; }

        /// <summary>
        /// The internal name of the view, used to refer to it from other objects (<c>IN</c>). When the
        /// document gives none this is the <see cref="ExternalName"/>.
        /// </summary>
        public string? InternalName { get; }

        /// <summary>
        /// How the camera-to-world matrix is determined (<c>MS</c>).
        /// </summary>
        public Artwork3DMatrixSource MatrixSource { get; }

        /// <summary>
        /// The position and orientation of the camera in world coordinates (<c>C2W</c>), when present and
        /// well formed. Meaningful when <see cref="MatrixSource"/> is <see cref="Artwork3DMatrixSource.Matrix"/>.
        /// </summary>
        public Artwork3DMatrix? CameraToWorld { get; }

        /// <summary>
        /// The node identifiers leading to a view node inside U3D artwork (<c>U3DPath</c>): the first is the
        /// root view node, each subsequent one a child of the previous. Empty when absent. Meaningful when
        /// <see cref="MatrixSource"/> is <see cref="Artwork3DMatrixSource.U3DPath"/>.
        /// </summary>
        public IReadOnlyList<string> U3DPath { get; }

        /// <summary>
        /// The distance along the camera's z axis to the centre of orbit (<c>CO</c>), or
        /// <see langword="null"/> if the viewer should determine it.
        /// </summary>
        public double? CenterOfOrbit { get; }

        /// <summary>
        /// The projection onto the annotation's target coordinate system (<c>P</c>), or
        /// <see langword="null"/> for the default: a perspective projection with a field of view of 90 degrees.
        /// </summary>
        public Artwork3DProjection? Projection { get; }

        /// <summary>
        /// The background over which the artwork is drawn (<c>BG</c>), or <see langword="null"/> for the
        /// default white background.
        /// </summary>
        public Artwork3DBackground? Background { get; }

        /// <summary>
        /// The render mode (<c>RM</c>), or <see langword="null"/> to use the one specified in the artwork.
        /// </summary>
        public Artwork3DRenderMode? RenderMode { get; }

        /// <summary>
        /// The lighting scheme (<c>LS</c>), or <see langword="null"/> to use the lights specified in the artwork.
        /// </summary>
        public Artwork3DLightingScheme? LightingScheme { get; }

        /// <summary>
        /// The cross sections applied while this view is in use (<c>SA</c>). Empty when there are none.
        /// </summary>
        public IReadOnlyList<Artwork3DCrossSection> CrossSections { get; }

        /// <summary>
        /// The node states applied by this view (<c>NA</c>). Empty when there are none. If a node appears
        /// more than once only its last entry applies.
        /// </summary>
        public IReadOnlyList<Artwork3DNode> Nodes { get; }

        /// <summary>
        /// Whether nodes are returned to their original states, as specified in the artwork, before
        /// <see cref="Nodes"/> are applied (<c>NR</c>).
        /// </summary>
        public bool ResetNodes { get; }

        /// <summary>
        /// A form XObject overlaid on the rendered artwork while this view is undisturbed (<c>O</c>), if any.
        /// </summary>
        public StreamToken? Overlay { get; }

        /// <summary>
        /// The 3D measurement and markup dictionaries displayed with this view (<c>MA</c>), unparsed.
        /// Empty when there are none.
        /// </summary>
        public IReadOnlyList<DictionaryToken> Measurements { get; }

        internal Artwork3DView(
            DictionaryToken viewDictionary,
            string? externalName,
            string? internalName,
            Artwork3DMatrixSource matrixSource,
            Artwork3DMatrix? cameraToWorld,
            IReadOnlyList<string> u3dPath,
            double? centerOfOrbit,
            Artwork3DProjection? projection,
            Artwork3DBackground? background,
            Artwork3DRenderMode? renderMode,
            Artwork3DLightingScheme? lightingScheme,
            IReadOnlyList<Artwork3DCrossSection> crossSections,
            IReadOnlyList<Artwork3DNode> nodes,
            bool resetNodes,
            StreamToken? overlay,
            IReadOnlyList<DictionaryToken> measurements)
        {
            ViewDictionary = viewDictionary;
            ExternalName = externalName;
            InternalName = internalName;
            MatrixSource = matrixSource;
            CameraToWorld = cameraToWorld;
            U3DPath = u3dPath;
            CenterOfOrbit = centerOfOrbit;
            Projection = projection;
            Background = background;
            RenderMode = renderMode;
            LightingScheme = lightingScheme;
            CrossSections = crossSections;
            Nodes = nodes;
            ResetNodes = resetNodes;
            Overlay = overlay;
            Measurements = measurements;
        }

        /// <inheritdoc />
        public override string ToString() => ExternalName ?? InternalName ?? "(unnamed view)";
    }
}
