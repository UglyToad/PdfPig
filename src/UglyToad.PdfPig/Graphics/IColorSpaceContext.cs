namespace UglyToad.PdfPig.Graphics
{
    using Colors;
    using Tokens;
    using UglyToad.PdfPig.Core;

    /// <summary>
    /// Methods for manipulating and retrieving the current color state for a PDF content stream.
    /// </summary>
    public interface IColorSpaceContext : IDeepCloneable<IColorSpaceContext>
    {
        /// <summary>
        /// The <see cref="ColorSpace"/> used for stroking operations.
        /// </summary>
        ColorSpace CurrentStrokingColorSpace { get; }

        /// <summary>
        /// The <see cref="ColorSpace"/> used for non-stroking operations.
        /// </summary>
        ColorSpace CurrentNonStrokingColorSpace { get; }

        /// <summary>
        /// The name of the advanced ColorSpace active for stroking operations, if any.
        /// </summary>
        NameToken AdvancedStrokingColorSpace { get; }

        /// <summary>
        /// The name of the advanced ColorSpace active for non-stroking operations, if any.
        /// </summary>
        NameToken AdvancedNonStrokingColorSpace { get; }

        /// <summary>
        ///  Set the current color space to use for stroking operations.
        /// </summary>
        void SetStrokingColorspace(NameToken colorspace);

        /// <summary>
        ///  Set the current color space to use for nonstroking operations.
        /// </summary>
        void SetNonStrokingColorspace(NameToken colorspace);

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
