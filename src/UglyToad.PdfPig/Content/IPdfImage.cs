namespace UglyToad.PdfPig.Content
{
    using System.Collections.Generic;
    using Geometry;
    using Graphics.Colors;
    using Graphics.Core;
    using XObjects;

    /// <summary>
    /// An image in a PDF document, may be an <see cref="InlineImage"/> or a PostScript image XObject (<see cref="XObjectImage"/>).
    /// </summary>
    public interface IPdfImage
    {
        /// <summary>
        /// The placement rectangle of the image in PDF coordinates.
        /// </summary>
        PdfRectangle Bounds { get; }

        /// <summary>
        /// The width of the image in samples.
        /// </summary>
        int WidthInSamples { get; }

        /// <summary>
        /// The height of the image in samples.
        /// </summary>
        int HeightInSamples { get; }

        /// <summary>
        /// The <see cref="ColorSpace"/> used to interpret the image.
        /// This defines the number of color components per sample, e.g.
        /// 1 component for <see cref="Graphics.Colors.ColorSpace.DeviceGray"/>,
        /// 3 components for <see cref="Graphics.Colors.ColorSpace.DeviceRGB"/>,
        /// 4 components for <see cref="Graphics.Colors.ColorSpace.DeviceCMYK"/>,
        /// etc.
        /// This is not defined where <see cref="IsImageMask"/> is <see langword="true"/> and is optional where the image is JPXEncoded for <see cref="XObjectImage"/>.
        /// </summary>
        ColorSpace? ColorSpace { get; }

        /// <summary>
        /// The number of bits used to represent each color component.
        /// </summary>
        int BitsPerComponent { get; }

        /// <summary>
        /// The bytes of the image with any filters decoded.
        /// If the filter used to encode the bytes is not supported accessing this property will throw, access the <see cref="RawBytes"/>
        /// instead.
        /// </summary>
        IReadOnlyList<byte> Bytes { get; }

        /// <summary>
        /// The encoded bytes of the image with all filters still applied.
        /// </summary>
        IReadOnlyList<byte> RawBytes { get; }

            /// <summary>
        /// The color rendering intent to be used when rendering the image.
        /// </summary>
        RenderingIntent RenderingIntent { get; }

        /// <summary>
        /// Indicates whether the image is to be treated as an image mask.
        /// If <see langword="true"/> the image is a monochrome image in which each sample
        /// is specified by a single bit (<see cref="BitsPerComponent"/> is 1).
        /// The image represents a stencil where sample values represent places on the page
        /// that should be marked with the current color or masked (not marked).
        /// </summary>
        bool IsImageMask { get; }

        /// <summary>
        /// Describes how to map image samples into the values appropriate for the
        /// <see cref="ColorSpace"/>.
        /// The image data is initially composed of values in the range 0 to 2^n - 1
        /// where n is <see cref="BitsPerComponent"/>.
        /// The decode array contains a pair of numbers for each component in the <see cref="ColorSpace"/>.
        /// The value from the image data is then interpolated into the values relevant to the <see cref="ColorSpace"/>
        /// using the corresponding values of the decode array.
        /// </summary>
        IReadOnlyList<decimal> Decode { get; }

        /// <summary>
        /// Specifies whether interpolation is to be performed. Interpolation smooths images where a single component in the image
        /// as defined may correspond to many pixels on the output device. The interpolation algorithm is implementation
        /// dependent and is not defined by the specification.
        /// </summary>
        bool Interpolate { get; }

        /// <summary>
        /// Whether this image is an <see cref="InlineImage"/> or a <see cref="XObjectImage"/>.
        /// </summary>
        bool IsInlineImage { get; }
    }
}
