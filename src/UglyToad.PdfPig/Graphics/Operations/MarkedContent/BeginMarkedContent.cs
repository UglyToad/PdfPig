namespace UglyToad.PdfPig.Graphics.Operations.MarkedContent
{
    using System.IO;
    using Tokens;
    using Writer;

    /// <inheritdoc />
    /// <summary>
    /// Begin a marked-content sequence terminated by a balancing <see cref="EndMarkedContent"/> operator.
    /// </summary>
    public class BeginMarkedContent : IGraphicsStateOperation
    {
        /// <summary>
        /// The symbol for this operation in a stream.
        /// </summary>
        public const string Symbol = "BMC";

        /// <inheritdoc />
        public string Operator => Symbol;

        /// <summary>
        /// A name indicating the role or significance of the sequence.
        /// </summary>
        public NameToken Name { get; }

        /// <summary>
        /// Create a new <see cref="BeginMarkedContent"/>.
        /// </summary>
        /// <param name="name">The name of the marked-content sequence.</param>
        public BeginMarkedContent(NameToken name)
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