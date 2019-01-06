namespace UglyToad.PdfPig.Graphics.Operations.Compatibility
{
    using System.IO;

    /// <inheritdoc />
    /// <summary>
    /// Begin a compatibility section. Unrecognized operators (along with their operands) are ignored without error.
    /// </summary>
    public class BeginCompatibilitySection : IGraphicsStateOperation
    {
        /// <summary>
        /// The symbol for this operation in a stream.
        /// </summary>
        public const string Symbol = "BX";

        /// <summary>
        /// The instance of the <see cref="BeginCompatibilitySection"/> operation.
        /// </summary>
        public static readonly BeginCompatibilitySection Value = new BeginCompatibilitySection();

        /// <inheritdoc />
        public string Operator => Symbol;

        private BeginCompatibilitySection()
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

        /// <inheritdoc />
        public override string ToString()
        {
            return Symbol;
        }
    }
}
