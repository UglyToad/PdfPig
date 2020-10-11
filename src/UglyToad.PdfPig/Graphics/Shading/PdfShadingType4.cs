namespace UglyToad.PdfPig.Graphics.Shading
{
    using System;
    using UglyToad.PdfPig.Function;
    using UglyToad.PdfPig.Tokenization.Scanner;
    using UglyToad.PdfPig.Tokens;

    /// <summary>
    /// Resources for a free-form Gouraud-shaded triangle mesh shading.
    /// </summary>
    public class PdfShadingType4 : PdfShading
    {
        /// <summary>
        /// (Required) The number of bits used to represent each vertex coordinate. The value shall be 1, 2, 4, 8, 12, 16, 24, or 32.
        /// </summary>
        public int BitsPerCoordinate { get; } = -1;

        /// <summary>
        /// (Required) The number of bits used to represent each colour component. The value shall be 1, 2, 4, 8, 12, or 16.
        /// </summary>
        public int BitsPerComponent { get; } = -1;

        /// <summary>
        /// (Required) The number of bits used to represent the edge flag for each vertex (see below). The value of BitsPerFlag shall be 2, 4, or8, but only the least significant 2 bits in each flag value shall beused. The value for the edge flag shall be 0, 1, or 2.
        /// </summary>
        public int BitsPerFlag { get; } = -1;

        /// <summary>
        /// (Required) An array of numbers specifying how to map vertex coordinates and colour components into the appropriate ranges of values. The decoding method is similar to that used in image dictionaries (see 8.9.5.2, "Decode Arrays"). The ranges shall bespecified as follows:
        /// [xmin xmax ymin ymax c1, min c1, max … cn, min cn, max]
        /// Only one pair of c values shall be specified if a Function entry is present.
        /// </summary>
        public ArrayToken Decode { get; }

        /*(Optional) A 1-in, n-out function or an array of n 1-in, 1-out functions (where n is the number of colour components in the shading dictionary’s colour space). If this entry is present, the colour data for each vertex shall be specified by a single parametric variable rather than by n separate colour components. The designated function(s) shall be called with each interpolated value of the parametric variable to determine the actual colour at each point. Each input value shall be forced into the range interval specified for the corresponding colour component in the shading dictionary’s Decode array. Each function’s domain shall be a superset of that interval. If the value returned by the function for a given colour component is out of range, it shall be adjusted to the nearest valid value.
This entry shall not be used with an Indexed colour space.*/
        // Func


        /// <inheritdoc/>
        public override int ShadingType => ShadingType4;

        /// <summary>
        /// <inheritdoc/>
        /// <para>Free-form Gouraud-shaded triangle mesh shading.</para>
        /// </summary>
        /// <param name="shadingDictionary"></param>
        /// <param name="pdfTokenScanner"></param>
        public PdfShadingType4(DictionaryToken shadingDictionary, IPdfTokenScanner pdfTokenScanner)
            : base(shadingDictionary, pdfTokenScanner)
        {
            if (shadingDictionary.TryGet<NumericToken>(NameToken.BitsPerCoordinate, pdfTokenScanner, out var bitsPerCoordinate))
            {
                BitsPerCoordinate = bitsPerCoordinate.Int;
            }
            else
            {
                throw new ArgumentException("BitsPerCoordinate is Required.");
            }

            if (shadingDictionary.TryGet<NumericToken>(NameToken.BitsPerComponent, pdfTokenScanner, out var bitsPerComponent))
            {
                BitsPerComponent = bitsPerComponent.Int;
            }
            else
            {
                throw new ArgumentException("BitsPerComponent is Required.");
            }

            if (shadingDictionary.TryGet<NumericToken>(NameToken.BitsPerFlag, pdfTokenScanner, out var bitsPerFlag))
            {
                BitsPerFlag = bitsPerFlag.Int;
            }
            else
            {
                throw new ArgumentException("BitsPerFlag is Required.");
            }

            if (shadingDictionary.TryGet<ArrayToken>(NameToken.Decode, pdfTokenScanner, out var decode))
            {
                Decode = decode;
            }
            else
            {
                throw new ArgumentException("Decode is Required.");
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
