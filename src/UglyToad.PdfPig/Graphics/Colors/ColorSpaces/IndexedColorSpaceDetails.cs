namespace UglyToad.PdfPig.Graphics.Colors
{
    using Core;
    using System;
    using System.Collections.Concurrent;

    /// <summary>
    /// An Indexed color space allows a PDF content stream to use small integers as indices into a color map or color table of arbitrary colors in some other space.
    /// A PDF consumer treats each sample value as an index into the color table and uses the color value it finds there.
    /// </summary>
    public sealed class IndexedColorSpaceDetails : ColorSpaceDetails
    {
        private readonly ConcurrentDictionary<(double Index, RenderingIntent Intent), IColor> cache = new();

        /// <summary>
        /// Creates an indexed color space useful for extracting stencil masks as black-and-white images,
        /// i.e. with a color palette of two colors (black and white).
        /// </summary>
        internal static ColorSpaceDetails Stencil(ColorSpaceDetails colorSpaceDetails)
        {
            return new IndexedColorSpaceDetails(colorSpaceDetails, 1, [0, 255]);
        }

        /// <inheritdoc/>
        public override int NumberOfColorComponents => 1;

        /// <summary>
        /// <inheritdoc/>
        /// <para>In the case of <see cref="IndexedColorSpaceDetails"/>, gets the <see cref="BaseColorSpace"/>' <c>BaseNumberOfColorComponents</c>.</para>
        /// </summary>
        public override int BaseNumberOfColorComponents => BaseColorSpace.BaseNumberOfColorComponents;

        /// <summary>
        /// <inheritdoc/>
        /// <para>
        /// An index selects an entry from the colour table and that entry is converted through
        /// <see cref="BaseColorSpace"/>, so this varies exactly when the base space does.
        /// </para>
        /// </summary>
        public override bool RenderingIntentAffectsOutput => BaseColorSpace.RenderingIntentAffectsOutput;

        /// <summary>
        /// The base color space in which the values in the color table are to be interpreted.
        /// It can be any device or CIE-based color space or (in PDF 1.3) a Separation or DeviceN space,
        /// but not a Pattern space or another Indexed space.
        /// </summary>
        public ColorSpaceDetails BaseColorSpace { get; }

        /// <summary>
        /// An integer that specifies the maximum valid index value. Can be no greater than 255.
        /// </summary>
        public byte HiVal { get; }

        private readonly byte[] colorTable;

        /// <summary>
        /// Provides the mapping between index values and the corresponding colors in the base color space.
        /// </summary>
        public ReadOnlySpan<byte> ColorTable => colorTable;

        /// <summary>
        /// Create a new <see cref="IndexedColorSpaceDetails"/>.
        /// </summary>
        public IndexedColorSpaceDetails(ColorSpaceDetails baseColorSpaceDetails, byte hiVal, byte[] colorTable)
            : base(ColorSpace.Indexed)
        {
            BaseColorSpace = baseColorSpaceDetails ?? throw new ArgumentNullException(nameof(baseColorSpaceDetails));
            HiVal = hiVal;
            this.colorTable = colorTable;
            BaseType = baseColorSpaceDetails.Type;
        }

        /// <summary>
        /// <inheritdoc/>
        /// <para>
        /// An Indexed sample is a palette index, not a colour value, so it decodes to
        /// <c>[0, 2^bitsPerComponent - 1]</c> (the identity for every bit depth).
        /// The colour arrives from the subsequent table lookup instead.
        /// </para>
        /// </summary>
        public override void GetDefaultDecode(int bitsPerComponent, Span<double> destination)
        {
            destination[0] = 0.0;
            destination[1] = (1 << bitsPerComponent) - 1;
        }

        /// <summary>
        /// Convert a colour index, which may be a real number or fall outside the valid
        /// range, into a valid table index. Per ISO 32000-2 (PDF 2.0) 8.6.6.3 the value is
        /// rounded to the nearest integer (0.5 rounds up) and any value outside 0..<see cref="HiVal"/>
        /// is adjusted to the nearest value within that range.
        /// </summary>
        private byte ClampColorIndex(double value)
        {
            double rounded = Math.Round(value, MidpointRounding.AwayFromZero);
            if (rounded <= 0)
            {
                return 0;
            }

            return rounded >= HiVal ? HiVal : (byte)rounded;
        }

        /// <summary>
        /// Decode colour-table bytes to the base colour space's component ranges.
        /// ISO 32000-2 (PDF 2.0) 8.6.6.3: the colour table data is interpreted as
        /// component values in the base space, i.e. each byte decodes to
        /// min + (byte / 255) × (max − min) of that component's range. Device and
        /// most CIE spaces use [0, 1]; Lab uses L* ∈ [0, 100] and a*/b* from the
        /// /Range entry — feeding Lab a [0, 1] L* renders near-black.
        /// </summary>
        /// <remarks>
        /// The decode rule lives on the base colour space (<see cref="ColorSpaceDetails.DecodeRawComponents"/>);
        /// this writes the decoded components in place into <paramref name="destination"/>, whose
        /// length selects the table entry width (the base space's component count).
        /// </remarks>
        private void DecodeTableEntry(byte index, Span<double> destination)
        {
            BaseColorSpace.DecodeRawComponents(
                ColorTable.Slice(index * destination.Length, destination.Length),
                destination);
        }

        /// <inheritdoc/>
        public override IColor GetColor(ReadOnlySpan<double> values, RenderingIntent intent)
        {
            if (values.Length != NumberOfColorComponents)
            {
                throw new ArgumentException($"Invalid number of inputs, expecting {NumberOfColorComponents} but got {values.Length}", nameof(values));
            }

            double index = values[0];
            var key = (index, intent);

            if (cache.TryGetValue(key, out var color))
            {
                return color;
            }

            int components = BaseColorSpace.NumberOfColorComponents;
            Span<double> buffer = components <= 32 ? stackalloc double[components] : new double[components];
            DecodeTableEntry(ClampColorIndex(index), buffer);
            color = BaseColorSpace.GetColor(buffer, intent);

            cache.TryAdd(key, color);
            return color;
        }

        /// <inheritdoc/>
        internal override double[] Process(double[] values, RenderingIntent intent)
        {
            var components = new double[BaseColorSpace.NumberOfColorComponents];
            DecodeTableEntry(ClampColorIndex(values[0]), components);
            return BaseColorSpace.Process(components, intent);
        }

        internal Span<byte> UnwrapIndexedColorSpaceBytes(Span<byte> input)
        {
            // ISO 32000-2 (PDF 2.0) 8.6.6.3: an index outside 0..hival is adjusted to the
            // nearest valid value. Indices arrive here as (unsigned) bytes (image samples, or
            // the already-clamped content-stream index), so only the upper bound can be
            // exceeded.
            if (HiVal != byte.MaxValue)
            {
                for (int k = 0; k < input.Length; ++k)
                {
                    ref byte c = ref input[k];
                    if (c > HiVal)
                    {
                        c = HiVal;
                    }
                }
            }

            switch (BaseType)
            {
                case ColorSpace.DeviceRGB:
                case ColorSpace.CalRGB:
                case ColorSpace.Lab:
                    {
                        Span<byte> result = new byte[input.Length * 3];
                        var i = 0;
                        foreach (var x in input)
                        {
                            for (var j = 0; j < 3; ++j)
                            {
                                result[i++] = ColorTable[x * 3 + j];
                            }
                        }

                        return result;
                    }

                case ColorSpace.DeviceCMYK:
                    {
                        Span<byte> result = new byte[input.Length * 4];
                        var i = 0;
                        foreach (var x in input)
                        {
                            for (var j = 0; j < 4; ++j)
                            {
                                result[i++] = ColorTable[x * 4 + j];
                            }
                        }

                        return result;
                    }

                case ColorSpace.DeviceGray:
                case ColorSpace.CalGray:
                case ColorSpace.Separation:
                    {
                        for (var i = 0; i < input.Length; ++i)
                        {
                            ref byte b = ref input[i];
                            b = ColorTable[b];
                        }

                        return input;
                    }

                case ColorSpace.DeviceN:
                case ColorSpace.ICCBased:
                    {
                        int i = 0;
                        if (BaseColorSpace.NumberOfColorComponents == 1)
                        {
                            // In place
                            for (i = 0; i < input.Length; ++i)
                            {
                                ref byte b = ref input[i];
                                b = ColorTable[b];
                            }

                            return input;
                        }

                        Span<byte> result = new byte[input.Length * BaseColorSpace.NumberOfColorComponents];
                        foreach (var x in input)
                        {
                            for (var j = 0; j < BaseColorSpace.NumberOfColorComponents; ++j)
                            {
                                result[i++] = ColorTable[x * BaseColorSpace.NumberOfColorComponents + j];
                            }
                        }

                        return result;
                    }
            }

            return input;
        }

        /// <inheritdoc/>
        public override IColor GetInitializeColor(RenderingIntent intent)
        {
            // Setting the current stroking or nonstroking colour space to an Indexed colour space shall
            // initialize the corresponding current colour to 0.
            return GetColor([0], intent);
        }

        /// <inheritdoc/>
        public override void GetRgb(ReadOnlySpan<double> values, RenderingIntent intent,
            out double r, out double g, out double b)
        {
            // Look up the index into the colour table and let the base colour space decode its
            // own table bytes into its native component ranges.
            byte index = ClampColorIndex(values[0]);
            int components = BaseColorSpace.NumberOfColorComponents;
            Span<double> buffer = components <= 32 ? stackalloc double[components] : new double[components];
            DecodeTableEntry(index, buffer);
            BaseColorSpace.GetRgb(buffer, intent, out r, out g, out b);
        }

        /// <summary>
        /// <inheritdoc/>
        /// <para>
        /// Unwrap then transform using base color space details.
        /// </para>
        /// </summary>
        internal override Span<byte> Transform(Span<byte> decoded, RenderingIntent intent)
        {
            var unwraped = UnwrapIndexedColorSpaceBytes(decoded);
            return BaseColorSpace.Transform(unwraped, intent);
        }
    }
}
