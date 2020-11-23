namespace UglyToad.PdfPig.Graphics.Operations.General
{
    using System;
    using System.IO;
    using Core;

    /// <inheritdoc />
    public class SetLineJoin : IGraphicsStateOperation
    {
        /// <summary>
        /// The symbol for this operation in a stream.
        /// </summary>
        public const string Symbol = "j";

        /// <inheritdoc />
        public string Operator => Symbol;

        /// <summary>
        /// The line join style.
        /// </summary>
        public LineJoinStyle Join { get; }

        /// <summary>
        /// Create a new <see cref="SetLineJoin"/>.
        /// </summary>
        public SetLineJoin(int join) : this((LineJoinStyle)join) { }
        /// <summary>
        /// Create a new <see cref="SetLineJoin"/>.
        /// </summary>
        public SetLineJoin(LineJoinStyle join)
        {
            if (join < 0 || (int)join > 2)
            {
                throw new ArgumentException("Invalid argument passed for line join style. Should be 0, 1 or 2; instead got: " + join);
            }

            Join = join;
        }

        /// <inheritdoc />
        public void Run(IOperationContext operationContext)
        {
            operationContext.SetLineJoin(Join);
        }

        /// <inheritdoc />
        public void Write(Stream stream)
        {
            stream.WriteNumberText((int)Join, Symbol);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{(int)Join} {Symbol}";
        }
    }
}