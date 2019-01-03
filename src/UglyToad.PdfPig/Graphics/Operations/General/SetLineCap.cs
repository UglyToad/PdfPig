namespace UglyToad.PdfPig.Graphics.Operations.General
{
    using System;
    using System.IO;
    using Core;

    /// <inheritdoc />
    /// <summary>
    /// Set the line cap style in the graphics state.
    /// </summary>
    public class SetLineCap : IGraphicsStateOperation
    {
        /// <summary>
        /// The symbol for this operation in a stream.
        /// </summary>
        public const string Symbol = "J";

        /// <inheritdoc />
        public string Operator => Symbol;

        /// <summary>
        /// The cap style.
        /// </summary>
        public LineCapStyle Cap { get; }

        /// <inheritdoc />
        /// <summary>
        /// Create a new <see cref="T:UglyToad.PdfPig.Graphics.Operations.General.SetLineCap" />.
        /// </summary>
        public SetLineCap(int cap) : this((LineCapStyle)cap) { }

        /// <summary>
        /// Create a new <see cref="SetLineCap"/>.
        /// </summary>
        public SetLineCap(LineCapStyle cap)
        {
            if (cap < 0 || (int)cap > 2)
            {
                throw new ArgumentException("Invalid argument passed for line cap style. Should be 0, 1 or 2; instead got: " + cap);
            }

            Cap = cap;
        }

        /// <inheritdoc />
        public void Run(IOperationContext operationContext)
        {
            operationContext.GetCurrentState().CapStyle = Cap;
        }

        /// <inheritdoc />
        public void Write(Stream stream)
        {
            stream.WriteNumberText((int)Cap, Symbol);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{(int) Cap} {Symbol}";
        }
    }
}