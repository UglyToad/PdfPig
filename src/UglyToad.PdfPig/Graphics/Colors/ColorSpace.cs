namespace UglyToad.PdfPig.Graphics.Colors
{
    /// <summary>
    /// Color values in a PDF are interpreted according to the current color space.
    /// Color spaces enable a PDF to specify abstract colors in a device independent way.
    /// </summary>
    public enum ColorSpace
    {
        /// <summary>
        /// Grayscale. Controls the intensity of achromatic light on a scale from black to white.
        /// </summary>
        DeviceGray = 0,
        /// <summary>
        /// RGB. Controls the intensities of red, green and blue light.
        /// </summary>
        DeviceRGB = 1,
        /// <summary>
        /// CMYK. Controls the concentrations of cyan, magenta, yellow and black (K) inks.
        /// </summary>
        DeviceCMYK = 2,
        /// <summary>
        /// CIE (Commission Internationale de l'Éclairage) colorspace.
        /// Specifies color related to human visual perception with the aim of producing consistent color on different output devices.
        /// CalGray - Special case of the CIE colorspace using a single channel (A) and a single transformation.
        /// A represents the gray component of a calibrated gray space in the range 0 to 1.
        /// </summary>
        CalGray = 3,
        /// <summary>
        /// CIE (Commission Internationale de l'Éclairage) colorspace.
        /// Specifies color related to human visual perception with the aim of producing consistent color on different output devices.
        /// CalRGB - A CIE ABC color space with a single transformation.
        /// A, B and C represent red, green and blue color values in the range 0 to 1. 
        /// </summary>
        CalRGB = 4,
        /// <summary>
        /// CIE (Commission Internationale de l'Éclairage) colorspace.
        /// Specifies color related to human visual perception with the aim of producing consistent color on different output devices.
        /// Lab - A CIE ABC color space with two transforms. A, B and C represent the L*, a* and b*
        /// components of a CIE 1976 L*a*b* space. The range of A (L*) is 0 to 100.
        /// The range of B (a*) and C (b*) are defined by the Range of the color space.
        /// </summary>
        Lab = 5,
        /// <summary>
        /// ICC (International Color Consortium) colorspace.
        /// ICC - Colorspace specified by a sequence of bytes which are interpreted according to the
        /// ICC specification.
        /// </summary>
        ICCBased = 6,
        /// <summary>
        /// An Indexed color space allows a PDF content stream to use small integers as indices into a color map or color table of arbitrary colors in some other space. 
        /// A PDF consumer application treats each sample value as an index into the color table and uses the color value it finds there. 
        /// </summary>
        Indexed = 7,
        /// <summary>
        /// Enables a PDF content stream to paint an area with a pattern rather than a single color.
        /// The pattern may be either a tiling pattern (type 1) or a shading pattern (type 2).
        /// </summary>
        Pattern = 8,
        /// <summary>
        /// Provides a means for specifying the use of additional colorants or for isolating the control of individual color components of
        /// a device color space for a subtractive device.
        /// When such a space is the current color space, the current color is a single-component value, called a tint,
        /// that controls the application of the given colorant or color components only. 
        /// </summary>
        Separation = 9,
        /// <summary>
        /// Can contain an arbitrary number of color components. They provide greater flexibility than is possible with standard device color
        /// spaces such as <see cref="DeviceCMYK"/> or with individual <see cref="Separation"/> color spaces.
        /// For example, it is possible to create a DeviceN color space consisting of only the cyan, magenta, and yellow color components,
        /// with the black component excluded. 
        /// </summary>
        DeviceN = 10
    }
}
