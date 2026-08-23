namespace UglyToad.PdfPig.Graphics.Colors
{
    using System;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Contains more document-specific information about the <see cref="ColorSpace"/>.
    /// </summary>
    public abstract class ColorSpaceDetails
    {
        /// <summary>
        /// The type of the ColorSpace.
        /// </summary>
        public ColorSpace Type { get; }

        /// <summary>
        /// The number of components for the color space.
        /// </summary>
        public abstract int NumberOfColorComponents { get; }

        /// <summary>
        /// The underlying type of <see cref="ColorSpace"/>, usually equal to <see cref="Type"/>
        /// unless <see cref="ColorSpace.Indexed"/> or <see cref="ColorSpace.DeviceN"/>.
        /// </summary>
        public ColorSpace BaseType { get; protected set; }

        /// <summary>
        /// The number of components for the underlying color space.
        /// </summary>
        public abstract int BaseNumberOfColorComponents { get; }

        /// <summary>
        /// Create a new <see cref="ColorSpaceDetails"/>.
        /// </summary>
        protected internal ColorSpaceDetails(ColorSpace type)
        {
            Type = type;
            BaseType = type;
        }

        /// <summary>
        /// Get the color.
        /// </summary>
        public abstract IColor GetColor(ReadOnlySpan<double> values);

        /// <summary>
        /// Get the color as an unboxed RGB triple. Avoids allocating an <see cref="IColor"/> and bypasses the
        /// virtual dispatch through <see cref="IColor.ToRGBValues"/>. Each component is in [0, 1].
        /// </summary>
        /// <param name="values">The component values, in this colour space.</param>
        /// <param name="r">The red component, in [0, 1].</param>
        /// <param name="g">The green component, in [0, 1].</param>
        /// <param name="b">The blue component, in [0, 1].</param>
        public abstract void GetRgb(ReadOnlySpan<double> values, out double r, out double g, out double b);

        /// <summary>
        /// Get the color, without check and caching.
        /// </summary>
        internal abstract double[] Process(params double[] values);

        /// <summary>
        /// Get the color that initialize the current stroking or nonstroking colour.
        /// </summary>
        public abstract IColor? GetInitializeColor();

        /// <summary>
        /// Transform image bytes.
        /// </summary>
        internal abstract Span<byte> Transform(Span<byte> decoded);

        /// <summary>
        /// The Decode array this colour space implies when an image declares none (PDF 2.0, 8.9.5.10,
        /// Table 89): 2 x <see cref="NumberOfColorComponents"/> values <c>[min0 max0 min1 max1 ...]</c>
        /// giving the range each sample decodes into.
        /// <para>
        /// This default is <c>[0, 1]</c> per component, which is right for the device spaces and for most
        /// CIE ones. Spaces whose components are not measured in <c>[0, 1]</c> (Indexed, whose sample is an
        /// index, and Lab, whose L* runs to 100) override it.
        /// </para>
        /// </summary>
        /// <param name="bitsPerComponent">Bits per component of the image the samples came from.</param>
        /// <param name="destination">Must be 2 x <see cref="NumberOfColorComponents"/> long.</param>
        public virtual void GetDefaultDecode(int bitsPerComponent, Span<double> destination)
        {
            for (int c = 0; c < destination.Length; c += 2)
            {
                destination[c] = 0.0;
                destination[c + 1] = 1.0;
            }
        }

        /// <summary>
        /// Decode raw 8-bit encoded component samples (e.g. an Indexed colour space's colour-table
        /// entry) into this colour space's native component ranges, writing in place into
        /// <paramref name="destination"/>. Per ISO 32000-2 (PDF 2.0) 8.6.6.3 each byte decodes to
        /// min + (byte / 255) × (max − min) of the component's range; for device and most CIE
        /// spaces this is [0, 1], which this default implements. Spaces with other native ranges
        /// (e.g. Lab) override this.
        /// </summary>
        /// <param name="raw">The encoded 8-bit component samples.</param>
        /// <param name="destination">Receives the decoded component values. Must be at least as long as <paramref name="raw"/>.</param>
        internal virtual void DecodeRawComponents(ReadOnlySpan<byte> raw, Span<double> destination)
        {
            for (int i = 0; i < raw.Length; i++)
            {
                destination[i] = raw[i] / 255.0;
            }
        }

        /// <summary>
        /// Convert a component in <c>[0, 1]</c> to the byte encoding it in <c>[0, 255]</c>, clipping anything outside that
        /// range to the nearest end.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static byte ConvertToByte(double componentValue)
        {
            // Written as a pair of positive tests so that NaN, which compares false against everything,
            // lands on 0 rather than falling through.
            if (!(componentValue > 0.0))
            {
                return 0;
            }

            if (componentValue >= 1.0)
            {
                return 255;
            }

            return (byte)Math.Round(componentValue * 255, MidpointRounding.AwayFromZero);
        }
    }
}
