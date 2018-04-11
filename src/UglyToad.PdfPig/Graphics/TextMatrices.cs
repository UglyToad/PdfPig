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
    }
}