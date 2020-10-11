namespace UglyToad.PdfPig.Graphics.Shading
{
    using System;
    using UglyToad.PdfPig.Function;
    using UglyToad.PdfPig.Tokenization.Scanner;
    using UglyToad.PdfPig.Tokens;

    /// <summary>
    /// Resources for a lattice-form Gouraud-shaded triangle mesh shading.
    /// </summary>
    public class PdfShadingType5 : PdfShading
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
        /// (Required) The number of vertices in each row of the lattice; the value shall be greater than or equal to 2. The number of rows need not be specified.
        /// </summary>
        public int VerticesPerRow { get; } = -1;

        /// <summary>
        /// (Required) An array of numbers specifying how to map vertex coordinates and colour components into the appropriate ranges of values. The decoding method is similar to that used in image dictionaries (see 8.9.5.2, "Decode Arrays"). The ranges shall bespecified as follows:
        /// [xmin xmax ymin ymax c1, min c1, max … cn, min cn, max]
        /// Only one pair of c values shall be specified if a Function entry is present.
        /// </summary>
        public ArrayToken Decode { get; }

        /// <inheritdoc/>
        public override int ShadingType => ShadingType5;

        /// <summary>
        /// <inheritdoc/>
        /// <para>Lattice-form Gouraud-shaded triangle mesh.</para>
        /// </summary>
        /// <param name="shadingDictionary"></param>
        /// <param name="pdfTokenScanner"></param>
        public PdfShadingType5(DictionaryToken shadingDictionary, IPdfTokenScanner pdfTokenScanner)
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
                BitsPerComponent = bitsPerCoordinate.Int;
            }
            else
            {
                throw new ArgumentException("BitsPerComponent is Required.");
            }

            if (shadingDictionary.TryGet<NumericToken>(NameToken.VerticesPerRow, pdfTokenScanner, out var verticesPerRow))
            {
                VerticesPerRow = verticesPerRow.Int;
            }
            else
            {
                throw new ArgumentException("VerticesPerRow is Required.");
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
