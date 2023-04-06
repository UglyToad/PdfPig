namespace UglyToad.PdfPig.Graphics.Colors
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
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
        /// Get the color that initialize the current stroking or nonstroking colour.
        /// </summary>
        public abstract IColor GetInitializeColor();

        /// <summary>
        /// Convert to byte.
        /// </summary>
        protected static byte ConvertToByte(decimal componentValue)
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

        private DeviceGrayColorSpaceDetails() : base(ColorSpace.DeviceGray)
        { }

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

        private DeviceRgbColorSpaceDetails() : base(ColorSpace.DeviceRGB)
        { }

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

        private DeviceCmykColorSpaceDetails() : base(ColorSpace.DeviceCMYK)
        {
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
    }

    /// <summary>
    /// An Indexed color space allows a PDF content stream to use small integers as indices into a color map or color table of arbitrary colors in some other space.
    /// A PDF consumer treats each sample value as an index into the color table and uses the color value it finds there.
    /// </summary>
    public class IndexedColorSpaceDetails : ColorSpaceDetails
    {
        private readonly ConcurrentDictionary<double, IColor> cache = new ConcurrentDictionary<double, IColor>();

        /// <summary>
        /// Creates a indexed color space useful for exracting stencil masks as black-and-white images,
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
            BaseType = baseColorSpaceDetails.BaseType;
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
                    transformer = x => new[] { ColorTable[x] };
                    multiplier = 1;
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
    }

    /// <summary>
    /// A Separation color space provides a means for specifying the use of additional colorants or
    /// for isolating the control of individual color components of a device color space for a subtractive device.
    /// When such a space is the current color space, the current color is a single-component value, called a tint,
    /// that controls the application of the given colorant or color components only.
    /// </summary>
    public class SeparationColorSpaceDetails : ColorSpaceDetails
    {
        private readonly ConcurrentDictionary<double, IColor> cache = new ConcurrentDictionary<double, IColor>();

        /// <inheritdoc/>
        public override int NumberOfColorComponents => 1;

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

        internal IReadOnlyList<byte> TransformToRGB(IReadOnlyList<byte> values)
        {
            var transformed = new List<byte>();
            for (var i = 0; i < values.Count; i += 3)
            {
                var (r, g, b) = GetColor(values[i++] / 255.0).ToRGBValues();
                transformed.Add(ConvertToByte(r));
                transformed.Add(ConvertToByte(g));
                transformed.Add(ConvertToByte(b));
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
    public class CalGrayColorSpaceDetails : ColorSpaceDetails
    {
        /// <inheritdoc/>
        public override int NumberOfColorComponents => 1;

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
            var colorRgb = colorSpaceTransformer.TransformToRGB((colorA, colorA, colorA));
            return new RGBColor((decimal)colorRgb.R, (decimal)colorRgb.G, (decimal)colorRgb.B);
        }

        internal IReadOnlyList<byte> TransformToRGB(IReadOnlyList<byte> decoded)
        {
            var transformed = new List<byte>();
            for (var i = 0; i < decoded.Count; i++)
            {
                var component = decoded[i] / 255.0;
                var rgbPixel = TransformToRGB(component);
                // We only need one component here 
                transformed.Add(ConvertToByte(rgbPixel.R));
            }

            return transformed;
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
    public class CalRGBColorSpaceDetails : ColorSpaceDetails
    {
        /// <inheritdoc/>
        public override int NumberOfColorComponents => 3;

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
            var colorRgb = colorSpaceTransformer.TransformToRGB((colorAbc.A, colorAbc.B, colorAbc.C));
            return new RGBColor((decimal)colorRgb.R, (decimal)colorRgb.G, (decimal)colorRgb.B);
        }

        internal IReadOnlyList<byte> TransformToRGB(IReadOnlyList<byte> decoded)
        {
            var transformed = new List<byte>();
            for (var i = 0; i < decoded.Count; i += 3)
            {
                var rgbPixel = TransformToRGB((decoded[i] / 255.0, decoded[i + 1] / 255.0, decoded[i + 2] / 255.0));
                transformed.Add(ConvertToByte(rgbPixel.R));
                transformed.Add(ConvertToByte(rgbPixel.G));
                transformed.Add(ConvertToByte(rgbPixel.B));
            }

            return transformed;
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
    /// The ICCBased color space is one of the CIE-based color spaces supported in PDFs. These color spaces
    /// enable a page description to specify color values in a way that is related to human visual perception.
    /// The goal is for the same color specification to produce consistent results on different output devices,
    /// within the limitations of each device.
    ///
    /// Currently support for this color space is limited in PdfPig. Calculations will only be based on
    /// the color space of <see cref="AlternateColorSpaceDetails"/>.
    /// </summary>
    public class ICCBasedColorSpaceDetails : ColorSpaceDetails
    {
        /// <summary>
        /// The number of color components in the color space described by the ICC profile data.
        /// This numbers shall match the number of components actually in the ICC profile.
        /// Valid values are 1, 3 and 4.
        /// </summary>
        public override int NumberOfColorComponents { get; }

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
        /// Create a new <see cref="ICCBasedColorSpaceDetails"/>.
        /// </summary>
        internal ICCBasedColorSpaceDetails(int numberOfColorComponents, [CanBeNull] ColorSpaceDetails alternateColorSpaceDetails,
            [CanBeNull] IReadOnlyList<decimal> range, [CanBeNull] XmpMetadata metadata)
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
        }

        /// <inheritdoc/>
        public override IColor GetColor(params double[] values)
        {
            if (values == null || values.Length != NumberOfColorComponents)
            {
                throw new ArgumentException($"Invalid number of imputs, expecting {NumberOfColorComponents} but got {values.Length}", nameof(values));
            }

            // TODO - use ICC profile

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
    }

    /// <summary>
    /// A ColorSpace which the PdfPig library does not currently support. Please raise a PR if you need support for this ColorSpace.
    /// </summary>
    public class UnsupportedColorSpaceDetails : ColorSpaceDetails
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

        //private readonly IColor debugColor = new RGBColor(255m / 255m, 20m / 255m, 147m / 255m);

        private UnsupportedColorSpaceDetails() : base(ColorSpace.DeviceGray)
        {
        }

        /// <inheritdoc/>
        public override IColor GetColor(params double[] values)
        {
            //return debugColor;
            throw new InvalidOperationException("UnsupportedColorSpaceDetails");
        }

        /// <inheritdoc/>
        public override IColor GetInitializeColor()
        {
            //return debugColor;
            throw new InvalidOperationException("UnsupportedColorSpaceDetails");
        }
    }
}
