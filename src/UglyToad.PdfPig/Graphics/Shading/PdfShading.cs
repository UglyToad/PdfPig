using System;
using System.Collections.Generic;
using System.Text;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Function;
using UglyToad.PdfPig.Graphics.Colors;
using UglyToad.PdfPig.Tokenization.Scanner;
using UglyToad.PdfPig.Tokens;

namespace UglyToad.PdfPig.Graphics.Shading
{
    /// <summary>
    /// A Shading Resource.
    /// </summary>
    public abstract class PdfShading
    {
        /// <summary>
        /// Gets the underlying dictionary.
        /// </summary>
        public DictionaryToken Dictionary { get; private set; }

        /// <summary>
        /// (Optional) An array of colour components appropriate to the colour space, specifying a single background colour value.
        /// If present, this colour shall be used, before any painting operation involving the shading, to fill those portions of the area to be painted that lie outside the bounds of the shading object.
        /// </summary>
        public ArrayToken Background { get; protected set; }

        /// <summary>
        /// (Optional) An array of four numbers giving the left, bottom, right, and top coordinates, respectively, of the shading’s bounding box. The coordinates shall be interpreted in the shading’s target coordinate space. If present, this bounding box shall be applied as a temporary clipping boundary when the shading is painted, in addition to the current clipping path and any other clipping boundaries in effect at that time.
        /// </summary>
        public PdfRectangle? BBox { get; protected set; }

        /// <summary>
        /// (Required) The colour space in which colour values shall beexpressed. This may be any device, CIE-based, or special colour space except a Pattern space. See 8.7.4.4, "Colour Space: Special Considerations" for further information.
        /// </summary>
        public ColorSpace? ColorSpace { get; protected set; }

        /// <summary>
        /// f
        /// </summary>
        public PdfFunction Function { get; protected set; }

        /// <summary>
        /// f
        /// </summary>
        public PdfFunction[] FunctionArray { get; protected set; }


        /// <summary>
        /// (Required) The shading type:
        /// <para>1 Function-based shading</para>
        /// <para>2 Axial shading</para>
        /// <para>3 Radial shading</para>
        /// <para>4 Free-form Gouraud-shaded triangle mesh</para>
        /// <para>5 Lattice-form Gouraud-shaded triangle mesh</para>
        /// <para>6 Coons patch mesh</para>
        /// <para>7 Tensor-product patch mesh</para>
        /// </summary>
        /// <returns>The shading type.</returns>
        public abstract int ShadingType { get; }

        /// <summary>
        /// (Optional) A flag indicating whether to filter the shading function to prevent aliasing artifacts
        /// </summary>
        public bool AntiAlias { get; protected set; }

        /// <summary>
        /// shading type 1 = function based shading.
        /// </summary>
        public const int ShadingType1 = 1;

        /// <summary>
        /// shading type 2 = axial shading.
        /// </summary>
        public const int ShadingType2 = 2;

        /// <summary>
        /// shading type 3 = radial shading.
        /// </summary>
        public const int ShadingType3 = 3;

        /// <summary>
        /// shading type 4 = Free-Form Gouraud-Shaded Triangle Meshes.
        /// </summary>
        public const int ShadingType4 = 4;

        /// <summary>
        /// shading type 5 = Lattice-Form Gouraud-Shaded Triangle Meshes.
        /// </summary>
        public const int ShadingType5 = 5;

        /// <summary>
        /// shading type 6 = Coons Patch Meshes.
        /// </summary>
        public const int ShadingType6 = 6;

        /// <summary>
        /// shading type 7 = Tensor-Product Patch Meshes.
        /// </summary>
        public const int ShadingType7 = 7;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public PdfShading()
        { }

        /// <summary>
        /// Constructor using the given shading dictionary.
        /// </summary>
        /// <param name="shadingDictionary">shadingDictionary the dictionary for this shading</param>
        /// <param name="pdfTokenScanner"></param>
        public PdfShading(DictionaryToken shadingDictionary, IPdfTokenScanner pdfTokenScanner)
        {
            Dictionary = shadingDictionary;

            if (shadingDictionary.TryGet<NameToken>(NameToken.ColorSpace, pdfTokenScanner, out var colorSpaceNameToken))
            {
                // to do
            }
            else if (shadingDictionary.TryGet<ArrayToken>(NameToken.ColorSpace, pdfTokenScanner, out var colorSpaceArrayToken))
            {
                // to do
            }
            else
            {
                throw new ArgumentException("ColorSpace is Required.");
            }

            if (shadingDictionary.TryGet<ArrayToken>(NameToken.Background, pdfTokenScanner, out var background))
            {
                this.Background = background;
            }

            if (shadingDictionary.TryGet<ArrayToken>(NameToken.Bbox, pdfTokenScanner, out var bbox))
            {
                this.BBox = new PdfRectangle(((NumericToken)bbox[0]).Double, ((NumericToken)bbox[1]).Double, ((NumericToken)bbox[2]).Double, ((NumericToken)bbox[3]).Double);
            }

            if (shadingDictionary.TryGet<BooleanToken>(NameToken.AntiAlias, pdfTokenScanner, out var antiAlias))
            {
                this.AntiAlias = antiAlias.Data;
            }
        }

        /// <summary>
        /// Parse shading dictionary.
        /// </summary>
        /// <param name="shadingDictionary"></param>
        /// <param name="pdfTokenScanner"></param>
        public static PdfShading Parse(DictionaryToken shadingDictionary, IPdfTokenScanner pdfTokenScanner)
        {
            if (shadingDictionary.TryGet<NumericToken>(NameToken.ShadingType, pdfTokenScanner, out var shadingType))
            {
                switch (shadingType.Int)
                {
                    case ShadingType1:
                        return new PdfShadingType1(shadingDictionary, pdfTokenScanner);

                    case ShadingType2:
                        return new PdfShadingType2(shadingDictionary, pdfTokenScanner);

                    case ShadingType3:
                        return new PdfShadingType3(shadingDictionary, pdfTokenScanner);

                    case ShadingType4:
                        return new PdfShadingType4(shadingDictionary, pdfTokenScanner);

                    case ShadingType5:
                        return new PdfShadingType5(shadingDictionary, pdfTokenScanner);

                    case ShadingType6:
                        return new PdfShadingType6(shadingDictionary, pdfTokenScanner);

                    case ShadingType7:
                        return new PdfShadingType7(shadingDictionary, pdfTokenScanner);

                    default:
                        throw new ArgumentException($"Error: Unknown shading type {shadingType}");
                }
            }
            else
            {
                throw new ArgumentException("ShadingType is Required.");
            }
        }
    }
}
