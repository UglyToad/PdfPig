namespace UglyToad.PdfPig.Graphics.Operations.SpecialGraphicsState
{
    using System.IO;
    using Tokens;

    /// <inheritdoc />
    /// <summary>
    ///  Set the specified parameters in the graphics state using the ExtGState subdictionary with the given name.
    /// </summary>
    public class SetGraphicsStateParametersFromDictionary : IGraphicsStateOperation
    {
        /// <summary>
        /// The symbol for this operation in a stream.
        /// </summary>
        public const string Symbol = "gs";

        /// <inheritdoc />
        public string Operator => Symbol;

        /// <summary>
        /// The name of a graphics state parameter dictionary in the ExtGState subdictionary of the current resource dictionary.
        /// </summary>
        public NameToken Name { get; }

        /// <summary>
        /// Create a new <see cref="SetGraphicsStateParametersFromDictionary"/>.
        /// </summary>
        /// <param name="name">The name of the ExtGState dictionary.</param>
        public SetGraphicsStateParametersFromDictionary(NameToken name)
        {
            Name = name;
        }

        /// <inheritdoc />
        public void Run(IOperationContext operationContext)
        {
            operationContext.SetNamedGraphicsState(Name);
        }

        /// <inheritdoc />
        public void Write(Stream stream)
        {
            stream.WriteText($"/{Name.Data}");
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
