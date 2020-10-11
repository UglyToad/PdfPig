namespace UglyToad.PdfPig.Graphics.Shading
{
    using System;
    using UglyToad.PdfPig.Function;
    using UglyToad.PdfPig.Tokenization.Scanner;
    using UglyToad.PdfPig.Tokens;

    /// <summary>
    /// Resources for a coons patch mesh shading.
    /// </summary>
    public class PdfShadingType6 : PdfShading
    {
        /// <summary>
        /// (Required) The number of bits used to represent each geometric coordinate.
        /// The value shall be 1, 2, 4, 8, 12, 16, 24, or 32.
        /// </summary>
        public int BitsPerCoordinate { get; } = -1;

        /// <summary>
        /// (Required) The number of bits used to represent each colour component.
        /// The value shall be 1, 2, 4, 8, 12, or 16.
        /// </summary>
        public int BitsPerComponent { get; } = -1;

        /// <summary>
        /// (Required) The number of bits used to represent the edge flag for each patch (see below). The value shall be 2, 4, or 8, but only the least significant 2 bits in each flag value shall be used.
        /// Valid values for the edge flag shall be 0, 1, 2, and 3.
        /// </summary>
        public int BitsPerFlag { get; } = -1;

        /// <summary>
        /// (Required) An array of numbers specifying how to map coordinates and colour components into the appropriate ranges of values. The decoding method is similar to that used in image dictionaries (see 8.9.5.2, "Decode Arrays"). The ranges shall be specified as follows:
        /// [xmin xmax ymin ymax c1, min c1, max … cn, min cn, max]
        /// Only one pair of c values shall be specified if a Function entry is present.
        /// </summary>
        public ArrayToken Decode { get; }

        /// <inheritdoc/>
        public override int ShadingType => ShadingType6;

        /// <summary>
        /// <inheritdoc/>
        /// <para>Coons patch mesh.</para>
        /// </summary>
        /// <param name="shadingDictionary"></param>
        /// <param name="pdfTokenScanner"></param>
        public PdfShadingType6(DictionaryToken shadingDictionary, IPdfTokenScanner pdfTokenScanner)
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
        }
    }
}
