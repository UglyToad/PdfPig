namespace UglyToad.PdfPig.Images.Png
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// The high level information about the image.
    /// </summary>
    internal readonly struct ImageHeader
    {
        internal static ReadOnlySpan<byte> HeaderBytes => [73, 72, 68, 82];

        private static readonly IReadOnlyDictionary<ColorType, HashSet<byte>> PermittedBitDepths = new Dictionary<ColorType, HashSet<byte>>
        {
            {ColorType.None, [1, 2, 4, 8, 16]},
            {ColorType.ColorUsed, [8, 16]},
            {ColorType.PaletteUsed | ColorType.ColorUsed, [1, 2, 4, 8]},
            {ColorType.AlphaChannelUsed, [8, 16]},
            {ColorType.AlphaChannelUsed | ColorType.ColorUsed, [8, 16]},
        };

        /// <summary>
        /// The width of the image in pixels.
        /// </summary>
        public int Width { get; }

        /// <summary>
        /// The height of the image in pixels.
        /// </summary>
        public int Height { get; }

        /// <summary>
        /// The bit depth of the image.
        /// </summary>
        public byte BitDepth { get; }

        /// <summary>
        /// The color type of the image.
        /// </summary>
        public ColorType ColorType { get; }

        /// <summary>
        /// The compression method used for the image.
        /// </summary>
        public CompressionMethod CompressionMethod { get; }

        /// <summary>
        /// The filter method used for the image.
        /// </summary>
        public FilterMethod FilterMethod { get; }

        /// <summary>
        /// The interlace method used by the image..
        /// </summary>
        public InterlaceMethod InterlaceMethod { get; }

        /// <summary>
        /// Create a new <see cref="ImageHeader"/>.
        /// </summary>
        public ImageHeader(int width, int height, byte bitDepth, ColorType colorType, CompressionMethod compressionMethod, FilterMethod filterMethod, InterlaceMethod interlaceMethod)
        {
            if (width == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(width), "Invalid width (0) for image.");
            }

            if (height == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(height), "Invalid height (0) for image.");
            }

            if (!PermittedBitDepths.TryGetValue(colorType, out var permitted)
                || !permitted.Contains(bitDepth))
            {
                throw new ArgumentException($"The bit depth {bitDepth} is not permitted for color type {colorType}.");
            }

            Width = width;
            Height = height;
            BitDepth = bitDepth;
            ColorType = colorType;
            CompressionMethod = compressionMethod;
            FilterMethod = filterMethod;
            InterlaceMethod = interlaceMethod;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"w: {Width}, h: {Height}, bitDepth: {BitDepth}, colorType: {ColorType}, " +
                   $"compression: {CompressionMethod}, filter: {FilterMethod}, interlace: {InterlaceMethod}.";
        }
    }
}