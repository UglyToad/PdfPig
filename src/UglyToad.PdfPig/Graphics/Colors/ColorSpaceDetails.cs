namespace UglyToad.PdfPig.Graphics.Colors
{
    using IccProfileNet;
    using IccProfileNet.Tags;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Tokens;
    using UglyToad.PdfPig.Content;
    using UglyToad.PdfPig.Functions;
    using UglyToad.PdfPig.Util;
    using UglyToad.PdfPig.Util.JetBrains.Annotations;

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
        /// The underlying type of ColorSpace, usually equal to <see cref="Type"/>
        /// unless <see cref="ColorSpace.Indexed"/>.
        /// </summary>
        public ColorSpace BaseType { get; protected set; }

        /// <summary>
        /// The number of components for the underlying color space.
        /// </summary>
        internal abstract int BaseNumberOfColorComponents { get; }

        /// <summary>
        /// Create a new <see cref="ColorSpaceDetails"/>.
        /// </summary>
        protected ColorSpaceDetails(ColorSpace type)
        {
            Type = type;
            BaseType = type;
        }

        /// <summary>
        /// Get the color.
        /// </summary>
        public abstract IColor GetColor(params double[] values);

        /// <summary>
        /// Get the color, without check and caching.
        /// </summary>
        internal abstract double[] Process(params double[] values);

        /// <summary>
        /// Get the color that initialize the current stroking or nonstroking colour.
        /// </summary>
        public abstract IColor GetInitializeColor();

        /// <summary>
        /// Transform image bytes.
        /// </summary>
        internal abstract IReadOnlyList<byte> Transform(IReadOnlyList<byte> decoded);

        /// <summary>
        /// Convert to byte.
        /// </summary>
        protected static byte ConvertToByte(double componentValue)
        {
            var rounded = Math.Round(componentValue * 255, MidpointRounding.AwayFromZero);
            return (byte)rounded;
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
        internal override int BaseNumberOfColorComponents => NumberOfColorComponents;

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
            if (values == null || values.Length != NumberOfColorComponents)
            {
                throw new ArgumentException($"Invalid number of imputs, expecting {NumberOfColorComponents} but got {values.Length}", nameof(values));
            }

            double gray = values[0];
            if (gray == 0)
            {
                return GrayColor.Black;
            }
            else if (gray == 1)
            {
                return GrayColor.White;
            }
            else
            {
                return new GrayColor((decimal)gray);
            }
        }

        /// <inheritdoc/>
        public override IColor GetInitializeColor()
        {
            return GrayColor.Black;
        }

        /// <inheritdoc/>
        internal override IReadOnlyList<byte> Transform(IReadOnlyList<byte> decoded)
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
        internal override int BaseNumberOfColorComponents => NumberOfColorComponents;

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
            if (values == null || values.Length != NumberOfColorComponents)
            {
                throw new ArgumentException($"Invalid number of imputs, expecting {NumberOfColorComponents} but got {values.Length}", nameof(values));
            }

            double r = values[0];
            double g = values[1];
            double b = values[2];
            if (r == 0 && g == 0 && b == 0)
            {
                return RGBColor.Black;
            }
            else if (r == 1 && g == 1 && b == 1)
            {
                return RGBColor.White;
            }

            return new RGBColor((decimal)r, (decimal)g, (decimal)b);
        }

        /// <inheritdoc/>
        public override IColor GetInitializeColor()
        {
            return RGBColor.Black;
        }

        /// <inheritdoc/>
        internal override IReadOnlyList<byte> Transform(IReadOnlyList<byte> decoded)
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
        internal override int BaseNumberOfColorComponents => NumberOfColorComponents;

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
            if (values == null || values.Length != NumberOfColorComponents)
            {
                throw new ArgumentException($"Invalid number of imputs, expecting {NumberOfColorComponents} but got {values.Length}", nameof(values));
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

            return new CMYKColor((decimal)c, (decimal)m, (decimal)y, (decimal)k);
        }

        /// <inheritdoc/>
        public override IColor GetInitializeColor()
        {
            return CMYKColor.Black;
        }

        /// <inheritdoc/>
        internal override IReadOnlyList<byte> Transform(IReadOnlyList<byte> decoded)
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
        /// Creates a indexed color space useful for extracting stencil masks as black-and-white images,
        /// i.e. with a color palette of two colors (black and white). If the decode parameter array is
        /// [0, 1] it indicates that black is at index 0 in the color palette, whereas [1, 0] indicates
        /// that the black color is at index 1.
        /// </summary>
        internal static ColorSpaceDetails Stencil(ColorSpaceDetails colorSpaceDetails, decimal[] decode)
        {
            var blackIsOne = decode.Length >= 2 && decode[0] == 1 && decode[1] == 0;
            return new IndexedColorSpaceDetails(colorSpaceDetails, 1, blackIsOne ? new byte[] { 255, 0 } : new byte[] { 0, 255 });
        }

        /// <inheritdoc/>
        public override int NumberOfColorComponents => 1;

        /// <summary>
        /// <inheritdoc/>
        /// <para>In the case of <see cref="IndexedColorSpaceDetails"/>, gets the <see cref="BaseColorSpaceDetails"/>' <c>BaseNumberOfColorComponents</c>.</para>
        /// </summary>
        internal override int BaseNumberOfColorComponents => BaseColorSpaceDetails.BaseNumberOfColorComponents;

        /// <summary>
        /// The base color space in which the values in the color table are to be interpreted.
        /// It can be any device or CIE-based color space or (in PDF 1.3) a Separation or DeviceN space,
        /// but not a Pattern space or another Indexed space.
        /// </summary>
        public ColorSpaceDetails BaseColorSpaceDetails { get; }

        /// <summary>
        /// An integer that specifies the maximum valid index value. Can be no greater than 255.
        /// </summary>
        public byte HiVal { get; }

        /// <summary>
        /// Provides the mapping between index values and the corresponding colors in the base color space.
        /// </summary>
        public IReadOnlyList<byte> ColorTable { get; }

        /// <summary>
        /// Create a new <see cref="IndexedColorSpaceDetails"/>.
        /// </summary>
        public IndexedColorSpaceDetails(ColorSpaceDetails baseColorSpaceDetails, byte hiVal, IReadOnlyList<byte> colorTable)
            : base(ColorSpace.Indexed)
        {
            BaseColorSpaceDetails = baseColorSpaceDetails ?? throw new ArgumentNullException(nameof(baseColorSpaceDetails));
            HiVal = hiVal;
            ColorTable = colorTable;
            BaseType = baseColorSpaceDetails.Type;
        }

        /// <inheritdoc/>
        internal override double[] Process(params double[] values)
        {
            var csBytes = UnwrapIndexedColorSpaceBytes(new[] { (byte)values[0] });
            return BaseColorSpaceDetails.Process(csBytes.Select(b => b / 255.0).ToArray());
        }

        /// <inheritdoc/>
        public override IColor GetColor(params double[] values)
        {
            if (values == null || values.Length != NumberOfColorComponents)
            {
                throw new ArgumentException($"Invalid number of imputs, expecting {NumberOfColorComponents} but got {values.Length}", nameof(values));
            }

            return cache.GetOrAdd(values[0], v =>
            {
                var csBytes = UnwrapIndexedColorSpaceBytes(new[] { (byte)v });
                return BaseColorSpaceDetails.GetColor(csBytes.Select(b => b / 255.0).ToArray());
            });
        }

        internal byte[] UnwrapIndexedColorSpaceBytes(IReadOnlyList<byte> input)
        {
            var multiplier = 1;
            Func<byte, IEnumerable<byte>> transformer = null;
            switch (BaseType)
            {
                case ColorSpace.DeviceRGB:
                case ColorSpace.CalRGB:
                case ColorSpace.Lab:
                    transformer = x =>
                    {
                        var r = new byte[3];
                        for (var i = 0; i < 3; i++)
                        {
                            r[i] = ColorTable[x * 3 + i];
                        }

                        return r;
                    };
                    multiplier = 3;
                    break;

                case ColorSpace.DeviceCMYK:
                    transformer = x =>
                    {
                        var r = new byte[4];
                        for (var i = 0; i < 4; i++)
                        {
                            r[i] = ColorTable[x * 4 + i];
                        }

                        return r;
                    };

                    multiplier = 4;
                    break;

                case ColorSpace.DeviceGray:
                case ColorSpace.CalGray:
                case ColorSpace.Separation:
                    transformer = x => new[] { ColorTable[x] };
                    multiplier = 1;
                    break;

                case ColorSpace.ICCBased:
                case ColorSpace.DeviceN:
                    transformer = x =>
                    {
                        var r = new byte[BaseColorSpaceDetails.NumberOfColorComponents];
                        for (var i = 0; i < BaseColorSpaceDetails.NumberOfColorComponents; i++)
                        {
                            r[i] = ColorTable[x * BaseColorSpaceDetails.NumberOfColorComponents + i];
                        }

                        return r;
                    };

                    multiplier = BaseColorSpaceDetails.NumberOfColorComponents;
                    break;
            }

            if (transformer != null)
            {
                var result = new byte[input.Count * multiplier];
                var i = 0;
                foreach (var b in input)
                {
                    foreach (var newByte in transformer(b))
                    {
                        result[i++] = newByte;
                    }
                }

                return result;
            }

            return input.ToArray();
        }

        /// <inheritdoc/>
        public override IColor GetInitializeColor()
        {
            // Setting the current stroking or nonstroking colour space to an Indexed colour space shall
            // initialize the corresponding current colour to 0.
            return GetColor(0);
        }

        /// <summary>
        /// <inheritdoc/>
        /// <para>
        /// Unwrap then transform using base color space details.
        /// </para>
        /// </summary>
        internal override IReadOnlyList<byte> Transform(IReadOnlyList<byte> decoded)
        {
            var unwraped = UnwrapIndexedColorSpaceBytes(decoded);
            return BaseColorSpaceDetails.Transform(unwraped);
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
        internal override int BaseNumberOfColorComponents => AlternateColorSpaceDetails.NumberOfColorComponents;

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
        public ColorSpaceDetails AlternateColorSpaceDetails { get; }

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
        /// That is, the number of components and the interpretation of their values depend on the <see cref="AlternateColorSpaceDetails"/>.
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
            AlternateColorSpaceDetails = alternateColorSpaceDetails;
            Attributes = attributes;
            TintFunction = tintFunction;
            BaseType = alternateColorSpaceDetails.Type;
        }

        /// <inheritdoc/>
        internal override double[] Process(params double[] values)
        {
            var evaled = TintFunction.Eval(values);
            return AlternateColorSpaceDetails.Process(evaled);
        }

        /// <inheritdoc/>
        public override IColor GetColor(params double[] values)
        {
            if (values == null || values.Length != NumberOfColorComponents)
            {
                throw new ArgumentException($"Invalid number of imputs, expecting {NumberOfColorComponents} but got {values.Length}", nameof(values));
            }

            // TODO - use attributes

            // TODO - caching
            var evaled = TintFunction.Eval(values);
            return AlternateColorSpaceDetails.GetColor(evaled);
        }

        /// <inheritdoc/>
        internal override IReadOnlyList<byte> Transform(IReadOnlyList<byte> decoded)
        {
            int outputCount = Process(Enumerable.Repeat(1.0, NumberOfColorComponents).ToArray()).Length;
            int outputSize = (int)(decoded.Count * outputCount / (double)NumberOfColorComponents);
            var transformed = new byte[outputSize];

            Parallel.For(0, decoded.Count / NumberOfColorComponents, i =>
            {
                double[] comps = new double[NumberOfColorComponents];
                for (int n = 0; n < NumberOfColorComponents; n++)
                {
                    comps[n] = decoded[i * NumberOfColorComponents + n] / 255.0;
                }

                var colors = Process(comps);
                for (int c = 0; c < outputCount; c++)
                {
                    transformed[i * outputCount + c] = ConvertToByte(colors[c]);
                }
            });
            return transformed;
        }

        /*
        
        /// <inheritdoc/>
        internal override IReadOnlyList<byte> Transform(IReadOnlyList<byte> decoded)
        {
            int outputCount = Process(Enumerable.Repeat(1.0, NumberOfColorComponents).ToArray()).Length;
            int outputSize = (int)(decoded.Count * outputCount / (double)NumberOfColorComponents);
            var transformed = new byte[outputSize];

            //Parallel.For(0, )
            for (var i = 0; i < decoded.Count / NumberOfColorComponents; i++)
            {
                double[] comps = new double[NumberOfColorComponents];
                for (int n = 0; n < NumberOfColorComponents; n++)
                {
                    comps[n] = decoded[i* NumberOfColorComponents + n] / 255.0;
                }

                var colors = Process(comps);
                for (int c = 0; c < outputCount; c++)
                {
                    transformed[i * outputCount + c] = ConvertToByte(colors[c]);
                }
            }

            return transformed;
        }
         */

        /*
        /// <inheritdoc/>
        internal override IReadOnlyList<byte> Transform(IReadOnlyList<byte> decoded)
        {
            int outputCount = Process(Enumerable.Repeat(1.0, NumberOfColorComponents).ToArray()).Length;
            int outputSize = (int)(decoded.Count * outputCount / (double)NumberOfColorComponents);

            var transformed = new byte[outputSize];
            for (var i = 0; i < decoded.Count; i += NumberOfColorComponents)
            {
                double[] comps = new double[NumberOfColorComponents];
                for (int n = 0; n < NumberOfColorComponents; n++)
                {
                    comps[n] = decoded[i + n] / 255.0;
                }

                var colors = Process(comps);
                for (int c = 0; c < outputCount; c++)
                {
                    transformed[(i / NumberOfColorComponents) * outputCount + c] = ConvertToByte(colors[c]);
                }
            }

            return transformed;
        }
        */

        /*
        /// <inheritdoc/>
        internal override IReadOnlyList<byte> Transform(IReadOnlyList<byte> decoded)
        {
            int outputCount = Process(Enumerable.Repeat(1.0, NumberOfColorComponents).ToArray()).Length;

            var transformed = new List<byte>();
            for (var i = 0; i < decoded.Count; i += NumberOfColorComponents)
            {
                double[] comps = new double[NumberOfColorComponents];
                for (int n = 0; n < NumberOfColorComponents; n++)
                {
                    comps[n] = decoded[i + n] / 255.0;
                }

                var colors = Process(comps);
                for (int c = 0; c < outputCount; c++)
                {
                    transformed.Add(ConvertToByte(colors[c]));
                }
            }

            return transformed;
        }
        */

        /// <inheritdoc/>
        public override IColor GetInitializeColor()
        {
            // When this space is set to the current colour space (using the CS or cs operators), each component
            // shall be given an initial value of 1.0. The SCN and scn operators respectively shall set the current
            // stroking and nonstroking colour.
            return GetColor(Enumerable.Repeat(1.0, NumberOfColorComponents).ToArray());
        }

        /// <summary>
        /// DeviceN Color Space Attributes.
        /// </summary>
        public struct DeviceNColorSpaceAttributes
        {
            /// <summary>
            /// A name specifying the preferred treatment for the colour space. Values shall be <c>DeviceN</c> or <c>NChannel</c>. Default value: <c>DeviceN</c>.
            /// </summary>
            public NameToken Subtype { get; }

            /// <summary>
            /// Colorants - dictionary - Required if Subtype is NChannel and the colour space includes spot colorants; otherwise optional.
            /// </summary>
            public DictionaryToken Colorants { get; }

            /// <summary>
            /// Process - dictionary - Required if Subtype is NChannel and the colour space includes components of a process colour space, otherwise optional.
            /// </summary>
            public DictionaryToken Process { get; }

            /// <summary>
            /// MixingHints - dictionary - Optional
            /// </summary>
            public DictionaryToken MixingHints { get; }

            /// <summary>
            /// TODO
            /// </summary>
            public DeviceNColorSpaceAttributes()
            {
                Subtype = NameToken.Devicen;
                Colorants = null;
                Process = null;
                MixingHints = null;
            }

            /// <summary>
            /// TODO
            /// </summary>
            public DeviceNColorSpaceAttributes(NameToken subtype, DictionaryToken colorants, DictionaryToken process, DictionaryToken mixingHints)
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
        internal override int BaseNumberOfColorComponents => AlternateColorSpaceDetails.NumberOfColorComponents;

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
        public ColorSpaceDetails AlternateColorSpaceDetails { get; }

        /// <summary>
        /// During subsequent painting operations, an application calls this function to transform a tint value into
        /// color component values in the alternate color space.
        /// The function is called with the tint value and must return the corresponding color component values.
        /// That is, the number of components and the interpretation of their values depend on the <see cref="AlternateColorSpaceDetails"/>.
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
            AlternateColorSpaceDetails = alternateColorSpaceDetails;
            TintFunction = tintFunction;
        }

        /// <inheritdoc/>
        internal override double[] Process(params double[] values)
        {
            var evaled = TintFunction.Eval(values[0]);
            return AlternateColorSpaceDetails.Process(evaled);
        }

        /// <inheritdoc/>
        public override IColor GetColor(params double[] values)
        {
            if (values == null || values.Length != NumberOfColorComponents)
            {
                throw new ArgumentException($"Invalid number of imputs, expecting {NumberOfColorComponents} but got {values.Length}", nameof(values));
            }

            // TODO - we ignore the name for now

            return cache.GetOrAdd(values[0], v =>
            {
                var evaled = TintFunction.Eval(v);
                return AlternateColorSpaceDetails.GetColor(evaled);
            });
        }

        /// <inheritdoc/>
        internal override IReadOnlyList<byte> Transform(IReadOnlyList<byte> values)
        {
            var transformed = new List<byte>();
            for (var i = 0; i < values.Count; i += 3)
            {
                var colors = Process(values[i++] / 255.0);
                for (int c = 0; c < colors.Length; c++)
                {
                    transformed.Add(ConvertToByte(colors[c]));
                }
            }

            return transformed;
        }

        /// <inheritdoc/>
        public override IColor GetInitializeColor()
        {
            // The initial value for both the stroking and nonstroking colour in the graphics state shall be 1.0.
            return GetColor(1.0);
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
        internal override int BaseNumberOfColorComponents => NumberOfColorComponents;

        private readonly CIEBasedColorSpaceTransformer colorSpaceTransformer;

        /// <summary>
        /// An array of three numbers [XW  YW  ZW] specifying the tristimulus value, in the CIE 1931 XYZ space of the
        /// diffuse white point. The numbers XW and ZW shall be positive, and YW shall be equal to 1.0.
        /// </summary>
        public IReadOnlyList<decimal> WhitePoint { get; }

        /// <summary>
        /// An array of three numbers [XB  YB  ZB] specifying the tristimulus value, in the CIE 1931 XYZ space of the
        /// diffuse black point. All three numbers must be non-negative. Default value: [0.0  0.0  0.0].
        /// </summary>
        public IReadOnlyList<decimal> BlackPoint { get; }

        /// <summary>
        /// A number defining the gamma for the gray (A) component. Gamma must be positive and is generally
        /// greater than or equal to 1. Default value: 1.
        /// </summary>
        public decimal Gamma { get; }

        /// <summary>
        /// Create a new <see cref="CalGrayColorSpaceDetails"/>.
        /// </summary>
        public CalGrayColorSpaceDetails([NotNull] IReadOnlyList<decimal> whitePoint, [CanBeNull] IReadOnlyList<decimal> blackPoint, decimal? gamma)
            : base(ColorSpace.CalGray)
        {
            WhitePoint = whitePoint ?? throw new ArgumentNullException(nameof(whitePoint));
            if (WhitePoint.Count != 3)
            {
                throw new ArgumentOutOfRangeException(nameof(whitePoint), whitePoint, $"Must consist of exactly three numbers, but was passed {whitePoint.Count}.");
            }

            BlackPoint = blackPoint ?? new[] { 0m, 0, 0 }.ToList();
            if (BlackPoint.Count != 3)
            {
                throw new ArgumentOutOfRangeException(nameof(blackPoint), blackPoint, $"Must consist of exactly three numbers, but was passed {blackPoint.Count}.");
            }

            Gamma = gamma ?? 1m;

            colorSpaceTransformer =
                new CIEBasedColorSpaceTransformer(((double)WhitePoint[0], (double)WhitePoint[1], (double)WhitePoint[2]), RGBWorkingSpace.sRGB)
                {
                    DecoderABC = color => (
                    Math.Pow(color.A, (double)Gamma),
                    Math.Pow(color.B, (double)Gamma),
                    Math.Pow(color.C, (double)Gamma)),

                    MatrixABC = new Matrix3x3(
                    (double)WhitePoint[0], 0, 0,
                    0, (double)WhitePoint[1], 0,
                    0, 0, (double)WhitePoint[2])
                };
        }

        /// <summary>
        /// Transforms the supplied A color to grayscale RGB (sRGB) using the propties of this
        /// <see cref="CalGrayColorSpaceDetails"/> in the transformation process.
        /// A represents the gray component of a calibrated gray space. The component must be in the range 0.0 to 1.0.
        /// </summary>
        private RGBColor TransformToRGB(double colorA)
        {
            var (R, G, B) = colorSpaceTransformer.TransformToRGB((colorA, colorA, colorA));
            return new RGBColor((decimal)R, (decimal)G, (decimal)B);
        }

        /// <inheritdoc/>
        internal override IReadOnlyList<byte> Transform(IReadOnlyList<byte> decoded)
        {
            var transformed = new List<byte>();
            for (var i = 0; i < decoded.Count; i++)
            {
                var component = decoded[i] / 255.0;
                var rgbPixel = Process(component);
                // We only need one component here 
                transformed.Add(ConvertToByte(rgbPixel[0]));
            }

            return transformed;
        }

        /// <inheritdoc/>
        internal override double[] Process(params double[] values)
        {
            var (R, _, _) = colorSpaceTransformer.TransformToRGB((values[0], values[0], values[0]));
            return new double[] { R };
        }

        /// <inheritdoc/>
        public override IColor GetColor(params double[] values)
        {
            if (values == null || values.Length != NumberOfColorComponents)
            {
                throw new ArgumentException($"Invalid number of imputs, expecting {NumberOfColorComponents} but got {values.Length}", nameof(values));
            }

            return TransformToRGB(values[0]);
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
        internal override int BaseNumberOfColorComponents => NumberOfColorComponents;

        private readonly CIEBasedColorSpaceTransformer colorSpaceTransformer;

        /// <summary>
        /// An array of three numbers [XW  YW  ZW] specifying the tristimulus value, in the CIE 1931 XYZ space of the
        /// diffuse white point. The numbers XW and ZW shall be positive, and YW shall be equal to 1.0.
        /// </summary>
        public IReadOnlyList<decimal> WhitePoint { get; }

        /// <summary>
        /// An array of three numbers [XB  YB  ZB] specifying the tristimulus value, in the CIE 1931 XYZ space of the
        /// diffuse black point. All three numbers must be non-negative. Default value: [0.0  0.0  0.0].
        /// </summary>
        public IReadOnlyList<decimal> BlackPoint { get; }

        /// <summary>
        /// An array of three numbers [GR  GG  GB] specifying the gamma for the red, green and blue (A, B, C) components
        /// of the color space. Default value: [1.0  1.0  1.0].
        /// </summary>
        public IReadOnlyList<decimal> Gamma { get; }

        /// <summary>
        /// An array of nine numbers [XA  YA  ZA  XB  YB  ZB  XC  YC  ZC] specifying the linear interpretation of the
        /// decoded A, B, C components of the color space with respect to the final XYZ representation. Default value:
        /// [1  0  0  0  1  0  0  0  1].
        /// </summary>
        public IReadOnlyList<decimal> Matrix { get; }

        /// <summary>
        /// Create a new <see cref="CalRGBColorSpaceDetails"/>.
        /// </summary>
        public CalRGBColorSpaceDetails([NotNull] IReadOnlyList<decimal> whitePoint, [CanBeNull] IReadOnlyList<decimal> blackPoint, [CanBeNull] IReadOnlyList<decimal> gamma, [CanBeNull] IReadOnlyList<decimal> matrix)
            : base(ColorSpace.CalRGB)
        {
            WhitePoint = whitePoint ?? throw new ArgumentNullException(nameof(whitePoint));
            if (WhitePoint.Count != 3)
            {
                throw new ArgumentOutOfRangeException(nameof(whitePoint), whitePoint, $"Must consist of exactly three numbers, but was passed {whitePoint.Count}.");
            }

            BlackPoint = blackPoint ?? new[] { 0m, 0, 0 }.ToList();
            if (BlackPoint.Count != 3)
            {
                throw new ArgumentOutOfRangeException(nameof(blackPoint), blackPoint, $"Must consist of exactly three numbers, but was passed {blackPoint.Count}.");
            }

            Gamma = gamma ?? new[] { 1m, 1, 1 }.ToList();
            if (Gamma.Count != 3)
            {
                throw new ArgumentOutOfRangeException(nameof(gamma), gamma, $"Must consist of exactly three numbers, but was passed {gamma.Count}.");
            }

            Matrix = matrix ?? new[] { 1m, 0, 0, 0, 1, 0, 0, 0, 1 }.ToList();
            if (Matrix.Count != 9)
            {
                throw new ArgumentOutOfRangeException(nameof(matrix), matrix, $"Must consist of exactly nine numbers, but was passed {matrix.Count}.");
            }

            colorSpaceTransformer =
                new CIEBasedColorSpaceTransformer(((double)WhitePoint[0], (double)WhitePoint[1], (double)WhitePoint[2]), RGBWorkingSpace.sRGB)
                {
                    DecoderABC = color => (
                    Math.Pow(color.A, (double)Gamma[0]),
                    Math.Pow(color.B, (double)Gamma[1]),
                    Math.Pow(color.C, (double)Gamma[2])),

                    MatrixABC = new Matrix3x3(
                    (double)Matrix[0], (double)Matrix[3], (double)Matrix[6],
                    (double)Matrix[1], (double)Matrix[4], (double)Matrix[7],
                    (double)Matrix[2], (double)Matrix[5], (double)Matrix[8])
                };
        }

        /// <summary>
        /// Transforms the supplied ABC color to RGB (sRGB) using the propties of this <see cref="CalRGBColorSpaceDetails"/>
        /// in the transformation process.
        /// A, B and C represent red, green and blue calibrated color values in the range 0.0 to 1.0.
        /// </summary>
        private RGBColor TransformToRGB((double A, double B, double C) colorAbc)
        {
            var (R, G, B) = colorSpaceTransformer.TransformToRGB((colorAbc.A, colorAbc.B, colorAbc.C));
            return new RGBColor((decimal)R, (decimal)G, (decimal)B);
        }

        /// <inheritdoc/>
        internal override IReadOnlyList<byte> Transform(IReadOnlyList<byte> decoded)
        {
            var transformed = new List<byte>();
            for (var i = 0; i < decoded.Count; i += 3)
            {
                var rgbPixel = Process(decoded[i] / 255.0, decoded[i + 1] / 255.0, decoded[i + 2] / 255.0);
                transformed.Add(ConvertToByte(rgbPixel[0]));
                transformed.Add(ConvertToByte(rgbPixel[1]));
                transformed.Add(ConvertToByte(rgbPixel[2]));
            }

            return transformed;
        }

        /// <inheritdoc/>
        internal override double[] Process(params double[] values)
        {
            var (R, G, B) = colorSpaceTransformer.TransformToRGB((values[0], values[1], values[2]));
            return new double[] { R, G, B };
        }

        /// <inheritdoc/>
        public override IColor GetColor(params double[] values)
        {
            if (values == null || values.Length != NumberOfColorComponents)
            {
                throw new ArgumentException($"Invalid number of imputs, expecting {NumberOfColorComponents} but got {values.Length}", nameof(values));
            }

            return TransformToRGB((values[0], values[1], values[2]));
        }

        /// <inheritdoc/>
        public override IColor GetInitializeColor()
        {
            // Setting the current stroking or nonstroking colour space to any CIE-based colour space shall
            // initialize all components of the corresponding current colour to 0.0 (unless the range of valid
            // values for a given component does not include 0.0, in which case the nearest valid value shall
            // be substituted.)
            return TransformToRGB((0, 0, 0));
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
        internal override int BaseNumberOfColorComponents => NumberOfColorComponents;

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
        public LabColorSpaceDetails([NotNull] IReadOnlyList<decimal> whitePoint, [CanBeNull] IReadOnlyList<decimal> blackPoint, [CanBeNull] IReadOnlyList<decimal> matrix)
            : base(ColorSpace.Lab)
        {
            WhitePoint = whitePoint?.Select(v => (double)v).ToArray() ?? throw new ArgumentNullException(nameof(whitePoint));
            if (WhitePoint.Count != 3)
            {
                throw new ArgumentOutOfRangeException(nameof(whitePoint), whitePoint, $"Must consist of exactly three numbers, but was passed {whitePoint.Count}.");
            }

            BlackPoint = blackPoint?.Select(v => (double)v).ToArray() ?? new[] { 0.0, 0.0, 0.0 };
            if (BlackPoint.Count != 3)
            {
                throw new ArgumentOutOfRangeException(nameof(blackPoint), blackPoint, $"Must consist of exactly three numbers, but was passed {blackPoint.Count}.");
            }

            Matrix = matrix?.Select(v => (double)v).ToArray() ?? new[] { -100.0, 100.0, -100.0, 100.0 };
            if (Matrix.Count != 4)
            {
                throw new ArgumentOutOfRangeException(nameof(matrix), matrix, $"Must consist of exactly four numbers, but was passed {matrix.Count}.");
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
        private RGBColor TransformToRGB((double A, double B, double C) colorAbc)
        {
            var rgb = Process(colorAbc.A, colorAbc.B, colorAbc.C);
            return new RGBColor((decimal)rgb[0], (decimal)rgb[1], (decimal)rgb[2]);
        }

        /// <inheritdoc/>
        internal override IReadOnlyList<byte> Transform(IReadOnlyList<byte> decoded)
        {
            var transformed = new List<byte>();
            for (var i = 0; i < decoded.Count; i += 3)
            {
                var rgbPixel = Process(decoded[i] / 255.0, decoded[i + 1] / 255.0, decoded[i + 2] / 255.0);
                transformed.Add(ConvertToByte(rgbPixel[0]));
                transformed.Add(ConvertToByte(rgbPixel[1]));
                transformed.Add(ConvertToByte(rgbPixel[2]));
            }

            return transformed;
        }

        internal static double g(double x)
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
            // Component Ranges: L*: [0 100]; a* and b*: [−128 127]
            double b = PdfFunction.ClipToRange(values[1], Matrix[0], Matrix[1]);
            double c = PdfFunction.ClipToRange(values[2], Matrix[2], Matrix[3]);

            double M = (values[0] + 16.0) / 116.0;
            double L = M + (b / 500.0);
            double N = M - (c / 200.0);

            double X = WhitePoint[0] * g(L);
            double Y = WhitePoint[1] * g(M);
            double Z = WhitePoint[2] * g(N);

            var (R, G, B) = colorSpaceTransformer.TransformToRGB((X, Y, Z));
            return new double[] { R, G, B };
        }

        /// <inheritdoc/>
        public override IColor GetColor(params double[] values)
        {
            if (values == null || values.Length != NumberOfColorComponents)
            {
                throw new ArgumentException($"Invalid number of imputs, expecting {NumberOfColorComponents} but got {values.Length}", nameof(values));
            }

            return TransformToRGB((values[0], values[1], values[2]));
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
            return TransformToRGB((0, b, c));
        }
    }

    /// <summary>
    /// The ICCBased color space is one of the CIE-based color spaces supported in PDFs. These color spaces
    /// enable a page description to specify color values in a way that is related to human visual perception.
    /// The goal is for the same color specification to produce consistent results on different output devices,
    /// within the limitations of each device.
    /// <para>
    /// Currently support for this color space is limited in PdfPig. Calculations will only be based on
    /// the color space of <see cref="AlternateColorSpaceDetails"/>.
    /// </para>
    /// </summary>
    public sealed class ICCBasedColorSpaceDetails : ColorSpaceDetails
    {
        private readonly CIEBasedColorSpaceTransformer colorSpaceTransformer;

        private readonly LabColorSpaceDetails labColorSpaceDetails;

        /// <summary>
        /// The number of color components in the color space described by the ICC profile data.
        /// This numbers shall match the number of components actually in the ICC profile.
        /// Valid values are 1, 3 and 4.
        /// </summary>
        public override int NumberOfColorComponents { get; }

        /// <inheritdoc/>
        internal override int BaseNumberOfColorComponents => NumberOfColorComponents;

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
        [NotNull]
        public ColorSpaceDetails AlternateColorSpaceDetails { get; }

        /// <summary>
        /// A list of 2 x <see cref="NumberOfColorComponents"/> numbers [min0 max0  min1 max1  ...] that
        /// specifies the minimum and maximum valid values of the corresponding color components. These
        /// values must match the information in the ICC profile. Default value: [0.0 1.0  0.0 1.0  ...].
        /// </summary>
        [NotNull]
        public IReadOnlyList<decimal> Range { get; }

        /// <summary>
        /// An optional metadata stream that contains metadata for the color space.
        /// </summary>
        [CanBeNull]
        public XmpMetadata Metadata { get; }

        /// <summary>
        /// ICC profile.
        /// </summary>
        [CanBeNull]
        internal IccProfile Profile { get; }

        /// <summary>
        /// Create a new <see cref="ICCBasedColorSpaceDetails"/>.
        /// </summary>
        internal ICCBasedColorSpaceDetails(int numberOfColorComponents, [CanBeNull] ColorSpaceDetails alternateColorSpaceDetails,
            [CanBeNull] IReadOnlyList<decimal> range, [CanBeNull] XmpMetadata metadata, [CanBeNull] IReadOnlyList<byte> rawProfile)
            : base(ColorSpace.ICCBased)
        {
            if (numberOfColorComponents != 1 && numberOfColorComponents != 3 && numberOfColorComponents != 4)
            {
                throw new ArgumentOutOfRangeException(nameof(numberOfColorComponents), "must be 1, 3 or 4");
            }

            NumberOfColorComponents = numberOfColorComponents;
            AlternateColorSpaceDetails = alternateColorSpaceDetails ??
                (NumberOfColorComponents == 1 ? (ColorSpaceDetails)DeviceGrayColorSpaceDetails.Instance :
                NumberOfColorComponents == 3 ? (ColorSpaceDetails)DeviceRgbColorSpaceDetails.Instance : (ColorSpaceDetails)DeviceCmykColorSpaceDetails.Instance);

            BaseType = AlternateColorSpaceDetails.BaseType;
            Range = range ??
                Enumerable.Range(0, numberOfColorComponents).Select(x => new[] { 0.0m, 1.0m }).SelectMany(x => x).ToList();
            if (Range.Count != 2 * numberOfColorComponents)
            {
                throw new ArgumentOutOfRangeException(nameof(range), range,
                    $"Must consist of exactly {2 * numberOfColorComponents} (2 x NumberOfColorComponents), but was passed {range.Count}");
            }
            Metadata = metadata;

            if (rawProfile != null)
            {
                System.IO.Directory.CreateDirectory("ICC_Profiles_errors");
                System.IO.File.WriteAllBytes($"ICC_Profiles_errors/ICC_{Guid.NewGuid().ToString().ToLower()}.icc",
                    rawProfile.ToArray());

                try
                {
                    Profile = new IccProfile(rawProfile.ToArray());

                    if (Profile.Header.Pcs == IccProfileConnectionSpace.PCSXYZ)
                    {
                        IccXyz referenceWhite = Profile.Header.nCIEXYZ; // Really not sure
                        colorSpaceTransformer = new CIEBasedColorSpaceTransformer((referenceWhite.X, referenceWhite.Y, referenceWhite.Z), RGBWorkingSpace.sRGB);
                    }
                    else // LAB
                    {
                        if (Profile.Tags.TryGetValue(IccTags.MediaWhitePointTag, out var tag) && tag is IccXyzType whitepoint)
                        {
                            IccXyz referenceWhite = Profile.Header.nCIEXYZ; // Really not sure
                            labColorSpaceDetails = new LabColorSpaceDetails(new decimal[]
                            {
                                (decimal)referenceWhite.X, (decimal)referenceWhite.Y, (decimal)referenceWhite.Z
                            }, null, null);
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"ERROR creating ICC profile: {ex}");

                    System.IO.Directory.CreateDirectory("ICC_Profiles_errors");
                    System.IO.File.WriteAllBytes($"ICC_Profiles_errors/ICC_{Guid.NewGuid().ToString().ToLower()}.icc",
                        rawProfile.ToArray());
                    //throw;
                }
            }
        }

        /// <inheritdoc/>
        internal override double[] Process(params double[] values)
        {
            if (Profile != null)
            {
                if (Profile.Header.ColourSpace == IccColourSpaceType.CMYK && values[0] == 0 && values[1] == 0 && values[2] == 0 && values[3] == 1)
                {
                    // See 'COLOR MANAGEMENT UNDERSTANDING AND USING ICC PROFILES' by Phil Green:
                    // Issues in CMYK Workflows
                    // Pure black (0–0–0–K) turns into four-color C–M–Y–K color build, with resulting color shift,
                    // misregister, and / or trap implications. Also known as the black type problem.
                    return new double[] { 0, 0, 0 }; // Black RGB
                }

                if (Profile.TryProcessToPcs(values, null, out double[] xyz) && xyz.Length == 3) // No rendering intent for now
                {
                    double x = xyz[0];
                    double y = xyz[1];
                    double z = xyz[2];

                    if (Profile.Header.Pcs == IccProfileConnectionSpace.PCSXYZ)
                    {
                        var rgb = colorSpaceTransformer.TransformXYZToRGB((x, y, z));
                        return new double[] { rgb.R, rgb.G, rgb.B };
                    }
                    else
                    {
                        return labColorSpaceDetails.Process(x * 100.0, y * 255.0 - 127.0, z * 255.0 - 127.0); // need to scale
                    }
                }
            }

            return AlternateColorSpaceDetails.Process(values);
        }

        /// <inheritdoc/>
        public override IColor GetColor(params double[] values)
        {
            if (values == null || values.Length != NumberOfColorComponents)
            {
                throw new ArgumentException($"Invalid number of imputs, expecting {NumberOfColorComponents} but got {values.Length}", nameof(values));
            }

            if (Profile != null)
            {
                if (Profile.Header.ColourSpace == IccColourSpaceType.CMYK && values[0] == 0 && values[1] == 0 && values[2] == 0 && values[3] == 1)
                {
                    // See 'COLOR MANAGEMENT UNDERSTANDING AND USING ICC PROFILES' by Phil Green:
                    // Issues in CMYK Workflows
                    // Pure black (0–0–0–K) turns into four-color C–M–Y–K color build, with resulting color shift,
                    // misregister, and / or trap implications. Also known as the black type problem.
                    return RGBColor.Black;
                }

                if (values.Any(x => x > 1.0))
                {
                    //values[0] /= 100.0;
                    //values[1] = (values[1] + 127.0) / 255.0;
                    //values[2] = (values[2] + 127.0) / 255.0;

                    for (int i = 0; i < values.Length; i++)
                    {
                        values[i] /= 100.0;
                    }
                }

                if (Profile.Header.ColourSpace == IccColourSpaceType.CIELABorPCSLAB)
                {
                    //values[0] /= 100.0;
                    //values[1] = (values[1] + 127.0) / 256.0;
                    //values[2] = (values[2] + 127.0) / 256.0;

                    // Component Ranges: L*: [0 100]; a* and b*: [−128 127]
                    //double b = PdfFunction.ClipToRange(values[1], -128.0, 127.0);
                    //double c = PdfFunction.ClipToRange(values[2], -128.0, 127.0);

                    //double M = (values[0] + 16.0) / 116.0;
                    //double L = M + (b / 500.0);
                    //double N = M - (c / 200.0);

                    //IccXyz referenceWhite = Profile.Header.nCIEXYZ;

                    //values[0] = LabColorSpaceDetails.g(L) * referenceWhite.X; // X
                    //values[1] = LabColorSpaceDetails.g(M) * referenceWhite.Y; // Y
                    //values[2] = LabColorSpaceDetails.g(N) * referenceWhite.Z; // Z

                    //var labColor =  labColorSpaceDetails.GetColor(values[0], values[1], values[2]);
                    //values[0] =
                }


                if (Profile.TryProcessToPcs(values, null, out double[] xyz) && xyz.Length == 3) // No rendering intent for now
                {
                    double x = xyz[0];
                    double y = xyz[1];
                    double z = xyz[2];

                    if (Profile.Header.Pcs == IccProfileConnectionSpace.PCSXYZ)
                    {
                        var (R, G, B) = colorSpaceTransformer.TransformXYZToRGB((x, y, z));
                        return new RGBColor((decimal)R, (decimal)G, (decimal)B);
                    }
                    else
                    {
                        return labColorSpaceDetails.GetColor(x * 100.0, y * 255.0 - 127.0, z * 255.0 - 127.0);
                    }
                }
            }

            return AlternateColorSpaceDetails.GetColor(values);
        }

        /// <inheritdoc/>
        public override IColor GetInitializeColor()
        {
            // Setting the current stroking or nonstroking colour space to any CIE-based colour space shall
            // initialize all components of the corresponding current colour to 0.0 (unless the range of valid
            // values for a given component does not include 0.0, in which case the nearest valid value shall
            // be substituted.)
            double v = PdfFunction.ClipToRange(0.0, (double)Range[0], (double)Range[1]);
            double[] init = Enumerable.Repeat(v, NumberOfColorComponents).ToArray();
            return GetColor(init);
        }

        /// <inheritdoc/>
        internal override IReadOnlyList<byte> Transform(IReadOnlyList<byte> decoded)
        {
            if (Profile != null)
            {
                int outputCount = Process(Enumerable.Repeat(1.0, NumberOfColorComponents).ToArray()).Length;
                int outputSize = (int)(decoded.Count * outputCount / (double)NumberOfColorComponents);
                var transformed = new byte[outputSize];

                //Parallel.For(0, decoded.Count / NumberOfColorComponents, i =>
                for (int i = 0; i < decoded.Count / NumberOfColorComponents; i++)
                {
                    double[] comps = new double[NumberOfColorComponents];
                    for (int n = 0; n < NumberOfColorComponents; n++)
                    {
                        comps[n] = decoded[i * NumberOfColorComponents + n] / 255.0;
                    }

                    var colors = Process(comps);
                    for (int c = 0; c < outputCount; c++)
                    {
                        transformed[i * outputCount + c] = ConvertToByte(colors[c]);
                    }
                }
                //);
                return transformed;
            }

            return AlternateColorSpaceDetails.Transform(decoded);
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
        internal override int BaseNumberOfColorComponents => NumberOfColorComponents;

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
        public override IColor GetInitializeColor()
        {
            throw new InvalidOperationException("UnsupportedColorSpaceDetails");
        }

        /// <inheritdoc/>
        internal override IReadOnlyList<byte> Transform(IReadOnlyList<byte> decoded)
        {
            throw new InvalidOperationException("UnsupportedColorSpaceDetails");
        }
    }
}
