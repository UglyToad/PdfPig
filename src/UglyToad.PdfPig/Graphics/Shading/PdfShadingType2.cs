namespace UglyToad.PdfPig.Graphics.Shading
{
    using System;
    using System.Linq;
    using UglyToad.PdfPig.Function;
    using UglyToad.PdfPig.Tokenization.Scanner;
    using UglyToad.PdfPig.Tokens;

    /// <summary>
    /// Resources for an axial shading.
    /// </summary>
    public class PdfShadingType2 : PdfShading
    {
        /// <summary>
        /// (Required) An array of four numbers [x0 y0 x1 y1] specifying the starting and ending coordinates of the axis, expressed in the shading’s target coordinate space.
        /// </summary>
        public double[] Coords { get; }

        /// <summary>
        /// (Optional) An array of two numbers [t0 t1] specifying the limiting values of a parametric variable t. The variable is considered to vary linearly between these 
        /// two values as the colour gradient varies between the starting and ending points of the axis. The variable t becomes the input argument to the colour function(s). Default value: [0.0 1.0].
        /// </summary>
        public double[] Domain { get; } = new double[] { 0.0, 1.0 };

        /*(Required) A 1-in, n-out function or an array of n 1-in, 1-out functions (where n is the number of colour components in the shading dictionary’s colour space). The function(s) shall be called with values of the parametric variable t in the domain defined by the Domain entry. Each function’s domain shall be a superset of that of the shading dictionary. If the value returned by the function for a given colour component is out of range, it shall be adjusted to the nearest valid value.*/
        // Function

        /// <summary>
        /// (Optional) An array of two boolean values specifying whether to extend the shading beyond the starting and ending points of the axis, respectively. Default value: [false false].
        /// </summary>
        public bool[] Extend { get; } = new bool[] { false, false };

        /// <inheritdoc/>
        public override int ShadingType => ShadingType2;

        /// <summary>
        /// <inheritdoc/>
        /// <para>Axial shading.</para>
        /// </summary>
        /// <param name="shadingDictionary"></param>
        /// <param name="pdfTokenScanner"></param>
        public PdfShadingType2(DictionaryToken shadingDictionary, IPdfTokenScanner pdfTokenScanner)
            : base(shadingDictionary, pdfTokenScanner)
        {
            if (shadingDictionary.TryGet<ArrayToken>(NameToken.Coords, pdfTokenScanner, out var coords))
            {
                Coords = coords.Data.Select(x => ((NumericToken)x).Double).ToArray();
            }
            else
            {
                throw new ArgumentException("Coords is Required.");
            }

            if (shadingDictionary.TryGet<ArrayToken>(NameToken.Domain, pdfTokenScanner, out var domain))
            {
                Domain = domain.Data.Select(x => ((NumericToken)x).Double).ToArray();
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

            if (shadingDictionary.TryGet<ArrayToken>(NameToken.Extend, pdfTokenScanner, out var extend))
            {
                Extend = extend.Data.Select(x => ((BooleanToken)x).Data).ToArray();
            }
        }
    }
}
