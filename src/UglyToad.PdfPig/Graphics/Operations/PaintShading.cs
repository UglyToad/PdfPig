namespace UglyToad.PdfPig.Graphics.Operations
{
    using System;
    using System.IO;
    using Tokens;

    /// <inheritdoc />
    /// <summary>
    /// Paint the shape and color shading described by a shading dictionary, subject to the current clipping path.
    /// The current color in the graphics state is neither used nor altered.
    /// The effect is different from that of painting a path using a shading pattern as the current color.
    /// </summary>
    public class PaintShading : IGraphicsStateOperation
    {
        /// <summary>
        /// The symbol for this operation in a stream.
        /// </summary>
        public const string Symbol = "sh";

        /// <inheritdoc />
        public string Operator => Symbol;

        /// <summary>
        /// The name of a shading dictionary resource in the Shading subdictionary of the current resource dictionary.
        /// </summary>
        public NameToken Name { get; }

        /// <summary>
        /// Create a new <see cref="PaintShading"/>.
        /// </summary>
        /// <param name="name">The name of the shading dictionary.</param>
        public PaintShading(NameToken name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        /// <inheritdoc />
        public void Run(IOperationContext operationContext)
        {
            operationContext.PaintShading(Name);
        }

        /// <inheritdoc />
        public void Write(Stream stream)
        {
            stream.WriteText($"/{Name}");
            stream.WriteWhiteSpace();
            stream.WriteText(Symbol);
            stream.WriteNewLine();
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{Name} {Symbol}";
        }
    }
}