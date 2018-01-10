namespace UglyToad.PdfPig.Graphics
{
    using PdfPig.Core;

    /// <summary>
    /// Manages the Text Matrix (Tm), Text line matrix (Tlm) and Text Rendering Matrix (Trm).
    /// </summary>
    internal class TextMatrices
    {
        public TransformationMatrix TextMatrix { get; set; }

        public TransformationMatrix TextLineMatrix { get; set; }

        /// <summary>
        /// Gets the Text Rendering Matrix (Trm) which maps from text space to device space.
        /// </summary>
        /// <remarks>
        /// <para>The rendering matrix is temporary and is calculated for each glyph in a text showing operation.</para>
        /// <para>
        /// The rendering matrix is calculated as follows:<br/>
        /// | (Tfs * Th)   0    0 |<br/>
        /// |     0       Tfs   0 | * Tm * CTM<br/>
        /// |     0      Trise  1 |<br/>
        /// Where Tfs is the font size, Th is the horizontal scaling, Trise is the text rise, Tm is the current <see cref="TextMatrix"/> and CTM is the
        /// <see cref="CurrentGraphicsState.CurrentTransformationMatrix"/>.
        /// </para>
        /// </remarks>
        public TransformationMatrix GetRenderingMatrix(CurrentGraphicsState currentGraphicsState)
        {
            var fontSize = currentGraphicsState.FontState.FontSize;
            var horizontalScaling = currentGraphicsState.FontState.HorizontalScaling;
            var rise = currentGraphicsState.FontState.Rise;

            var initialMatrix = TransformationMatrix.FromArray(new[]
            {
                (fontSize * horizontalScaling), 0, 0,
                0, fontSize, 0,
                0, rise, 1
            });

            var multipledByTextMatrix = initialMatrix.Multiply(TextMatrix);

            var result = multipledByTextMatrix.Multiply(currentGraphicsState.CurrentTransformationMatrix);
            
            return result;
        }
    }
}