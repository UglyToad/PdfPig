namespace UglyToad.PdfPig.Artwork
{
    /// <summary>
    /// The style in which 3D artwork is rendered (ISO 32000-2, Table 319).
    /// </summary>
    public enum Artwork3DRenderModeType : byte
    {
        /// <summary>
        /// A render mode this library does not recognise. Such a render mode dictionary should be ignored.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Textured and lit geometric shapes.
        /// </summary>
        Solid = 1,

        /// <summary>
        /// Textured and lit geometric shapes with single-colour edges on top.
        /// </summary>
        SolidWireframe = 2,

        /// <summary>
        /// Textured and lit geometric shapes with added transparency.
        /// </summary>
        Transparent = 3,

        /// <summary>
        /// Textured and lit geometric shapes with added transparency and single-colour opaque edges on top.
        /// </summary>
        TransparentWireframe = 4,

        /// <summary>
        /// The bounding box edges of each node.
        /// </summary>
        BoundingBox = 5,

        /// <summary>
        /// The bounding box faces of each node, with added transparency.
        /// </summary>
        TransparentBoundingBox = 6,

        /// <summary>
        /// The bounding box edges and faces of each node, with added transparency.
        /// </summary>
        TransparentBoundingBoxOutline = 7,

        /// <summary>
        /// Only edges, in a single colour.
        /// </summary>
        Wireframe = 8,

        /// <summary>
        /// Only edges, interpolating their colour between vertices and computing it from lighting.
        /// </summary>
        ShadedWireframe = 9,

        /// <summary>
        /// Edges in a single colour, with hidden edges removed.
        /// </summary>
        HiddenWireframe = 10,

        /// <summary>
        /// Only vertices, in a single colour.
        /// </summary>
        Vertices = 11,

        /// <summary>
        /// Only vertices, colour computed from lighting.
        /// </summary>
        ShadedVertices = 12,

        /// <summary>
        /// Silhouette edges with surfaces, hidden lines removed.
        /// </summary>
        Illustration = 13,

        /// <summary>
        /// Silhouette edges with lit and textured surfaces, hidden lines removed.
        /// </summary>
        SolidOutline = 14,

        /// <summary>
        /// Silhouette edges with lit and textured surfaces and an additional emissive term to remove poorly lit areas.
        /// </summary>
        ShadedIllustration = 15
    }
}
