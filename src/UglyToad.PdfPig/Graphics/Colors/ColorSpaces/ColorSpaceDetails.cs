namespace UglyToad.PdfPig.Graphics.Colors
{
    using System;
    using Core;

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
        /// The device colour space this one's colours are ultimately expressed in, or this colour space
        /// itself when they are not device colours at all.
        /// <para>
        /// Resolved all the way down, not one level: a Separation over an ICCBased that fell back to
        /// DeviceCMYK reports <see cref="ColorSpace.DeviceCMYK"/>, because that is what its colours end up
        /// being. It pairs with <see cref="BaseNumberOfColorComponents"/>, which counts the components of
        /// that same space, and the two are always resolved to the same depth.
        /// </para>
        /// <para>
        /// The chain stops at any colour space that defines its colours absolutely rather than by what a
        /// device would do with them - <see cref="ColorSpace.Lab"/>, <see cref="ColorSpace.CalRGB"/>,
        /// <see cref="ColorSpace.CalGray"/>, and an <see cref="ColorSpace.ICCBased"/> space with a usable
        /// profile - each of which reports itself. Those colours are already colorimetric, so the question
        /// this property answers, "may these be treated as device colours?", is answered "no" for them.
        /// That is what keeps an output intent from being applied on top of a profile that has already
        /// placed the colour (14.11.5 and 8.6.5.7).
        /// </para>
        /// </summary>
        public ColorSpace BaseType { get; protected set; }

        /// <summary>
        /// The number of components of the colour space <see cref="BaseType"/> names, which is also the
        /// number <see cref="Transform"/> writes per sample.
        /// </summary>
        public abstract int BaseNumberOfColorComponents { get; }

        /// <summary>
        /// Whether this colour space can output different colours with different <see cref="RenderingIntent"/>.
        /// </summary>
        public virtual bool RenderingIntentAffectsOutput => false;

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
        public IColor GetColor(ReadOnlySpan<double> values)
            => GetColor(values, RenderingIntent.RelativeColorimetric);

        /// <summary>
        /// Get the color, using the intent.
        /// </summary>
        public abstract IColor GetColor(ReadOnlySpan<double> values, RenderingIntent intent);

        /// <summary>
        /// Get the color as an unboxed RGB triple. Avoids allocating an <see cref="IColor"/> and bypasses the
        /// virtual dispatch through <see cref="IColor.ToRGBValues"/>. Each component is in [0, 1].
        /// </summary>
        /// <param name="values">The component values, in this colour space.</param>
        /// <param name="r">The red component, in [0, 1].</param>
        /// <param name="g">The green component, in [0, 1].</param>
        /// <param name="b">The blue component, in [0, 1].</param>
        public void GetRgb(ReadOnlySpan<double> values, out double r, out double g, out double b)
            => GetRgb(values, RenderingIntent.RelativeColorimetric, out r, out g, out b);

        /// <summary>
        /// Get the color as an unboxed RGB triple. Avoids allocating an <see cref="IColor"/> and bypasses the
        /// virtual dispatch through <see cref="IColor.ToRGBValues"/>. Each component is in [0, 1].
        /// </summary>
        /// <param name="values">The component values, in this colour space.</param>
        /// <param name="intent">Rendering intent.</param>
        /// <param name="r">The red component, in [0, 1].</param>
        /// <param name="g">The green component, in [0, 1].</param>
        /// <param name="b">The blue component, in [0, 1].</param>
        public abstract void GetRgb(ReadOnlySpan<double> values, RenderingIntent intent, out double r, out double g, out double b);

        /// <summary>
        /// Get the color, without check and caching.
        /// </summary>
        internal abstract double[] Process(double[] values, RenderingIntent intent);

        /// <summary>
        /// Copy <paramref name="values"/> into <paramref name="destination"/> so that exactly
        /// <paramref name="destination"/>'s length components are defined: a short input is zero-filled and
        /// a surplus is dropped.
        /// <para>
        /// <paramref name="values"/> may overlap <paramref name="destination"/>, which is what lets a caller
        /// normalise a buffer a function has just written into.
        /// </para>
        /// </summary>
        internal static void Normalise(ReadOnlySpan<double> values, Span<double> destination)
        {
            if (values.Length >= destination.Length)
            {
                values.Slice(0, destination.Length).CopyTo(destination);
            }
            else
            {
                values.CopyTo(destination);
                destination.Slice(values.Length).Clear();
            }
        }

        /// <summary>
        /// Get the color that initialize the current stroking or nonstroking colour.
        /// </summary>
        public IColor? GetInitializeColor()
            => GetInitializeColor(RenderingIntent.RelativeColorimetric);

        /// <summary>
        /// Get the color that initialize the current stroking or nonstroking colour.
        /// </summary>
        public abstract IColor? GetInitializeColor(RenderingIntent intent);

        /// <summary>
        /// Transform image bytes.
        /// </summary>
        internal abstract Span<byte> Transform(Span<byte> decoded, RenderingIntent intent);

        /// <summary>
        /// The Decode array this colour space implies when an image declares none (PDF 2.0, 8.9.5.10,
        /// Table 89): 2 x <see cref="NumberOfColorComponents"/> values <c>[min0 max0 min1 max1 ...]</c>
        /// giving the range each sample decodes into.
        /// <para>
        /// This default is <c>[0, 1]</c> per component, which is right for the device spaces and for most
        /// CIE ones. Spaces whose components are not measured in <c>[0, 1]</c> (Indexed, whose sample is an
        /// index, and Lab and an L*a*b* ICC profile, whose L* runs to 100) override it.
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
        /// Convert a component in <c>[0, 1]</c> to the byte encoding it, clipping anything outside that
        /// range to the nearest end.
        /// <para>
        /// The clipping is not belt and braces: converting a <see cref="double"/> that lies outside
        /// <see cref="byte"/>'s range - or that is <see cref="double.NaN"/> - is undefined in C# and in
        /// practice yields an arbitrary byte rather than a clipped one, so a colour space backed by a
        /// third-party <see cref="Icc.IIccTransform"/> could turn a slightly out-of-gamut component into a
        /// wildly wrong pixel.
        /// </para>
        /// </summary>
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

            // Now that the value is known to be in (0, 1), adding a half and truncating rounds away from
            // zero exactly as Math.Round(x, MidpointRounding.AwayFromZero) did, without the call.
            return (byte)(componentValue * 255.0 + 0.5);
        }
    }
}
