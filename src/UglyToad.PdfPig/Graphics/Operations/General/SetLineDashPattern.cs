namespace UglyToad.PdfPig.Graphics.Operations.General
{
    using Core;
    using System.IO;

    /// <inheritdoc />
    /// <summary>
    /// Set the line dash pattern in the graphics state.
    /// </summary>
    public class SetLineDashPattern : IGraphicsStateOperation
    {
        /// <summary>
        /// The symbol for this operation in a stream.
        /// </summary>
        public const string Symbol = "d";

        /// <inheritdoc />
        public string Operator => Symbol;

        /// <summary>
        /// The line dash pattern.
        /// </summary>
        public LineDashPattern Pattern { get; }
        
        /// <summary>
        /// Create a new <see cref="SetLineDashPattern"/>.
        /// </summary>
        public SetLineDashPattern(decimal[] array, int phase)
        {
            Pattern = new LineDashPattern(phase, array);
        }

        /// <inheritdoc />
        public void Run(IOperationContext operationContext)
        {
            operationContext.SetLineDashPattern(Pattern);
        }

        /// <inheritdoc />
        public void Write(Stream stream)
        {
            stream.WriteText("[");

            for (var i = 0; i < Pattern.Array.Count; i++)
            {
                var value = Pattern.Array[i];
                stream.WriteDecimal(value);

                if (i < Pattern.Array.Count - 1)
                {
                    stream.WriteWhiteSpace();
                }
            }

            stream.WriteText("]");

            stream.WriteWhiteSpace();

            stream.WriteDecimal(Pattern.Phase);

            stream.WriteWhiteSpace();

            stream.WriteText(Symbol);

            stream.WriteNewLine();
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{Pattern.Array} {Pattern.Phase} {Symbol}";
        }
    }
}