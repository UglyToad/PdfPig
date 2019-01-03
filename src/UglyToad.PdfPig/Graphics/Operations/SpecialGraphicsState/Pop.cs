namespace UglyToad.PdfPig.Graphics.Operations.SpecialGraphicsState
{
    using System;
    using System.IO;

    /// <inheritdoc />
    /// <summary>
    /// Restore the graphics state by removing the most recently saved state from the stack and making it the current state.
    /// </summary>
    public class Pop : IGraphicsStateOperation
    {
        /// <summary>
        /// The symbol for this operation in a stream.
        /// </summary>
        public const string Symbol = "Q";

        /// <summary>
        /// The instance of the <see cref="Pop"/> operation.
        /// </summary>
        public static readonly Pop Value = new Pop();

        /// <inheritdoc />
        public string Operator => Symbol;

        private Pop()
        {
        }

        /// <inheritdoc />
        public void Run(IOperationContext operationContext)
        {
            var currentStackSize = operationContext.StackSize;
            if (currentStackSize > 1)
            {
                operationContext.PopState();
            }
            else
            {
                throw new InvalidOperationException("Cannot execute a pop of the graphics state stack, it would leave the stack empty.");
            }
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