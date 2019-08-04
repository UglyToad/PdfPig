namespace UglyToad.PdfPig.Graphics.Colors
{
    /// <summary>
    /// <see cref="ColorSpace"/>s can be classified into colorspace families.
    /// <see cref="ColorSpace"/>s within the same family share general characteristics.
    /// </summary>
    public enum ColorSpaceFamily
    {
        /// <summary>
        /// Device colorspaces directly specify colors or shades of gray that the output device
        /// should produce.
        /// </summary>
        Device,
        /// <summary>
        /// CIE-based color spaces are based on an international standard for color specification created by
        /// the Commission Internationale de l'Éclairage (International Commission on Illumination) (CIE).
        /// These spaces specify colors in a way that is independent of the characteristics of any particular output device.
        /// </summary>
        CIEBased,
        /// <summary>
        /// Special color spaces add features or properties to an underlying color space.
        /// They include facilities for patterns, color mapping, separations, and high-fidelity and multitone color. 
        /// </summary>
        Special
    }
}