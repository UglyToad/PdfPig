namespace UglyToad.PdfPig.Graphics.Shading
{
    using System;
    using UglyToad.PdfPig.Function;
    using UglyToad.PdfPig.Tokenization.Scanner;
    using UglyToad.PdfPig.Tokens;

    /// <summary>
    /// Resources for a function based shading.
    /// </summary>
    public class PdfShadingType1 : PdfShading
    {
        /// <summary>
        /// (Optional) An array of four numbers [xmin xmax ymin ymax] specifying the rectangular domain of coordinates over which the colour function(s) are defined.
        /// Default value: [0.0 1.0 0.0 1.0].
        /// </summary>
        public ArrayToken Domain { get; }

        /// <summary>
        /// (Optional) An array of six numbers specifying a transformation matrix mapping the coordinate space specified by the Domain entry into the shading’s target coordinate space.
        /// Default value: the identity matrix [1 0 0 1 0 0].
        /// </summary>
        public ArrayToken Matrix { get;  }

        /*(Required) A 2-in, n-out function or an array of n 2-in, 1-out functions (where n is the number of colour components in the shading dictionary’s colour space). Each function’s domain shall be a superset of that of the shading dictionary. If the value returned by the function for a given colour component is out of range, it shall be adjusted to the nearest valid value.*/
        // function

        /// <inheritdoc/>
        public override int ShadingType => PdfShading.ShadingType1;

        /// <summary>
        /// <inheritdoc/>
        /// <para>Function based shading.</para>
        /// </summary>
        /// <param name="shadingDictionary"></param>
        /// <param name="pdfTokenScanner"></param>
        public PdfShadingType1(DictionaryToken shadingDictionary, IPdfTokenScanner pdfTokenScanner)
            : base(shadingDictionary, pdfTokenScanner)
        {
            if (shadingDictionary.TryGet<ArrayToken>(NameToken.Domain, pdfTokenScanner, out var domain))
            {
                Domain = domain;
            }

            if (shadingDictionary.TryGet<ArrayToken>(NameToken.Matrix, pdfTokenScanner, out var matrix))
            {
                Matrix = matrix;
            }

            if (shadingDictionary.TryGet<DictionaryToken>(NameToken.Function, pdfTokenScanner, out var functionDic))
            {
                Function = PdfFunction.Parse(functionDic, pdfTokenScanner);
            }
            else if (shadingDictionary.TryGet<StreamToken>(NameToken.Function, pdfTokenScanner, out var functionStr))
            {
                Function = PdfFunction.Parse(functionStr.StreamDictionary, pdfTokenScanner);
            }
            else
            {
                throw new ArgumentException("Function is Required.");
            }
        }
    }
}
