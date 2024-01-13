namespace UglyToad.PdfPig.Images
{
    /// <summary>
    /// Information read from a JPEG image.
    /// </summary>
    internal class JpegInformation
    {
        /// <summary>
        /// Width of the image in pixels.
        /// </summary>
        public int Width { get; }

        /// <summary>
        /// Height of the image in pixels.
        /// </summary>
        public int Height { get; }

        /// <summary>
        /// Bits per component.
        /// </summary>
        public int BitsPerComponent { get; }

        /// <summary>
        /// 1 grayscale, 3 RGB, 4 CMYK.
        /// </summary>
        public int NumberOfComponents { get; }

        /// <summary>
        /// Create a new <see cref="JpegInformation"/>.
        /// </summary>
        public JpegInformation(int width, int height, int bitsPerComponent, int numberOfComponents)
        {
            Width = width;
            Height = height;
            BitsPerComponent = bitsPerComponent;
            NumberOfComponents = numberOfComponents;
        }
    }
}