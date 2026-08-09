namespace UglyToad.PdfPig.Graphics.Colors
{
    using Core;
    using System;

    /// <summary>
    /// A ColorSpace which the PdfPig library does not currently support. Please raise a PR if you need support for this ColorSpace.
    /// </summary>
    public sealed class UnsupportedColorSpaceDetails : ColorSpaceDetails
    {
        /// <summary>
        /// The single instance of the <see cref="UnsupportedColorSpaceDetails"/>.
        /// </summary>
        public static readonly UnsupportedColorSpaceDetails Instance = new UnsupportedColorSpaceDetails();

        /// <summary>
        /// <inheritdoc/>
        /// <para>
        /// Cannot be called for <see cref="UnsupportedColorSpaceDetails"/>, will throw a <see cref="InvalidOperationException"/>.
        /// </para>
        /// </summary>
        public override int NumberOfColorComponents => throw new InvalidOperationException("UnsupportedColorSpaceDetails");

        /// <summary>
        /// <inheritdoc/>
        /// <para>
        /// Cannot be called for <see cref="UnsupportedColorSpaceDetails"/>, will throw a <see cref="InvalidOperationException"/>.
        /// </para>
        /// </summary>
        public override int BaseNumberOfColorComponents => NumberOfColorComponents;

        private UnsupportedColorSpaceDetails() : base(ColorSpace.DeviceGray)
        {
        }

        /// <inheritdoc/>
        internal override double[] Process(double[] values, RenderingIntent intent)
        {
            throw new InvalidOperationException("UnsupportedColorSpaceDetails");
        }

        /// <inheritdoc/>
        public override IColor GetColor(ReadOnlySpan<double> values, RenderingIntent intent)
        {
            throw new InvalidOperationException("UnsupportedColorSpaceDetails");
        }

        /// <inheritdoc/>
        public override void GetRgb(ReadOnlySpan<double> values, RenderingIntent intent,
            out double r, out double g, out double b)
        {
            throw new InvalidOperationException("UnsupportedColorSpaceDetails");
        }

        /// <inheritdoc/>
        public override IColor? GetInitializeColor()
        {
            throw new InvalidOperationException("UnsupportedColorSpaceDetails");
        }

        /// <inheritdoc/>
        internal override Span<byte> Transform(Span<byte> decoded, RenderingIntent intent)
        {
            throw new InvalidOperationException("UnsupportedColorSpaceDetails");
        }
    }
}
