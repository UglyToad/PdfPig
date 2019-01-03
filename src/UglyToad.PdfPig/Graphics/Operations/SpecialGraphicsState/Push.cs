namespace UglyToad.PdfPig.Graphics.Operations.SpecialGraphicsState
{
    using System.IO;

    /// <inheritdoc />
    /// <summary>
    /// Save the current graphics state on the graphics state stack.
    /// </summary>
    public class Push : IGraphicsStateOperation
    {
        /// <summary>
        /// The symbol for this operation in a stream.
        /// </summary>
        public const string Symbol = "q";

        /// <summary>
        /// The instance of the <see cref="Push"/> operation.
        /// </summary>
        public static readonly Push Value = new Push();

        /// <inheritdoc />
        public string Operator => Symbol;

        private Push()
        {
        }

        /// <inheritdoc />
        public void Run(IOperationContext context)
        {
            context.PushState();
        }

        /// <inheritdoc />
        public void Write(Stream stream)
        {
            stream.WriteText(Symbol);
            stream.WriteNewLine();
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return Symbol;
        }
    }
}
