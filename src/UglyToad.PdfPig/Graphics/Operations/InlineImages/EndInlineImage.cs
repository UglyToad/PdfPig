namespace UglyToad.PdfPig.Graphics.Operations.InlineImages
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Tokens;

    /// <inheritdoc />
    /// <summary>
    /// End an inline image object.
    /// </summary>
    public class EndInlineImage : IGraphicsStateOperation
    {
        /// <summary>
        /// The symbol for this operation in a stream.
        /// </summary>
        public const string Symbol = "EI";

        /// <summary>
        /// The tokens declared in order for this inline image object.
        /// </summary>
        public IReadOnlyList<IToken> ImageTokens { get; }

        /// <summary>
        /// The raw data for the inline image which should be interpreted according to the <see cref="ImageTokens"/>.
        /// </summary>
        public IReadOnlyList<byte> ImageData { get; }
        
        /// <inheritdoc />
        public string Operator => Symbol;

        /// <summary>
        /// Create a new <see cref="EndInlineImage"/> operation.
        /// </summary>
        /// <param name="imageTokens">The tokens which were set during the declaration of this image.</param>
        /// <param name="imageData">The raw byte data of this image.</param>
        public EndInlineImage(IReadOnlyList<IToken> imageTokens, IReadOnlyList<byte> imageData)
        {
            ImageTokens = imageTokens ?? throw new ArgumentNullException(nameof(imageTokens));
            ImageData = imageData ?? throw new ArgumentNullException(nameof(imageData));
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