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
    }

    /// <inheritdoc />
    /// <summary>
    /// Begin a marked-content sequence terminated by a balancing EMC operator.
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
    }

    /// <inheritdoc />
    /// <summary>
    /// End a marked-content sequence.
    /// </summary>
    public class EndMarkedContent : IGraphicsStateOperation
    {
        /// <summary>
        /// The symbol for this operation in a stream.
        /// </summary>
        public const string Symbol = "EMC";

        /// <summary>
        /// The instance of the <see cref="EndMarkedContent"/> operation.
        /// </summary>
        public static readonly EndMarkedContent Value = new EndMarkedContent();

        /// <inheritdoc />
        public string Operator => Symbol;
        
        private EndMarkedContent()
        {
        }

        /// <inheritdoc />
        public void Run(IOperationContext operationContext)
        {
        }

        /// <inheritdoc />
        public void Write(Stream stream)
        {
            stream.WriteText(Symbol);
            stream.WriteNewLine();
        }
    }
}
