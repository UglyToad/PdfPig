namespace UglyToad.PdfPig.Graphics.Operations.ClippingPaths
{
    using System.IO;

    /// <inheritdoc />
    /// <summary>
    /// Modify the current clipping path by intersecting it with the current path, using the
    /// nonzero winding number rule to determine which regions lie inside the clipping path.
    /// </summary>
    public class ModifyClippingByNonZeroWindingIntersect : IGraphicsStateOperation
    {
        /// <summary>
        /// The symbol for this operation in a stream.
        /// </summary>
        public const string Symbol = "W";

        /// <summary>
        /// The instance of the <see cref="EndPath"/> operation.
        /// </summary>
        public static readonly ModifyClippingByNonZeroWindingIntersect Value = new ModifyClippingByNonZeroWindingIntersect();

        /// <inheritdoc />
        public string Operator => Symbol;

        private ModifyClippingByNonZeroWindingIntersect()
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