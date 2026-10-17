namespace UglyToad.PdfPig.Artwork
{
    using Tokens;

    /// <summary>
    /// The state a view gives to one node of the 3D artwork (ISO 32000-2, Table 323). A node is a 3D
    /// graphic object or a collection of them. Entries the document omits are <see langword="null"/>,
    /// meaning that aspect of the node is left as it is.
    /// </summary>
    public sealed class Artwork3DNode
    {
        /// <summary>
        /// The name of the node inside the 3D artwork (<c>N</c>).
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The opacity of the node's geometry, from 0 to 1 (<c>O</c>).
        /// </summary>
        public double? Opacity { get; }

        /// <summary>
        /// Whether the node is visible (<c>V</c>).
        /// </summary>
        public bool? Visible { get; }

        /// <summary>
        /// The node's position and orientation relative to its parent, in world coordinates (<c>M</c>).
        /// </summary>
        public Artwork3DMatrix? Matrix { get; }

        /// <summary>
        /// The render mode of this node, overriding the view's (<c>RM</c>, PDF 2.0).
        /// </summary>
        public Artwork3DRenderMode? RenderMode { get; }

        /// <summary>
        /// The rich media instance the <see cref="Data"/> applies to, as written (<c>Instance</c>, PDF 2.0).
        /// </summary>
        public IToken? Instance { get; }

        /// <summary>
        /// Data passed to and from the rich media run time (<c>Data</c>, PDF 2.0), when it is a text string.
        /// </summary>
        public string? Data { get; }

        internal Artwork3DNode(
            string name,
            double? opacity,
            bool? visible,
            Artwork3DMatrix? matrix,
            Artwork3DRenderMode? renderMode,
            IToken? instance,
            string? data)
        {
            Name = name;
            Opacity = opacity;
            Visible = visible;
            Matrix = matrix;
            RenderMode = renderMode;
            Instance = instance;
            Data = data;
        }

        /// <inheritdoc />
        public override string ToString() => Name;
    }
}
