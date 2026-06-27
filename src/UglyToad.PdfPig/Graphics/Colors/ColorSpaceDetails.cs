namespace UglyToad.PdfPig.Graphics.Colors
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.InteropServices;
    using Tokens;
    using UglyToad.PdfPig.Content;
    using UglyToad.PdfPig.Functions;
    using UglyToad.PdfPig.Util;

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
        public abstract IColor GetColor(params double[] values);

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
        /// Convert to byte.
        /// </summary>
        protected static byte ConvertToByte(double componentValue)
        {
            var rounded = Math.Round(componentValue * 255, MidpointRounding.AwayFromZero);
            return (byte)rounded;
        }

        /// <summary>
        /// Evaluate <paramref name="tintInput"/> through a tint <paramref name="tint"/> function whose output values
        /// are then mapped to RGB by <paramref name="alternate"/>'s <see cref="GetRgb"/>. Allocation-free for the
        /// typical case where the alternate colour space has at most 8 components.
        /// </summary>
        private protected static void GetRgbViaTint(PdfFunction tint, ColorSpaceDetails alternate,
            ReadOnlySpan<double> tintInput, out double r, out double g, out double b)
        {
            int alternateComponents = alternate.NumberOfColorComponents;
            int tintMax = tint.MaxOutputComponentCount;
            int max = tintMax > alternateComponents ? tintMax : alternateComponents;
            Span<double> buffer = max <= 16 ? stackalloc double[max] : new double[max];
            int written = tint.Eval(tintInput, buffer);
            if (written < alternateComponents)
            {
                // A buggy tint function under-filled the buffer. Zero the trailing slots so the alternate
                // space's GetRgb call doesn't read uninitialised stack memory.
                buffer.Slice(written, alternateComponents - written).Clear();
                written = alternateComponents;
            }
            alternate.GetRgb(buffer.Slice(0, written), out r, out g, out b);
        }
    }

    /// <summary>
    /// A grayscale value is represented by a single number in the range 0.0 to 1.0,
    /// where 0.0 corresponds to black, 1.0 to white, and intermediate values to different gray levels.
    /// </summary>
    public sealed class DeviceGrayColorSpaceDetails : ColorSpaceDetails
    {
        /// <summary>
        /// The single instance of the <see cref="DeviceGrayColorSpaceDetails"/>.
        /// </summary>
        public static readonly DeviceGrayColorSpaceDetails Instance = new DeviceGrayColorSpaceDetails();

        /// <inheritdoc/>
        public override int NumberOfColorComponents => 1;

        /// <inheritdoc/>
        public override int BaseNumberOfColorComponents => NumberOfColorComponents;

        private DeviceGrayColorSpaceDetails() : base(ColorSpace.DeviceGray)
        { }

        /// <inheritdoc/>
        internal override double[] Process(params double[] values)
        {
            return values;
        }

        /// <inheritdoc/>
        public override IColor GetColor(params double[] values)
        {
            if (values is null || values.Length != NumberOfColorComponents)
            {
                throw new ArgumentException($"Invalid number of inputs, expecting {NumberOfColorComponents} but got {values?.Length ?? 0}", nameof(values));
            }

            double gray = values[0];
            if (gray == 0)
            {
                return GrayColor.Black;
            }

            if (gray == 1)
            {
                return GrayColor.White;
            }

            return new GrayColor(gray);
        }

        /// <inheritdoc/>
        public override IColor GetInitializeColor()
        {
            return GrayColor.Black;
        }

        /// <inheritdoc/>
        public override void GetRgb(ReadOnlySpan<double> values, out double r, out double g, out double b)
        {
            double gray = values[0];
            r = gray;
            g = gray;
            b = gray;
        }

        /// <inheritdoc/>
        internal override Span<byte> Transform(Span<byte> decoded)
        {
            return decoded;
        }
    }

    /// <summary>
    /// Color values are defined by three components representing the intensities of the additive primary colorants red, green and blue.
    /// Each component is specified by a number in the range 0.0 to 1.0, where 0.0 denotes the complete absence of a primary component and 1.0 denotes maximum intensity.
    /// </summary>
    public sealed class DeviceRgbColorSpaceDetails : ColorSpaceDetails
    {
        /// <summary>
        /// The single instance of the <see cref="DeviceRgbColorSpaceDetails"/>.
        /// </summary>
        public static readonly DeviceRgbColorSpaceDetails Instance = new DeviceRgbColorSpaceDetails();

        /// <inheritdoc/>
        public override int NumberOfColorComponents => 3;

        /// <inheritdoc/>
        public override int BaseNumberOfColorComponents => NumberOfColorComponents;

        private DeviceRgbColorSpaceDetails() : base(ColorSpace.DeviceRGB)
        { }

        /// <inheritdoc/>
        internal override double[] Process(params double[] values)
        {
            return values;
        }

        /// <inheritdoc/>
        public override IColor GetColor(params double[] values)
        {
            if (values is null || values.Length != NumberOfColorComponents)
            {
                throw new ArgumentException($"Invalid number of inputs, expecting {NumberOfColorComponents} but got {values?.Length ?? 0}", nameof(values));
            }

            double r = values[0];
            double g = values[1];
            double b = values[2];
            if (r == 0 && g == 0 && b == 0)
            {
                return RGBColor.Black;
            }

            if (r == 1 && g == 1 && b == 1)
            {
                return RGBColor.White;
            }

            return new RGBColor(r, g, b);
        }

        /// <inheritdoc/>
        public override IColor GetInitializeColor()
        {
            return RGBColor.Black;
        }

        /// <inheritdoc/>
        public override void GetRgb(ReadOnlySpan<double> values, out double r, out double g, out double b)
        {
            r = values[0];
            g = values[1];
            b = values[2];
        }

        /// <inheritdoc/>
        internal override Span<byte> Transform(Span<byte> decoded)
        {
            return decoded;
        }
    }

    /// <summary>
    /// Color values are defined by four components cyan, magenta, yellow and black.
    /// </summary>
    public sealed class DeviceCmykColorSpaceDetails : ColorSpaceDetails
    {
        /// <summary>
        /// The single instance of the <see cref="DeviceCmykColorSpaceDetails"/>.
        /// </summary>
        public static readonly DeviceCmykColorSpaceDetails Instance = new DeviceCmykColorSpaceDetails();

        /// <inheritdoc/>
        public override int NumberOfColorComponents => 4;

        /// <inheritdoc/>
        public override int BaseNumberOfColorComponents => NumberOfColorComponents;

        private DeviceCmykColorSpaceDetails() : base(ColorSpace.DeviceCMYK)
        {
        }

        /// <inheritdoc/>
        internal override double[] Process(params double[] values)
        {
            return values;
        }

        /// <inheritdoc/>
        public override IColor GetColor(params double[] values)
        {
            if (values is null || values.Length != NumberOfColorComponents)
            {
                throw new ArgumentException($"Invalid number of inputs, expecting {NumberOfColorComponents} but got {values?.Length ?? 0}", nameof(values));
            }

            double c = values[0];
            double m = values[1];
            double y = values[2];
            double k = values[3];
            if (c == 0 && m == 0 && y == 0 && k == 1)
            {
                return CMYKColor.Black;
            }
            else if (c == 0 && m == 0 && y == 0 && k == 0)
            {
                return CMYKColor.White;
            }

            return new CMYKColor(c, m, y, k);
        }

        /// <inheritdoc/>
        public override IColor GetInitializeColor()
        {
            return CMYKColor.Black;
        }

        /// <inheritdoc/>
        public override void GetRgb(ReadOnlySpan<double> values, out double r, out double g, out double b)
        {
            double c = values[0];
            double m = values[1];
            double y = values[2];
            double k = values[3];
            double oneMinusK = 1.0 - k;
            r = (1.0 - c) * oneMinusK;
            g = (1.0 - m) * oneMinusK;
            b = (1.0 - y) * oneMinusK;
        }

        /// <inheritdoc/>
        internal override Span<byte> Transform(Span<byte> decoded)
        {
            return decoded;
        }
    }

    /// <summary>
    /// An Indexed color space allows a PDF content stream to use small integers as indices into a color map or color table of arbitrary colors in some other space.
    /// A PDF consumer treats each sample value as an index into the color table and uses the color value it finds there.
    /// </summary>
    public sealed class IndexedColorSpaceDetails : ColorSpaceDetails
    {
        private readonly ConcurrentDictionary<double, IColor> cache = new ConcurrentDictionary<double, IColor>();

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

        /// <inheritdoc/>
        internal override double[] Process(params double[] values)
        {
            var csBytes = UnwrapIndexedColorSpaceBytes([ClampColorIndex(values[0])]);

            var scaledCsBytes = new double[csBytes.Length];

            for (int i = 0; i < csBytes.Length; i++)
            {
                scaledCsBytes[i] = csBytes[i] / 255.0;
            }

            return BaseColorSpace.Process(scaledCsBytes);
        }

        /// <inheritdoc/>
        public override IColor GetColor(params double[] values)
        {
            if (values is null || values.Length != NumberOfColorComponents)
            {
                throw new ArgumentException($"Invalid number of inputs, expecting {NumberOfColorComponents} but got {values?.Length ?? 0}", nameof(values));
            }

            return cache.GetOrAdd(values[0], v =>
            {
                var csBytes = UnwrapIndexedColorSpaceBytes([ClampColorIndex(v)]);

                var scaledCsBytes = new double[csBytes.Length];

                for (int i = 0; i < csBytes.Length; i++)
                {
                    scaledCsBytes[i] = csBytes[i] / 255.0;
                }

                return BaseColorSpace.GetColor(scaledCsBytes);
            });
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
        public override IColor GetInitializeColor()
        {
            // Setting the current stroking or nonstroking colour space to an Indexed colour space shall
            // initialize the corresponding current colour to 0.
            return GetColor(0);
        }

        /// <inheritdoc/>
        public override void GetRgb(ReadOnlySpan<double> values, out double r, out double g, out double b)
        {
            // Look up the index into the colour table and dispatch to the base colour space.
            // Base color spaces have at most 4 components for our supported types.
            byte index = (byte)values[0];
            Span<double> buffer = stackalloc double[4];
            int components;
            switch (BaseType)
            {
                case ColorSpace.DeviceRGB:
                case ColorSpace.CalRGB:
                case ColorSpace.Lab:
                    components = 3;
                    for (int j = 0; j < 3; j++)
                    {
                        buffer[j] = colorTable[index * 3 + j] / 255.0;
                    }

                    break;
                case ColorSpace.DeviceCMYK:
                    components = 4;
                    for (int j = 0; j < 4; j++)
                    {
                        buffer[j] = colorTable[index * 4 + j] / 255.0;
                    }

                    break;
                case ColorSpace.DeviceGray:
                case ColorSpace.CalGray:
                case ColorSpace.Separation:
                    components = 1;
                    buffer[0] = colorTable[index] / 255.0;
                    break;
                case ColorSpace.DeviceN:
                case ColorSpace.ICCBased:
                    components = BaseColorSpace.NumberOfColorComponents;
                    if (components == 1)
                    {
                        buffer[0] = colorTable[index] / 255.0;
                    }
                    else
                    {
                        if (components > buffer.Length)
                        {
                            Span<double> big = components <= 128 ? stackalloc double[components] : new double[components];
                            for (int j = 0; j < components; j++)
                            {
                                big[j] = colorTable[index * components + j] / 255.0;
                            }

                            BaseColorSpace.GetRgb(big, out r, out g, out b);
                            return;
                        }

                        for (int j = 0; j < components; j++)
                        {
                            buffer[j] = colorTable[index * components + j] / 255.0;
                        }
                    }

                    break;
                default:
                    components = 1;
                    buffer[0] = values[0];
                    break;
            }

            BaseColorSpace.GetRgb(buffer.Slice(0, components), out r, out g, out b);
        }

        /// <summary>
        /// <inheritdoc/>
        /// <para>
        /// Unwrap then transform using base color space details.
        /// </para>
        /// </summary>
        internal override Span<byte> Transform(Span<byte> decoded)
        {
            var unwraped = UnwrapIndexedColorSpaceBytes(decoded);
            return BaseColorSpace.Transform(unwraped);
        }
    }

    /// <summary>
    /// DeviceN colour spaces may contain an arbitrary number of colour components. They provide greater flexibility than
    /// is possible with standard device colour spaces such as DeviceCMYK or with individual Separation colour spaces.
    /// </summary>
    public sealed class DeviceNColorSpaceDetails : ColorSpaceDetails
    {
        /// <summary>
        /// <inheritdoc/>
        /// <para>The 'N' in DeviceN.</para>
        /// </summary>
        public override int NumberOfColorComponents { get; }

        /// <inheritdoc/>
        public override int BaseNumberOfColorComponents => AlternateColorSpace.NumberOfColorComponents;

        /// <summary>
        /// Specifies name objects specifying the individual colour components. The length of the array shall
        /// determine the number of components in the DeviceN colour space.
        /// </summary>
        /// <remarks>
        /// The component names shall all be different from one another, except for the name None, which may be repeated.
        /// <para>
        /// The special name All, used by Separation colour spaces, shall not be used.
        /// </para>
        /// </remarks>
        public IReadOnlyList<NameToken> Names { get; }

        /// <summary>
        /// If the colorant name associated with a DeviceN color space does not correspond to a colorant available on the device,
        /// the application arranges for subsequent painting operations to be performed in an alternate color space.
        /// The intended colors can be approximated by colors in a device or CIE-based color space
        /// which are then rendered with the usual primary or process colorants.
        /// </summary>
        public ColorSpaceDetails AlternateColorSpace { get; }

        /// <summary>
        /// The optional attributes parameter shall be a dictionary containing additional information about the components of
        /// colour space that conforming readers may use. Conforming readers need not use the alternateSpace and tintTransform
        /// parameters, and may instead use custom blending algorithms, along with other information provided in the attributes
        /// dictionary if present.
        /// </summary>
        public DeviceNColorSpaceAttributes? Attributes { get; }

        /// <summary>
        /// During subsequent painting operations, an application calls this function to transform a tint value into
        /// color component values in the alternate color space.
        /// The function is called with the tint value and must return the corresponding color component values.
        /// That is, the number of components and the interpretation of their values depend on the <see cref="AlternateColorSpace"/>.
        /// </summary>
        public PdfFunction TintFunction { get; }

        /// <summary>
        /// Create a new <see cref="DeviceNColorSpaceDetails"/>.
        /// </summary>
        public DeviceNColorSpaceDetails(IReadOnlyList<NameToken> names, ColorSpaceDetails alternateColorSpaceDetails,
            PdfFunction tintFunction, DeviceNColorSpaceAttributes? attributes = null)
            : base(ColorSpace.DeviceN)
        {
            Names = names;
            NumberOfColorComponents = Names.Count;
            AlternateColorSpace = alternateColorSpaceDetails;
            Attributes = attributes;
            TintFunction = tintFunction;
            BaseType = AlternateColorSpace.Type;
        }

        /// <inheritdoc/>
        internal override double[] Process(params double[] values)
        {
            var evaled = TintFunction.Eval(values);
            return AlternateColorSpace.Process(evaled);
        }

        /// <inheritdoc/>
        public override IColor GetColor(params double[] values)
        {
            if (values is null || values.Length != NumberOfColorComponents)
            {
                throw new ArgumentException($"Invalid number of inputs, expecting {NumberOfColorComponents} but got {values?.Length ?? 0}", nameof(values));
            }

            // TODO - use attributes

            // TODO - caching
            var evaled = TintFunction.Eval(values);
            return AlternateColorSpace.GetColor(evaled);
        }

        /// <inheritdoc/>
        internal override Span<byte> Transform(Span<byte> decoded)
        {
            var cache = new Dictionary<int, double[]>();
            var transformed = new List<byte>();
            for (var i = 0; i < decoded.Length; i += NumberOfColorComponents)
            {
                int key = 0;
                var comps = new double[NumberOfColorComponents];
                for (int n = 0; n < NumberOfColorComponents; n++)
                {
                    byte b = decoded[i + n];
                    key = (key * 31) ^ b;
                    comps[n] = b / 255.0;
                }

                if (!cache.TryGetValue(key, out double[]? colors))
                {
                    colors = Process(comps);
                    cache[key] = colors;
                }

                for (int c = 0; c < colors.Length; c++)
                {
                    transformed.Add(ConvertToByte(colors[c]));
                }
            }

#if NET
            return CollectionsMarshal.AsSpan(transformed);
#else
            return transformed.ToArray();
#endif
        }

        /// <inheritdoc/>
        public override IColor GetInitializeColor()
        {
            // When this space is set to the current colour space (using the CS or cs operators), each component
            // shall be given an initial value of 1.0. The SCN and scn operators respectively shall set the current
            // stroking and nonstroking colour.
            return GetColor(Enumerable.Repeat(1.0, NumberOfColorComponents).ToArray());
        }

        /// <inheritdoc/>
        public override void GetRgb(ReadOnlySpan<double> values, out double r, out double g, out double b)
        {
            GetRgbViaTint(TintFunction, AlternateColorSpace, values, out r, out g, out b);
        }

        /// <summary>
        /// DeviceN Color Space Attributes.
        /// </summary>
        public readonly struct DeviceNColorSpaceAttributes
        {
            /// <summary>
            /// A name specifying the preferred treatment for the colour space. Values shall be <c>DeviceN</c> or <c>NChannel</c>. Default value: <c>DeviceN</c>.
            /// </summary>
            public NameToken Subtype { get; }

            /// <summary>
            /// Colorants - dictionary - Required if Subtype is NChannel and the colour space includes spot colorants; otherwise optional.
            /// </summary>
            public DictionaryToken? Colorants { get; }

            /// <summary>
            /// Process - dictionary - Required if Subtype is NChannel and the colour space includes components of a process colour space, otherwise optional.
            /// </summary>
            public DictionaryToken? Process { get; }

            /// <summary>
            /// MixingHints - dictionary - Optional
            /// </summary>
            public DictionaryToken? MixingHints { get; }

            /// <summary>
            /// Create a new <see cref="DeviceNColorSpaceAttributes"/>.
            /// </summary>
            public DeviceNColorSpaceAttributes()
            {
                Subtype = NameToken.Devicen;
                Colorants = null;
                Process = null;
                MixingHints = null;
            }

            /// <summary>
            /// Create a new <see cref="DeviceNColorSpaceAttributes"/>.
            /// </summary>
            public DeviceNColorSpaceAttributes(NameToken subtype, DictionaryToken? colorants, DictionaryToken? process, DictionaryToken? mixingHints)
            {
                Subtype = subtype;
                Colorants = colorants;
                Process = process;
                MixingHints = mixingHints;
            }
        }
    }

    /// <summary>
    /// A Separation color space provides a means for specifying the use of additional colorants or
    /// for isolating the control of individual color components of a device color space for a subtractive device.
    /// When such a space is the current color space, the current color is a single-component value, called a tint,
    /// that controls the application of the given colorant or color components only.
    /// </summary>
    public sealed class SeparationColorSpaceDetails : ColorSpaceDetails
    {
        private readonly ConcurrentDictionary<double, IColor> cache = new ConcurrentDictionary<double, IColor>();

        /// <inheritdoc/>
        public override int NumberOfColorComponents => 1;

        /// <inheritdoc/>
        public override int BaseNumberOfColorComponents => AlternateColorSpace.NumberOfColorComponents;

        /// <summary>
        /// Specifies the name of the colorant that this Separation color space is intended to represent.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The special colorant name All refers collectively to all colorants available on an output device,
        /// including those for the standard process colorants.
        /// </para>
        /// <para>
        /// The special colorant name None never produces any visible output.
        /// Painting operations in a Separation space with this colorant name have no effect on the current page.
        /// </para>
        /// </remarks>
        public NameToken Name { get; }

        /// <summary>
        /// If the colorant name associated with a Separation color space does not correspond to a colorant available on the device,
        /// the application arranges for subsequent painting operations to be performed in an alternate color space.
        /// The intended colors can be approximated by colors in a device or CIE-based color space
        /// which are then rendered with the usual primary or process colorants.
        /// </summary>
        public ColorSpaceDetails AlternateColorSpace { get; }

        /// <summary>
        /// During subsequent painting operations, an application calls this function to transform a tint value into
        /// color component values in the alternate color space.
        /// The function is called with the tint value and must return the corresponding color component values.
        /// That is, the number of components and the interpretation of their values depend on the <see cref="AlternateColorSpace"/>.
        /// </summary>
        public PdfFunction TintFunction { get; }

        /// <summary>
        /// Create a new <see cref="SeparationColorSpaceDetails"/>.
        /// </summary>
        public SeparationColorSpaceDetails(NameToken name,
            ColorSpaceDetails alternateColorSpaceDetails,
            PdfFunction tintFunction)
            : base(ColorSpace.Separation)
        {
            Name = name;
            AlternateColorSpace = alternateColorSpaceDetails;
            TintFunction = tintFunction;
        }

        /// <inheritdoc/>
        internal override double[] Process(params double[] values)
        {
            var evaled = TintFunction.Eval(values[0]);
            return AlternateColorSpace.Process(evaled);
        }

        /// <inheritdoc/>
        public override IColor GetColor(params double[] values)
        {
            if (values is null || values.Length != NumberOfColorComponents)
            {
                throw new ArgumentException($"Invalid number of inputs, expecting {NumberOfColorComponents} but got {values?.Length ?? 0}", nameof(values));
            }

            // TODO - we ignore the name for now

            return cache.GetOrAdd(values[0], v =>
            {
                var evaled = TintFunction.Eval(v);
                return AlternateColorSpace.GetColor(evaled);
            });
        }

        /// <inheritdoc/>
        internal override Span<byte> Transform(Span<byte> values)
        {
            var colorCache = new Dictionary<int, double[]>(values.Length);
            var transformed = new List<byte>(values.Length);

            for (var i = 0; i < values.Length; ++i)
            {
                byte b = values[i];
                if (!colorCache.TryGetValue(b, out double[]? colors))
                {
                    colors = Process(b / 255.0);
                    colorCache[b] = colors;
                }

                for (int c = 0; c < colors.Length; ++c)
                {
                    transformed.Add(ConvertToByte(colors[c]));
                }
            }

#if NET
            return CollectionsMarshal.AsSpan(transformed);
#else
            return transformed.ToArray();
#endif
        }

        /// <inheritdoc/>
        public override IColor GetInitializeColor()
        {
            // The initial value for both the stroking and nonstroking colour in the graphics state shall be 1.0.
            return GetColor(1.0);
        }

        /// <inheritdoc/>
        public override void GetRgb(ReadOnlySpan<double> values, out double r, out double g, out double b)
        {
            GetRgbViaTint(TintFunction, AlternateColorSpace, values.Slice(0, 1), out r, out g, out b);
        }
    }

    /// <summary>
    /// CIE (Commission Internationale de l'Éclairage) colorspace.
    /// Specifies color related to human visual perception with the aim of producing consistent color on different output devices.
    /// CalGray - A CIE A color space with a single transformation.
    /// A represents the gray component of a calibrated gray space. The component must be in the range 0.0 to 1.0.
    /// </summary>
    public sealed class CalGrayColorSpaceDetails : ColorSpaceDetails
    {
        /// <inheritdoc/>
        public override int NumberOfColorComponents => 1;

        /// <inheritdoc/>
        public override int BaseNumberOfColorComponents => NumberOfColorComponents;

        private readonly CIEBasedColorSpaceTransformer colorSpaceTransformer;

        /// <summary>
        /// An array of three numbers [XW  YW  ZW] specifying the tristimulus value, in the CIE 1931 XYZ space of the
        /// diffuse white point. The numbers XW and ZW shall be positive, and YW shall be equal to 1.0.
        /// </summary>
        public IReadOnlyList<double> WhitePoint { get; }

        /// <summary>
        /// An array of three numbers [XB  YB  ZB] specifying the tristimulus value, in the CIE 1931 XYZ space of the
        /// diffuse black point. All three numbers must be non-negative. Default value: [0.0  0.0  0.0].
        /// </summary>
        public IReadOnlyList<double> BlackPoint { get; }

        /// <summary>
        /// A number defining the gamma for the gray (A) component. Gamma must be positive and is generally
        /// greater than or equal to 1. Default value: 1.
        /// </summary>
        public double Gamma { get; }

        /// <summary>
        /// Create a new <see cref="CalGrayColorSpaceDetails"/>.
        /// </summary>
        public CalGrayColorSpaceDetails(double[] whitePoint, double[]? blackPoint, double? gamma)
            : base(ColorSpace.CalGray)
        {
            WhitePoint = whitePoint ?? throw new ArgumentNullException(nameof(whitePoint));
            if (WhitePoint.Count != 3)
            {
                throw new ArgumentOutOfRangeException(nameof(whitePoint), whitePoint, $"Must consist of exactly three numbers, but was passed {whitePoint.Length}.");
            }

            BlackPoint = blackPoint ?? [0.0, 0, 0];
            if (BlackPoint.Count != 3)
            {
                throw new ArgumentOutOfRangeException(nameof(blackPoint), blackPoint, $"Must consist of exactly three numbers, but was passed {blackPoint?.Length ?? 0}.");
            }

            Gamma = gamma ?? 1.0;

            colorSpaceTransformer =
                new CIEBasedColorSpaceTransformer((WhitePoint[0], WhitePoint[1], WhitePoint[2]), RGBWorkingSpace.sRGB)
                {
                    DecoderABC = color => (
                    Math.Pow(color.A, Gamma),
                    Math.Pow(color.B, Gamma),
                    Math.Pow(color.C, Gamma)),

                    MatrixABC = new Matrix3x3(
                    WhitePoint[0], 0, 0,
                    0, WhitePoint[1], 0,
                    0, 0, WhitePoint[2])
                };
        }

        /// <inheritdoc/>
        internal override Span<byte> Transform(Span<byte> decoded)
        {
            var transformed = new byte[decoded.Length];
            Span<double> input = stackalloc double[1];

            for (var i = 0; i < decoded.Length; i++)
            {
                input[0] = decoded[i] / 255.0;
                GetRgb(input, out double r, out _, out _);
                // We only need one component here
                transformed[i] = ConvertToByte(r);
            }

            return transformed;
        }

        /// <inheritdoc/>
        internal override double[] Process(params double[] values)
        {
            GetRgb(values, out double r, out _, out _);
            return [r];
        }

        /// <inheritdoc/>
        public override IColor GetColor(params double[] values)
        {
            if (values is null || values.Length != NumberOfColorComponents)
            {
                throw new ArgumentException($"Invalid number of inputs, expecting {NumberOfColorComponents} but got {values?.Length ?? 0}", nameof(values));
            }

            GetRgb(values, out double r, out double g, out double b);
            return new RGBColor(r, g, b);
        }

        /// <inheritdoc/>
        public override IColor GetInitializeColor()
        {
            // Setting the current stroking or nonstroking colour space to any CIE-based colour space shall
            // initialize all components of the corresponding current colour to 0.0 (unless the range of valid
            // values for a given component does not include 0.0, in which case the nearest valid value shall
            // be substituted.)
            return GetColor(0);
        }

        /// <inheritdoc/>
        public override void GetRgb(ReadOnlySpan<double> values, out double r, out double g, out double b)
        {
            double a = values[0];
            (r, g, b) = colorSpaceTransformer.TransformToRGB((a, a, a));
        }
    }

    /// <summary>
    /// CIE (Commission Internationale de l'Éclairage) colorspace.
    /// Specifies color related to human visual perception with the aim of producing consistent color on different output devices.
    /// CalRGB - A CIE ABC color space with a single transformation.
    /// A, B and C represent red, green and blue color values in the range 0.0 to 1.0.
    /// </summary>
    public sealed class CalRGBColorSpaceDetails : ColorSpaceDetails
    {
        /// <inheritdoc/>
        public override int NumberOfColorComponents => 3;

        /// <inheritdoc/>
        public override int BaseNumberOfColorComponents => NumberOfColorComponents;

        private readonly CIEBasedColorSpaceTransformer colorSpaceTransformer;

        /// <summary>
        /// An array of three numbers [XW  YW  ZW] specifying the tristimulus value, in the CIE 1931 XYZ space of the
        /// diffuse white point. The numbers XW and ZW shall be positive, and YW shall be equal to 1.0.
        /// </summary>
        public IReadOnlyList<double> WhitePoint { get; }

        /// <summary>
        /// An array of three numbers [XB  YB  ZB] specifying the tristimulus value, in the CIE 1931 XYZ space of the
        /// diffuse black point. All three numbers must be non-negative. Default value: [0.0  0.0  0.0].
        /// </summary>
        public IReadOnlyList<double> BlackPoint { get; }

        /// <summary>
        /// An array of three numbers [GR  GG  GB] specifying the gamma for the red, green and blue (A, B, C) components
        /// of the color space. Default value: [1.0  1.0  1.0].
        /// </summary>
        public IReadOnlyList<double> Gamma { get; }

        /// <summary>
        /// An array of nine numbers [XA  YA  ZA  XB  YB  ZB  XC  YC  ZC] specifying the linear interpretation of the
        /// decoded A, B, C components of the color space with respect to the final XYZ representation. Default value:
        /// [1  0  0  0  1  0  0  0  1].
        /// </summary>
        public IReadOnlyList<double> Matrix { get; }

        /// <summary>
        /// Create a new <see cref="CalRGBColorSpaceDetails"/>.
        /// </summary>
        public CalRGBColorSpaceDetails(double[] whitePoint, double[]? blackPoint, double[]? gamma, double[]? matrix)
            : base(ColorSpace.CalRGB)
        {
            WhitePoint = whitePoint ?? throw new ArgumentNullException(nameof(whitePoint));
            if (WhitePoint.Count != 3)
            {
                throw new ArgumentOutOfRangeException(nameof(whitePoint), whitePoint, $"Must consist of exactly three numbers, but was passed {whitePoint.Length}.");
            }

            BlackPoint = blackPoint ?? [0.0, 0, 0];
            if (BlackPoint.Count != 3)
            {
                throw new ArgumentOutOfRangeException(nameof(blackPoint), blackPoint, $"Must consist of exactly three numbers, but was passed {blackPoint!.Length}.");
            }

            Gamma = gamma ?? [1.0, 1, 1];
            if (Gamma.Count != 3)
            {
                throw new ArgumentOutOfRangeException(nameof(gamma), gamma, $"Must consist of exactly three numbers, but was passed {gamma!.Length}.");
            }

            Matrix = matrix ?? [1.0, 0, 0, 0, 1, 0, 0, 0, 1];
            if (Matrix.Count != 9)
            {
                throw new ArgumentOutOfRangeException(nameof(matrix), matrix, $"Must consist of exactly nine numbers, but was passed {matrix!.Length}.");
            }

            colorSpaceTransformer =
                new CIEBasedColorSpaceTransformer((WhitePoint[0], WhitePoint[1], WhitePoint[2]), RGBWorkingSpace.sRGB)
                {
                    DecoderABC = color => (
                    Math.Pow(color.A, Gamma[0]),
                    Math.Pow(color.B, Gamma[1]),
                    Math.Pow(color.C, Gamma[2])),

                    MatrixABC = new Matrix3x3(
                    Matrix[0], Matrix[3], Matrix[6],
                    Matrix[1], Matrix[4], Matrix[7],
                    Matrix[2], Matrix[5], Matrix[8])
                };
        }

        /// <inheritdoc/>
        internal override Span<byte> Transform(Span<byte> decoded)
        {
            var transformed = new byte[decoded.Length];
            int index = 0;
            Span<double> input = stackalloc double[3];

            for (var i = 0; i < decoded.Length; i += 3)
            {
                input[0] = decoded[i] / 255.0;
                input[1] = decoded[i + 1] / 255.0;
                input[2] = decoded[i + 2] / 255.0;
                GetRgb(input, out double r, out double g, out double b);
                transformed[index++] = ConvertToByte(r);
                transformed[index++] = ConvertToByte(g);
                transformed[index++] = ConvertToByte(b);
            }

            return transformed;
        }

        /// <inheritdoc/>
        internal override double[] Process(params double[] values)
        {
            GetRgb(values, out double r, out double g, out double b);
            return [r, g, b];
        }

        /// <inheritdoc/>
        public override IColor GetColor(params double[] values)
        {
            if (values is null || values.Length != NumberOfColorComponents)
            {
                throw new ArgumentException($"Invalid number of inputs, expecting {NumberOfColorComponents} but got {values?.Length ?? 0}", nameof(values));
            }

            GetRgb(values, out double r, out double g, out double b);
            return new RGBColor(r, g, b);
        }

        /// <inheritdoc/>
        public override IColor GetInitializeColor()
        {
            // Setting the current stroking or nonstroking colour space to any CIE-based colour space shall
            // initialize all components of the corresponding current colour to 0.0 (unless the range of valid
            // values for a given component does not include 0.0, in which case the nearest valid value shall
            // be substituted.)
            Span<double> zero = stackalloc double[3];
            GetRgb(zero, out double r, out double g, out double b);
            return new RGBColor(r, g, b);
        }

        /// <inheritdoc/>
        public override void GetRgb(ReadOnlySpan<double> values, out double r, out double g, out double b)
        {
            (r, g, b) = colorSpaceTransformer.TransformToRGB((values[0], values[1], values[2]));
        }
    }

    /// <summary>
    /// CIE (Commission Internationale de l'Éclairage) colorspace.
    /// Specifies color related to human visual perception with the aim of producing consistent color on different output devices.
    /// CalRGB - A CIE ABC color space with a single transformation.
    /// A, B and C represent red, green and blue color values in the range 0.0 to 1.0.
    /// </summary>
    public sealed class LabColorSpaceDetails : ColorSpaceDetails
    {
        private readonly CIEBasedColorSpaceTransformer colorSpaceTransformer;

        /// <inheritdoc/>
        public override int NumberOfColorComponents => 3;

        /// <inheritdoc/>
        public override int BaseNumberOfColorComponents => NumberOfColorComponents;

        /// <summary>
        /// An array of three numbers [XW  YW  ZW] specifying the tristimulus value, in the CIE 1931 XYZ space of the
        /// diffuse white point. The numbers XW and ZW shall be positive, and YW shall be equal to 1.0.
        /// </summary>
        public IReadOnlyList<double> WhitePoint { get; }

        /// <summary>
        /// An array of three numbers [XB  YB  ZB] specifying the tristimulus value, in the CIE 1931 XYZ space of the
        /// diffuse black point. All three numbers must be non-negative. Default value: [0.0  0.0  0.0].
        /// </summary>
        public IReadOnlyList<double> BlackPoint { get; }

        /// <summary>
        /// An array of four numbers [a_min a_max b_min b_max] that shall specify the range of valid values for the a* and b* (B and C)
        /// components of the colour space — that is, a_min ≤ a* ≤ a_max and b_min ≤ b* ≤ b_max
        /// <para>Component values falling outside the specified range shall be adjusted to the nearest valid value without error indication.</para>
        /// Default value: [−100 100 −100 100].
        /// </summary>
        public IReadOnlyList<double> Matrix { get; }

        /// <summary>
        /// Create a new <see cref="LabColorSpaceDetails"/>.
        /// </summary>
        public LabColorSpaceDetails(double[] whitePoint, double[]? blackPoint, double[]? matrix)
            : base(ColorSpace.Lab)
        {
            WhitePoint = whitePoint ?? throw new ArgumentNullException(nameof(whitePoint));
            if (whitePoint.Length != 3)
            {
                throw new ArgumentOutOfRangeException(nameof(whitePoint), whitePoint, $"Must consist of exactly three numbers, but was passed {whitePoint.Length}.");
            }

            BlackPoint = blackPoint ?? [0.0, 0.0, 0.0];
            if (BlackPoint.Count != 3)
            {
                throw new ArgumentOutOfRangeException(nameof(blackPoint), blackPoint, $"Must consist of exactly three numbers, but was passed {blackPoint!.Length}.");
            }

            Matrix = matrix ?? [-100.0, 100.0, -100.0, 100.0];
            if (Matrix.Count != 4)
            {
                throw new ArgumentOutOfRangeException(nameof(matrix), matrix, $"Must consist of exactly four numbers, but was passed {matrix!.Length}.");
            }

            colorSpaceTransformer = new CIEBasedColorSpaceTransformer((WhitePoint[0], WhitePoint[1], WhitePoint[2]), RGBWorkingSpace.sRGB);
        }

        /// <summary>
        /// Transforms the supplied ABC color to RGB (sRGB) using the properties of this <see cref="LabColorSpaceDetails"/>
        /// in the transformation process.
        /// A, B and C represent the L*, a*, and b* components of a CIE 1976 L*a*b* space. The range of the first (L*)
        /// component shall be 0 to 100; the ranges of the second and third (a* and b*) components shall be defined by
        /// the Range entry in the colour space dictionary
        /// </summary>
        /// <inheritdoc/>
        internal override Span<byte> Transform(Span<byte> decoded)
        {
            var transformed = new byte[decoded.Length];
            int index = 0;
            Span<double> input = stackalloc double[3];

            for (var i = 0; i < decoded.Length; i += 3)
            {
                input[0] = decoded[i] / 255.0;
                input[1] = decoded[i + 1] / 255.0;
                input[2] = decoded[i + 2] / 255.0;
                GetRgb(input, out double r, out double g, out double b);
                transformed[index++] = ConvertToByte(r);
                transformed[index++] = ConvertToByte(g);
                transformed[index++] = ConvertToByte(b);
            }

            return transformed;
        }

        private static double g(double x)
        {
            if (x > 6.0 / 29.0)
            {
                return x * x * x;
            }
            return 108.0 / 841.0 * (x - 4.0 / 29.0);
        }

        /// <inheritdoc/>
        internal override double[] Process(params double[] values)
        {
            GetRgb(values, out double r, out double g, out double b);
            return [r, g, b];
        }

        /// <inheritdoc/>
        public override IColor GetColor(params double[] values)
        {
            if (values is null || values.Length != NumberOfColorComponents)
            {
                throw new ArgumentException($"Invalid number of inputs, expecting {NumberOfColorComponents} but got {values?.Length ?? 0}", nameof(values));
            }

            GetRgb(values, out double r, out double g, out double b);
            return new RGBColor(r, g, b);
        }

        /// <inheritdoc/>
        public override IColor GetInitializeColor()
        {
            // Setting the current stroking or nonstroking colour space to any CIE-based colour space shall
            // initialize all components of the corresponding current colour to 0.0 (unless the range of valid
            // values for a given component does not include 0.0, in which case the nearest valid value shall
            // be substituted.)
            double b = PdfFunction.ClipToRange(0, Matrix[0], Matrix[1]);
            double c = PdfFunction.ClipToRange(0, Matrix[2], Matrix[3]);
            Span<double> init = stackalloc double[3] { 0, b, c };
            GetRgb(init, out double rr, out double gg, out double bb);
            return new RGBColor(rr, gg, bb);
        }

        /// <inheritdoc/>
        public override void GetRgb(ReadOnlySpan<double> values, out double r, out double g, out double b)
        {
            // Component Ranges: L*: [0 100]; a* and b*: [-128 127]
            double bClip = PdfFunction.ClipToRange(values[1], Matrix[0], Matrix[1]);
            double cClip = PdfFunction.ClipToRange(values[2], Matrix[2], Matrix[3]);

            double M = (values[0] + 16.0) / 116.0;
            double L = M + (bClip / 500.0);
            double N = M - (cClip / 200.0);

            double X = WhitePoint[0] * LabColorSpaceDetails.g(L);
            double Y = WhitePoint[1] * LabColorSpaceDetails.g(M);
            double Z = WhitePoint[2] * LabColorSpaceDetails.g(N);

            (r, g, b) = colorSpaceTransformer.TransformToRGB((X, Y, Z));
        }
    }

    /// <summary>
    /// The ICCBased color space is one of the CIE-based color spaces supported in PDFs. These color spaces
    /// enable a page description to specify color values in a way that is related to human visual perception.
    /// The goal is for the same color specification to produce consistent results on different output devices,
    /// within the limitations of each device.
    /// <para>
    /// Currently support for this color space is limited in PdfPig. Calculations will only be based on
    /// the color space of <see cref="AlternateColorSpace"/>.
    /// </para>
    /// </summary>
    public sealed class ICCBasedColorSpaceDetails : ColorSpaceDetails
    {
        /// <summary>
        /// The number of color components in the color space described by the ICC profile data.
        /// This numbers shall match the number of components actually in the ICC profile.
        /// Valid values are 1, 3 and 4.
        /// </summary>
        public override int NumberOfColorComponents { get; }

        /// <inheritdoc/>
        public override int BaseNumberOfColorComponents => NumberOfColorComponents;

        /// <summary>
        /// An alternate color space that can be used in case the one specified in the stream data is not
        /// supported. Non-conforming readers may use this color space. The alternate color space may be any
        /// valid color space (except a Pattern color space). If this property isn't explicitly set during
        /// construction, it will assume one of the color spaces, DeviceGray, DeviceRGB or DeviceCMYK depending
        /// on whether the value of <see cref="NumberOfColorComponents"/> is 1, 3 or respectively.
        /// <para>
        /// Conversion of the source color values should not be performed when using the alternate color space.
        /// Color values within the range of the ICCBased color space might not be within the range of the
        /// alternate color space. In this case, the nearest values within the range of the alternate space
        /// must be substituted.
        /// </para>
        /// </summary>
        public ColorSpaceDetails AlternateColorSpace { get; }

        /// <summary>
        /// A list of 2 x <see cref="NumberOfColorComponents"/> numbers [min0 max0  min1 max1  ...] that
        /// specifies the minimum and maximum valid values of the corresponding color components. These
        /// values must match the information in the ICC profile. Default value: [0.0 1.0  0.0 1.0  ...].
        /// </summary>
        public IReadOnlyList<double> Range { get; }

        /// <summary>
        /// An optional metadata stream that contains metadata for the color space.
        /// </summary>
        public XmpMetadata? Metadata { get; }

        /// <summary>
        /// Create a new <see cref="ICCBasedColorSpaceDetails"/>.
        /// </summary>
        internal ICCBasedColorSpaceDetails(int numberOfColorComponents,
            ColorSpaceDetails? alternateColorSpaceDetails,
            IReadOnlyList<double>? range,
            XmpMetadata? metadata)
            : base(ColorSpace.ICCBased)
        {
            if (numberOfColorComponents != 1 && numberOfColorComponents != 3 && numberOfColorComponents != 4)
            {
                throw new ArgumentOutOfRangeException(nameof(numberOfColorComponents), "must be 1, 3 or 4");
            }

            NumberOfColorComponents = numberOfColorComponents;
            AlternateColorSpace = alternateColorSpaceDetails ??
                (NumberOfColorComponents == 1 ? DeviceGrayColorSpaceDetails.Instance :
                NumberOfColorComponents == 3 ? DeviceRgbColorSpaceDetails.Instance : DeviceCmykColorSpaceDetails.Instance);

            BaseType = AlternateColorSpace.BaseType;
            Range = range ??
                Enumerable.Range(0, numberOfColorComponents).Select(x => new[] { 0.0, 1.0 }).SelectMany(x => x).ToArray();
            if (Range.Count != 2 * numberOfColorComponents)
            {
                throw new ArgumentOutOfRangeException(nameof(range), range,
                    $"Must consist of exactly {2 * numberOfColorComponents} (2 x NumberOfColorComponents), but was passed {range?.Count ?? 0}");
            }
            Metadata = metadata;
        }

        /// <inheritdoc/>
        internal override double[] Process(params double[] values)
        {
            // TODO - use ICC profile

            return AlternateColorSpace.Process(values);
        }

        /// <inheritdoc/>
        public override IColor GetColor(params double[] values)
        {
            if (values is null || values.Length != NumberOfColorComponents)
            {
                throw new ArgumentException($"Invalid number of inputs, expecting {NumberOfColorComponents} but got {values?.Length ?? 0}", nameof(values));
            }

            // TODO - use ICC profile

            for (int c = 0; c < values.Length; c++)
            {
                int i = 2 * c;
                values[c] = PdfFunction.ClipToRange(values[c], Range[i], Range[i + 1]);
            }

            return AlternateColorSpace.GetColor(values);
        }

        /// <inheritdoc/>
        public override IColor GetInitializeColor()
        {
            // Setting the current stroking or nonstroking colour space to any CIE-based colour space shall
            // initialize all components of the corresponding current colour to 0.0 (unless the range of valid
            // values for a given component does not include 0.0, in which case the nearest valid value shall
            // be substituted.)
            double v = PdfFunction.ClipToRange(0.0, Range[0], Range[1]);
            double[] init = Enumerable.Repeat(v, NumberOfColorComponents).ToArray();
            return GetColor(init);
        }

        /// <inheritdoc/>
        public override void GetRgb(ReadOnlySpan<double> values, out double r, out double g, out double b)
        {
            // TODO - use ICC profile
            int n = NumberOfColorComponents;
            if (n <= 4)
            {
                Span<double> clipped = stackalloc double[4];
                for (int c = 0; c < n; c++)
                {
                    int i = 2 * c;
                    clipped[c] = PdfFunction.ClipToRange(values[c], Range[i], Range[i + 1]);
                }
                AlternateColorSpace.GetRgb(clipped.Slice(0, n), out r, out g, out b);
            }
            else
            {
                double[] clipped = new double[n];
                for (int c = 0; c < n; c++)
                {
                    int i = 2 * c;
                    clipped[c] = PdfFunction.ClipToRange(values[c], Range[i], Range[i + 1]);
                }
                AlternateColorSpace.GetRgb(clipped, out r, out g, out b);
            }
        }

        /// <inheritdoc/>
        internal override Span<byte> Transform(Span<byte> decoded)
        {
            // TODO - use ICC profile

            return AlternateColorSpace.Transform(decoded);
        }
    }

    /// <summary>
    /// Pattern color space.
    /// </summary>
    public sealed class PatternColorSpaceDetails : ColorSpaceDetails
    {
        /// <summary>
        /// The pattern dictionary.
        /// </summary>
        public IReadOnlyDictionary<NameToken, PatternColor> Patterns { get; }

        /// <summary>
        /// <inheritdoc/>
        /// <para>
        /// Cannot be called for <see cref="PatternColorSpaceDetails"/>, will throw a <see cref="InvalidOperationException"/>.
        /// </para>
        /// </summary>
        public override int NumberOfColorComponents => throw new InvalidOperationException("PatternColorSpaceDetails");

        /// <summary>
        /// <inheritdoc/>
        /// <para>
        /// Valid for Uncoloured Tiling Patterns. Will throw a <see cref="InvalidOperationException"/> otherwise.
        /// </para>
        /// </summary>
        public override int BaseNumberOfColorComponents => UnderlyingColourSpace!.NumberOfColorComponents;

        /// <summary>
        /// The underlying color space for Uncoloured Tiling Patterns.
        /// </summary>
        public ColorSpaceDetails? UnderlyingColourSpace { get; }

        /// <summary>
        /// Create a new <see cref="PatternColorSpaceDetails"/>.
        /// </summary>
        /// <param name="patterns">The patterns.</param>
        /// <param name="underlyingColourSpace">The underlying colour space for Uncoloured Tiling Patterns.</param>
        public PatternColorSpaceDetails(IReadOnlyDictionary<NameToken, PatternColor> patterns, ColorSpaceDetails underlyingColourSpace)
            : base(ColorSpace.Pattern)
        {
            Patterns = patterns ?? throw new ArgumentNullException(nameof(patterns));
            UnderlyingColourSpace = underlyingColourSpace;
        }

        /// <summary>
        /// Get the corresponding <see cref="PatternColor"/>.
        /// </summary>
        /// <param name="name"></param>
        public PatternColor GetColor(NameToken name)
        {
            return Patterns[name];
        }

        /// <summary>
        /// <inheritdoc/>
        /// <para>
        /// Cannot be called for <see cref="PatternColorSpaceDetails"/>, will throw a <see cref="InvalidOperationException"/>.
        /// </para>
        /// </summary>
        internal override double[] Process(params double[] values)
        {
            throw new InvalidOperationException("PatternColorSpaceDetails");
        }

        /// <summary>
        /// <inheritdoc/>
        /// <para>
        /// Cannot be called for <see cref="PatternColorSpaceDetails"/>, will throw a <see cref="InvalidOperationException"/>.
        /// Use <see cref="GetColor(NameToken)"/> instead.
        /// </para>
        /// </summary>
        public override IColor GetColor(params double[] values)
        {
            throw new InvalidOperationException("PatternColorSpaceDetails");
        }

        /// <inheritdoc/>
        public override void GetRgb(ReadOnlySpan<double> values, out double r, out double g, out double b)
        {
            throw new InvalidOperationException("PatternColorSpaceDetails");
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <returns>Always returns <c>null</c>.</returns>
        public override IColor? GetInitializeColor()
        {
            return null;
        }

        /// <summary>
        /// <inheritdoc/>
        /// <para>
        /// Cannot be called for <see cref="PatternColorSpaceDetails"/>, will throw a <see cref="InvalidOperationException"/>.
        /// </para>
        /// </summary>
        internal override Span<byte> Transform(Span<byte> decoded)
        {
            throw new InvalidOperationException("PatternColorSpaceDetails");
        }
    }

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
        internal override double[] Process(params double[] values)
        {
            throw new InvalidOperationException("UnsupportedColorSpaceDetails");
        }

        /// <inheritdoc/>
        public override IColor GetColor(params double[] values)
        {
            throw new InvalidOperationException("UnsupportedColorSpaceDetails");
        }

        /// <inheritdoc/>
        public override void GetRgb(ReadOnlySpan<double> values, out double r, out double g, out double b)
        {
            throw new InvalidOperationException("UnsupportedColorSpaceDetails");
        }

        /// <inheritdoc/>
        public override IColor? GetInitializeColor()
        {
            throw new InvalidOperationException("UnsupportedColorSpaceDetails");
        }

        /// <inheritdoc/>
        internal override Span<byte> Transform(Span<byte> decoded)
        {
            throw new InvalidOperationException("UnsupportedColorSpaceDetails");
        }
    }
}
