namespace UglyToad.PdfPig.Graphics.Shading
{
    using UglyToad.PdfPig.Tokenization.Scanner;
    using UglyToad.PdfPig.Tokens;

    /// <summary>
    /// Resources for a tensor-product patch mesh shading.
    /// <para>Type 7 shadings (tensor-product patch meshes) are identical to type 6, except that they are based on a bicubic 
    /// tensor-product patch defined by 16 control points instead of the 12 control points that define a Coons patch.</para>
    /// </summary>
    public class PdfShadingType7 : PdfShadingType6
    {
        /// <summary>
        ///
        /// <para>Tensor-product patch mesh.</para>
        /// <para>Type 7 shadings (tensor-product patch meshes) are identical to type 6, except that they are based on a bicubic 
        /// tensor-product patch defined by 16 control points instead of the 12 control points that define a Coons patch.</para>
        /// </summary>
        /// <param name="shadingDictionary"></param>
        /// <param name="pdfTokenScanner"></param>
        public PdfShadingType7(DictionaryToken shadingDictionary, IPdfTokenScanner pdfTokenScanner)
            : base(shadingDictionary, pdfTokenScanner)
        { }
    }
}
