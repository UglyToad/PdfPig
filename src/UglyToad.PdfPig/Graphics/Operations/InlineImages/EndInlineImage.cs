namespace UglyToad.PdfPig.Graphics.Operations.InlineImages
{
    using System;
    using System.Collections.Generic;
    using System.IO;

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
        /// The raw data for the inline image which should be interpreted according to the corresponding <see cref="BeginInlineImageData.Dictionary"/>.
        /// </summary>
        public IReadOnlyList<byte> ImageData { get; }
        
        /// <inheritdoc />
        public string Operator => Symbol;

        /// <summary>
        /// Create a new <see cref="EndInlineImage"/> operation.
        /// </summary>
        /// <param name="imageData">The raw byte data of this image.</param>
        public EndInlineImage(IReadOnlyList<byte> imageData)
        {
            ImageData = imageData ?? throw new ArgumentNullException(nameof(imageData));
        }

        /// <inheritdoc />
        public void Run(IOperationContext operationContext)
        {
            operationContext.EndInlineImage(ImageData);
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