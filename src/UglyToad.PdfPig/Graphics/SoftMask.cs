namespace UglyToad.PdfPig.Graphics
{
    using System;
    using System.Linq;
    using UglyToad.PdfPig.Filters;
    using UglyToad.PdfPig.Functions;
    using UglyToad.PdfPig.Tokenization.Scanner;
    using UglyToad.PdfPig.Tokens;
    using UglyToad.PdfPig.Util;

    /// <summary>
    /// Soft Mask.
    /// </summary>
    public sealed class SoftMask
    {
        /// <summary>
        /// (Required) A subtype specifying the method that shall be used in deriving the mask
        /// values from the transparency group specified by the G entry:
        /// <list type="bullet">
        /// <item>Alpha - The group's computed alpha shall be used, disregarding its colour.</item>
        /// <item>Luminosity - The group's computed colour shall be converted to a single-component luminosity value.</item>
        /// </list>
        /// </summary>
        public SoftMaskType Subtype { get; private set; }

        /// <summary>
        /// (Required) A transparency group XObject that shall be used as the source of alpha
        /// or colour values for deriving the mask. If the subtype S is Luminosity, the group
        /// attributes dictionary shall contain a CS entry defining the colour space in which
        /// the compositing computation is to be performed.
        /// </summary>
        public StreamToken TransparencyGroup { get; private set; }

        /// <summary>
        /// (Optional) An array of component values specifying the colour that shall be used
        /// as the backdrop against which to composite the transparency group XObject G.
        /// This entry shall be consulted only if the subtype S is Luminosity.
        /// The array shall consist of n numbers, where n is the number of components in the
        /// colour space specified by the CS entry in the group attributes dictionary.
        /// <para>
        /// Default value: the colour space’s initial value, representing black.
        /// </para>
        /// </summary>
        public double[]? BC { get; private set; }

        /// <summary>
        /// (Optional) A function object (see 7.10, "Functions") specifying the transfer
        /// function that shall be used in deriving the mask values.The function shall
        /// accept one input, the computed group alpha or luminosity (depending on the
        /// value of the subtype S), and shall return one output, the resulting mask
        /// value.The input shall be in the range 0.0 to 1.0. The computed output shall
        /// be in the range 0.0 to 1.0; if it falls outside this range, it shall be forced
        /// to the nearest valid value.The name Identity may be specified in place of a
        /// function object to designate the identity function.
        /// <para>
        /// Default value: Identity.
        /// </para>
        /// </summary>
        public PdfFunction? TransferFunction { get; private set; }

        internal static SoftMask Parse(DictionaryToken dictionaryToken, IPdfTokenScanner pdfTokenScanner, ILookupFilterProvider filterProvider)
        {
            if (dictionaryToken == null)
            {
                throw new ArgumentNullException(nameof(dictionaryToken));
            }

            var softMask = new SoftMask();

            if (!dictionaryToken.TryGet(NameToken.S, pdfTokenScanner, out NameToken? s))
            {
                /*
                 * (Required) A subtype specifying the method that shall be used in deriving
                 * the mask values from the transparency group specified by the G entry: Alpha
                 * The group’s computed alpha shall be used, disregarding its colour (see 11.5.2,
                 * "Deriving a soft mask from group alpha"). Luminosity The group’s computed
                 * colour shall be converted to a single-component luminosity value (see 11.5.3,
                 * "Deriving a soft mask from group luminosity").
                 */
                throw new Exception($"Missing soft-mask dictionary '{NameToken.S}' entry.");
            }

            if (s.Equals(NameToken.Luminosity))
            {
                softMask.Subtype = SoftMaskType.Luminosity;
            }
            else if (s.Equals(NameToken.Alpha))
            {
                softMask.Subtype = SoftMaskType.Alpha;
            }
            else
            {
                throw new Exception($"Invalid soft-mask Subtype '{s}' entry.");
            }

            if (!dictionaryToken.TryGet(NameToken.G, pdfTokenScanner, out StreamToken g))
            {
                /*
                 * (Required) A transparency group XObject (see 11.6.6, "Transparency group
                 * XObjects") that shall be used as the source of alpha or colour values for
                 * deriving the mask. If the subtype S is Luminosity, the group attributes
                 * dictionary shall contain a CS entry defining the colour space in which
                 * the compositing computation is to be performed.
                 */
                throw new Exception($"Missing soft-mask dictionary '{NameToken.G}' entry.");
            }

            softMask.TransparencyGroup = g;

            if (dictionaryToken.TryGet(NameToken.Bc, pdfTokenScanner, out ArrayToken bc))
            {
                /*
                 * (Optional) An array of component values specifying the colour that shall
                 * be used as the backdrop against which to composite the transparency group
                 * XObject G. This entry shall be consulted only if the subtype S is Luminosity.
                 * The array shall consist of n numbers, where n is the number of components in
                 * the colour space specified by the CS entry in the group attributes dictionary
                 * (see 11.6.6, "Transparency group XObjects"). Default value: the colour space’s
                 * initial value, representing black.
                 */
                softMask.BC = bc.Data.OfType<NumericToken>().Select(x => x.Data).ToArray();
            }

            if (dictionaryToken.TryGet(NameToken.Tr, pdfTokenScanner, out NameToken trName))
            {
                /*
                 * (Optional) A function object (see 7.10, "Functions") specifying the transfer
                 * function that shall be used in deriving the mask values. The function shall
                 * accept one input, the computed group alpha or luminosity (depending on the
                 * value of the subtype S), and shall return one output, the resulting mask
                 * value. The input shall be in the range 0.0 to 1.0. The computed output shall
                 * be in the range 0.0 to 1.0; if it falls outside this range, it shall be forced
                 * to the nearest valid value. The name Identity may be specified in place of a
                 * function object to designate the identity function. Default value: Identity
                 */
                if (!trName.Equals(NameToken.Identity))
                {
                    throw new Exception($"Invalid transfer function name '{trName}' entry, should be '{NameToken.Identity}'.");
                }
            }
            else if (dictionaryToken.TryGet(NameToken.Tr, pdfTokenScanner, out IToken? trFunction))
            {
                softMask.TransferFunction = PdfFunctionParser.Create(trFunction, pdfTokenScanner, filterProvider);
            }

            return softMask;
        }
    }

    /// <summary>
    /// The soft mask type.
    /// <para>Alpha or Luminosity.</para>
    /// </summary>
    public enum SoftMaskType : byte
    {
        /// <summary>
        /// Alpha - The group's computed alpha shall be used, disregarding its colour.
        /// </summary>
        Alpha = 0,

        /// <summary>
        /// Luminosity - The group's computed colour shall be converted to a single-component luminosity value.
        /// </summary>
        Luminosity = 1
    }
}
