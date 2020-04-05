namespace UglyToad.PdfPig.Graphics.Operations.TextShowing
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Tokens;
    using Writer;

    /// <inheritdoc />
    /// <summary>
    /// Show one or more text strings, allowing individual glyph positioning. 
    /// Each element of array can be a string or a number.
    /// If the element is a string, this operator shows the string. 
    /// If it is a number, the operator adjusts the text position by that amount
    /// </summary>
    public class ShowTextsWithPositioning : IGraphicsStateOperation
    {
        /// <summary>
        /// The symbol for this operation in a stream.
        /// </summary>
        public const string Symbol = "TJ";

        /// <inheritdoc />
        public string Operator => Symbol;

        /// <summary>
        /// The array elements.
        /// </summary>
        public IReadOnlyList<IToken> Array { get; }

        /// <summary>
        /// Create a new <see cref="ShowTextsWithPositioning"/>.
        /// </summary>
        /// <param name="array">The array elements.</param>
        public ShowTextsWithPositioning(IReadOnlyList<IToken> array)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }

            for (var i = 0; i < array.Count; i++)
            {
                var token = array[i];
                if (!(token is StringToken) && !(token is NumericToken) && !(token is HexToken))
                {
                    throw new ArgumentException($"Found invalid token for showing texts with position: {token}");
                }
            }

            Array = array;
        }

        /// <inheritdoc />
        public void Run(IOperationContext operationContext)
        {
            operationContext.ShowPositionedText(Array);
        }

        /// <inheritdoc />
        public void Write(Stream stream)
        {
            stream.WriteText("[");
            for (var i = 0; i < Array.Count; i++)
            {
                TokenWriter.WriteToken(Array[i], stream);

                if (i < Array.Count - 1)
                {
                    stream.WriteWhiteSpace();
                }
            }
            stream.WriteText("]");
            stream.WriteWhiteSpace();
            stream.WriteText(Symbol);
            stream.WriteNewLine();
        }
    }
}