namespace UglyToad.PdfPig.Graphics.Operations.ClippingPaths
{
    using System.IO;

    /// <inheritdoc />
    /// <summary>
    /// Modify the current clipping path by intersecting it with the current path, using the
    /// even-odd rule to determine which regions lie inside the clipping path.
    /// </summary>
    public class ModifyClippingByEvenOddIntersect : IGraphicsStateOperation
    {
        /// <summary>
        /// The symbol for this operation in a stream.
        /// </summary>
        public const string Symbol = "W*";

        /// <summary>
        /// The instance of the <see cref="ModifyClippingByEvenOddIntersect"/> operation.
        /// </summary>
        public static readonly ModifyClippingByEvenOddIntersect Value = new ModifyClippingByEvenOddIntersect();

        /// <inheritdoc />
        public string Operator => Symbol;

        private ModifyClippingByEvenOddIntersect()
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