namespace UglyToad.PdfPig.Graphics.Operations.MarkedContent
{
    using System.IO;
    using Tokens;
    using Writer;

    /// <inheritdoc />
    /// <summary>
    /// Designate a single marked-content point in the content stream.
    /// </summary>
    public class DesignateMarkedContentPoint : IGraphicsStateOperation
    {
        private static readonly TokenWriter TokenWriter = new TokenWriter();

        /// <summary>
        /// The symbol for this operation in a stream.
        /// </summary>
        public const string Symbol = "MP";

        /// <inheritdoc />
        public string Operator => Symbol;

        /// <summary>
        /// A name indicating the role or significance of the point.
        /// </summary>
        public NameToken Name { get; }

        /// <summary>
        /// Create a new <see cref="DesignateMarkedContentPoint"/>.
        /// </summary>
        /// <param name="name">The name of the marked-content point.</param>
        public DesignateMarkedContentPoint(NameToken name)
        {
            Name = name;
        }

        /// <inheritdoc />
        public void Run(IOperationContext operationContext)
        {
        }

        /// <inheritdoc />
        public void Write(Stream stream)
        {
            TokenWriter.WriteToken(Name, stream);
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
