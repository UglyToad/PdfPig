namespace UglyToad.PdfPig.Graphics
{
    using PdfPig.Core;

    /// <summary>
    /// Manages the Text Matrix (Tm), Text line matrix (Tlm) and used to generate the Text Rendering Matrix (Trm).
    /// </summary>
    public class TextMatrices
    {
        /// <summary>
        /// The current text matrix (Tm).
        /// </summary>
        public TransformationMatrix TextMatrix { get; set; }

        /// <summary>
        /// Captures the value of the <see cref="TextMatrix"/> at the beginning of a line of text.
        /// This is convenient for aligning evenly spaced lines of text. 
        /// </summary>
        public TransformationMatrix TextLineMatrix { get; set; }
    }
}