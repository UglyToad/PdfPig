namespace UglyToad.PdfPig.Graphics.Operations.General
{
    using System.IO;

    /// <inheritdoc />
    /// <summary>
    /// Set the flatness tolerance in the graphics state. 
    /// Flatness is a number in the range 0 to 100; a value of 0 specifies the output device’s default flatness tolerance.
    /// </summary>
    public class SetFlatnessTolerance : IGraphicsStateOperation
    {
        /// <summary>
        /// The symbol for this operation in a stream.
        /// </summary>
        public const string Symbol = "i";

        /// <inheritdoc />
        public string Operator => Symbol;

        /// <summary>
        /// The flatness tolerance controls the maximum permitted distance in device pixels
        /// between the mathematically correct path and an approximation constructed from straight line segments.
        /// </summary>
        public decimal Tolerance { get; }

        /// <summary>
        /// Create new <see cref="SetFlatnessTolerance"/>.
        /// </summary>
        /// <param name="tolerance">The flatness tolerance.</param>
        public SetFlatnessTolerance(decimal tolerance)
        {
            Tolerance = tolerance;
        }

        /// <inheritdoc />
        public void Run(IOperationContext operationContext)
        {
            operationContext.GetCurrentState().Flatness = Tolerance;
        }

        /// <inheritdoc />
        public void Write(Stream stream)
        {
            stream.WriteNumberText(Tolerance, Symbol);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{Tolerance} {Symbol}";
        }
    }
}