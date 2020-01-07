namespace UglyToad.PdfPig.Graphics.Operations.InlineImages
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Tokens;

    /// <inheritdoc />
    /// <summary>
    /// Begin the image data for an inline image object. 
    /// </summary>
    public class BeginInlineImageData : IGraphicsStateOperation
    {
        /// <summary>
        /// The symbol for this operation in a stream.
        /// </summary>
        public const string Symbol = "ID";
        
        /// <inheritdoc />
        public string Operator => Symbol;

        /// <summary>
        /// The key-value pairs which specify attributes of the following image.
        /// </summary>
        public IReadOnlyDictionary<NameToken, IToken> Dictionary { get; }

        /// <summary>
        /// Create a new <see cref="BeginInlineImageData"/>.
        /// </summary>
        public BeginInlineImageData(IReadOnlyDictionary<NameToken, IToken> dictionary)
        {
            Dictionary = dictionary ?? throw new ArgumentNullException(nameof(dictionary));
        }

        /// <inheritdoc />
        public void Run(IOperationContext operationContext)
        {
            operationContext.SetInlineImageProperties(Dictionary);
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