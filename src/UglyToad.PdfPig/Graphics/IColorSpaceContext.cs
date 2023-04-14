namespace UglyToad.PdfPig.Graphics
{
    using Colors;
    using System.Collections.Generic;
    using Tokens;
    using UglyToad.PdfPig.Core;

    /// <summary>
    /// Methods for manipulating and retrieving the current color state for a PDF content stream.
    /// </summary>
    public interface IColorSpaceContext : IDeepCloneable<IColorSpaceContext>
    {
        /// <summary>
        /// The <see cref="ColorSpaceDetails"/> used for stroking operations.
        /// </summary>
        ColorSpaceDetails CurrentStrokingColorSpace { get; }

        /// <summary>
        /// The <see cref="ColorSpaceDetails"/> used for non-stroking operations.
        /// </summary>
        ColorSpaceDetails CurrentNonStrokingColorSpace { get; }

        /// <summary>
        /// Set the current color space to use for stroking operations and initialize the stroking color.
        /// </summary>
        /// <param name="colorspace">The color space name.</param>
        /// <param name="dictionary">The color space dictionary. Default value is null.</param>
        void SetStrokingColorspace(NameToken colorspace, DictionaryToken dictionary = null);

        /// <summary>
        /// Set the current color space to use for nonstroking operations and initialize the nonstroking color.
        /// </summary>
        /// <param name="colorspace">The color space name.</param>
        /// <param name="dictionary">The color space dictionary. Default value is null.</param>
        void SetNonStrokingColorspace(NameToken colorspace, DictionaryToken dictionary = null);

        /// <summary>
        /// Set the color to use for stroking operations using the current color space.
        /// </summary>
        void SetStrokingColor(IReadOnlyList<decimal> operands, NameToken patternName = null);

        /// <summary>
        /// Set the stroking color space to DeviceGray and set the gray level to use for stroking operations.
        /// </summary>
        /// <param name="gray">A number between 0.0 (black) and 1.0 (white).</param>
        void SetStrokingColorGray(decimal gray);

        /// <summary>
        /// Set the stroking color space to DeviceRGB and set the color to use for stroking operations.
        /// </summary>
        /// <param name="r">Red - A number between 0 (minimum intensity) and 1 (maximum intensity).</param>
        /// <param name="g">Green - A number between 0 (minimum intensity) and 1 (maximum intensity).</param>
        /// <param name="b">Blue - A number between 0 (minimum intensity) and 1 (maximum intensity).</param>
        void SetStrokingColorRgb(decimal r, decimal g, decimal b);

        /// <summary>
        /// Set the stroking color space to DeviceCMYK and set the color to use for stroking operations. 
        /// </summary>
        /// <param name="c">Cyan - A number between 0 (minimum concentration) and 1 (maximum concentration).</param>
        /// <param name="m">Magenta - A number between 0 (minimum concentration) and 1 (maximum concentration).</param>
        /// <param name="y">Yellow - A number between 0 (minimum concentration) and 1 (maximum concentration).</param>
        /// <param name="k">Black - A number between 0 (minimum concentration) and 1 (maximum concentration).</param>
        void SetStrokingColorCmyk(decimal c, decimal m, decimal y, decimal k);

        /// <summary>
        /// Set the color to use for nonstroking operations using the current color space.
        /// </summary>
        void SetNonStrokingColor(IReadOnlyList<decimal> operands, NameToken patternName = null);

        /// <summary>
        /// Set the nonstroking color space to DeviceGray and set the gray level to use for nonstroking operations.
        /// </summary>
        /// <param name="gray">A number between 0.0 (black) and 1.0 (white).</param>
        void SetNonStrokingColorGray(decimal gray);

        /// <summary>
        /// Set the nonstroking color space to DeviceRGB and set the color to use for nonstroking operations.
        /// </summary>
        /// <param name="r">Red - A number between 0 (minimum intensity) and 1 (maximum intensity).</param>
        /// <param name="g">Green - A number between 0 (minimum intensity) and 1 (maximum intensity).</param>
        /// <param name="b">Blue - A number between 0 (minimum intensity) and 1 (maximum intensity).</param>
        void SetNonStrokingColorRgb(decimal r, decimal g, decimal b);

        /// <summary>
        /// Set the nonstroking color space to DeviceCMYK and set the color to use for nonstroking operations.
        /// </summary>
        /// <param name="c">Cyan - A number between 0 (minimum concentration) and 1 (maximum concentration).</param>
        /// <param name="m">Magenta - A number between 0 (minimum concentration) and 1 (maximum concentration).</param>
        /// <param name="y">Yellow - A number between 0 (minimum concentration) and 1 (maximum concentration).</param>
        /// <param name="k">Black - A number between 0 (minimum concentration) and 1 (maximum concentration).</param>
        void SetNonStrokingColorCmyk(decimal c, decimal m, decimal y, decimal k);
    }
}
