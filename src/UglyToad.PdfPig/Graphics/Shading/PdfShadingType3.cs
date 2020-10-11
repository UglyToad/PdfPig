namespace UglyToad.PdfPig.Graphics.Shading
{
    using System;
    using UglyToad.PdfPig.Function;
    using UglyToad.PdfPig.Tokenization.Scanner;
    using UglyToad.PdfPig.Tokens;

    /// <summary>
    /// Resources for a radial shading.
    /// </summary>
    public class PdfShadingType3 : PdfShading
    {
        /// <summary>
        /// (Required) An array of six numbers [x0 y0 r0 x1 y1 r1] specifying the centres and radii of the starting and ending circles, expressed in the shading’s target coordinate space. The radii r0 and r1 shall both be greater than or equal to 0. If one radius is 0, the corresponding circle shall be treated as a point; if both are 0, nothing shall be painted.
        /// </summary>
        public ArrayToken Coords { get; private set; }

        /// <summary>
        /// (Optional) An array of two numbers [t0 t1] specifying the limiting values of a parametric variable t. The variable is considered to vary linearly between these two values as the colour gradient varies between the starting and ending circles. The variable t becomes the input argument to the colour function(s). Default value: [0.0 1.0].
        /// </summary>
        public ArrayToken Domain { get; private set; }

        /*(Required) A 1-in, n-out function or an array of n 1-in, 1-out functions (where n is the number of colour components in the shading dictionary’s colour space). The function(s) shall be called with values of the parametric variable t in the domain defined by the shading dictionary’s Domain entry. Each function’s domain shall be a superset of that of the shading dictionary. If the value returned by the function for a given colour component is out of range, it shall be adjusted to the nearest valid value.*/
        //Func

        /// <summary>
        /// (Optional) An array of two boolean values specifying whether to extend the shading beyond the starting and ending circles, respectively. Default value: [false false].
        /// </summary>
        public ArrayToken Extend { get; private set; }

        /// <inheritdoc/>
        public override int ShadingType => ShadingType3;

        /// <summary>
        /// <inheritdoc/>
        /// <para>Function based shading.</para>
        /// </summary>
        /// <param name="shadingDictionary"></param>
        /// <param name="pdfTokenScanner"></param>
        public PdfShadingType3(DictionaryToken shadingDictionary, IPdfTokenScanner pdfTokenScanner)
            : base(shadingDictionary, pdfTokenScanner)
        {
            if (shadingDictionary.TryGet<ArrayToken>(NameToken.Coords, pdfTokenScanner, out var coords))
            {
                Coords = coords;
            }

            if (shadingDictionary.TryGet<ArrayToken>(NameToken.Domain, pdfTokenScanner, out var domain))
            {
                Domain = domain;
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
                Extend = extend;
            }
        }
    }
}
