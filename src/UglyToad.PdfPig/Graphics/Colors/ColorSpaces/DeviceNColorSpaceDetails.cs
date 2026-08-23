namespace UglyToad.PdfPig.Graphics.Colors
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using UglyToad.PdfPig.Functions;
    using UglyToad.PdfPig.Tokens;

    /// <summary>
    /// DeviceN colour spaces may contain an arbitrary number of colour components. They provide greater flexibility than
    /// is possible with standard device colour spaces such as DeviceCMYK or with individual Separation colour spaces.
    /// </summary>
    public sealed class DeviceNColorSpaceDetails : ColorSpaceDetails
    {
        private readonly ConcurrentDictionary<double[], IColor> cache = new(DoubleArrayEqualityComparer.Instance);

#if NET9_0_OR_GREATER
        private readonly ConcurrentDictionary<double[], IColor>.AlternateLookup<ReadOnlySpan<double>> lookup;
#endif

        /// <summary>
        /// <inheritdoc/>
        /// <para>The 'N' in DeviceN.</para>
        /// </summary>
        public override int NumberOfColorComponents { get; }

        /// <inheritdoc/>
        public override int BaseNumberOfColorComponents => AlternateColorSpace.BaseNumberOfColorComponents;

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
            BaseType = AlternateColorSpace.BaseType;

#if NET9_0_OR_GREATER
            lookup = cache.GetAlternateLookup<ReadOnlySpan<double>>();
#endif
        }

        /// <inheritdoc/>
        internal override double[] Process(params double[] values)
        {
            var evaled = TintFunction.Eval(values);
            return AlternateColorSpace.Process(evaled);
        }

        /// <inheritdoc/>
        public override IColor GetColor(ReadOnlySpan<double> values)
        {
            if (values.Length != NumberOfColorComponents)
            {
                throw new ArgumentException($"Invalid number of inputs, expecting {NumberOfColorComponents} but got {values.Length}", nameof(values));
            }

            // TODO - use attributes

#if NET9_0_OR_GREATER
            if (lookup.TryGetValue(values, out var color))
            {
                return color;
            }

            color = TintColorSpaceDetailsHelper.GetColorViaTint(TintFunction, AlternateColorSpace, values);

            lookup.TryAdd(values, color);
#else
            double[] key = values.ToArray();

            if (cache.TryGetValue(key, out var color))
            {
                return color;
            }

            color = TintColorSpaceDetailsHelper.GetColorViaTint(TintFunction, AlternateColorSpace, values);

            cache.TryAdd(key, color);
#endif
            return color;
        }

        /// <inheritdoc/>
        internal override Span<byte> Transform(Span<byte> decoded)
        {
            // This is cached locally
            var transformCache = new Dictionary<int, double[]>();
            var transformed = new byte[decoded.Length * BaseNumberOfColorComponents / NumberOfColorComponents];
            int k = 0;

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

                if (!transformCache.TryGetValue(key, out double[]? colors))
                {
                    colors = Process(comps);
                    transformCache[key] = colors;
                }

                for (int c = 0; c < colors.Length; c++)
                {
                    transformed[k++] = ConvertToByte(colors[c]);
                }
            }

            return transformed;
        }

        /// <inheritdoc/>
        public override IColor GetInitializeColor()
        {
            // When this space is set to the current colour space (using the CS or cs operators), each component
            // shall be given an initial value of 1.0. The SCN and scn operators respectively shall set the current
            // stroking and nonstroking colour.
            Span<double> buffer = NumberOfColorComponents <= 32 ? stackalloc double[NumberOfColorComponents] : new double[NumberOfColorComponents];
            buffer.Fill(1.0);
            return GetColor(buffer);
        }

        /// <inheritdoc/>
        public override void GetRgb(ReadOnlySpan<double> values, out double r, out double g, out double b)
        {
            TintColorSpaceDetailsHelper.GetRgbViaTint(TintFunction, AlternateColorSpace, values, out r, out g, out b);
        }

        private sealed class DoubleArrayEqualityComparer : IEqualityComparer<double[]>
#if NET9_0_OR_GREATER
        , IAlternateEqualityComparer<ReadOnlySpan<double>, double[]>
#endif
        {
            public static readonly DoubleArrayEqualityComparer Instance = new DoubleArrayEqualityComparer();

            public bool Equals(double[]? x, double[]? y)
            {
                if (ReferenceEquals(x, y))
                {
                    return true;
                }

                if (x is null || y is null)
                {
                    return false;
                }

                return x.AsSpan().SequenceEqual(y);
            }

            public int GetHashCode(double[] obj)
            {
                var hash = new HashCode();
                foreach (var value in obj)
                {
                    hash.Add(value);
                }

                return hash.ToHashCode();
            }

#if NET9_0_OR_GREATER
            public bool Equals(ReadOnlySpan<double> alternate, double[] other)
            {
                return alternate.SequenceEqual(other);
            }

            public int GetHashCode(ReadOnlySpan<double> alternate)
            {
                var hash = new HashCode();
                foreach (var value in alternate)
                {
                    hash.Add(value);
                }

                return hash.ToHashCode();
            }

            public double[] Create(ReadOnlySpan<double> alternate)
            {
                return alternate.ToArray();
            }
#endif
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
}
