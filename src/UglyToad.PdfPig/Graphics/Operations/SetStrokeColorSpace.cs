namespace UglyToad.PdfPig.Graphics.Operations
{
    using System.IO;
    using Tokens;
    using Writer;

    /// <inheritdoc />
    /// <summary>
    /// Set the current color space to use for stroking operations. 
    /// </summary>
    public class SetStrokeColorSpace : IGraphicsStateOperation
    {
        private static readonly TokenWriter TokenWriter = new TokenWriter();

        /// <summary>
        /// The symbol for this operation in a stream.
        /// </summary>
        public const string Symbol = "CS";

        /// <inheritdoc />
        public string Operator => Symbol;

        /// <summary>
        /// The name of the color space.
        /// </summary>
        public NameToken Name { get; }

        /// <summary>
        /// Create a new <see cref="SetStrokeColorSpace"/>.
        /// </summary>
        /// <param name="name">The name of the color space.</param>
        public SetStrokeColorSpace(NameToken name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        /// <inheritdoc />
        public void Run(IOperationContext operationContext)
        {
            operationContext.GetCurrentState().ColorSpaceContext.SetStrokingColorspace(Name);
        }

        /// <inheritdoc />
        public void Write(Stream stream)
        {
            TokenWriter.WriteToken(Name, stream);
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
