namespace UglyToad.PdfPig.Graphics.Colors
{
    using Core;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using Functions;
    using Tokens;

    /// <summary>
    /// DeviceN colour spaces may contain an arbitrary number of colour components. They provide greater flexibility than
    /// is possible with standard device colour spaces such as DeviceCMYK or with individual Separation colour spaces.
    /// </summary>
    public sealed class DeviceNColorSpaceDetails : ColorSpaceDetails
    {
        private readonly ConcurrentDictionary<TintKey, IColor> cache = new(TintKeyEqualityComparer.Instance);

#if NET9_0_OR_GREATER
        private readonly ConcurrentDictionary<TintKey, IColor>.AlternateLookup<TintKeyRef> lookup;
#endif

        /// <summary>
        /// <inheritdoc/>
        /// <para>The 'N' in DeviceN.</para>
        /// </summary>
        public override int NumberOfColorComponents { get; }

        /// <inheritdoc/>
        public override int BaseNumberOfColorComponents => AlternateColorSpace.BaseNumberOfColorComponents;

        /// <summary>
        /// <inheritdoc/>
        /// <para>
        /// The tint transform ignores the intent and only <see cref="AlternateColorSpace"/> can consume it.
        /// </para>
        /// </summary>
        public override bool RenderingIntentAffectsOutput => AlternateColorSpace.RenderingIntentAffectsOutput;

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

#if NET9_0_OR_GREATER
            lookup = cache.GetAlternateLookup<TintKeyRef>();
#endif
        }

        /// <inheritdoc/>
        internal override double[] Process(double[] values, RenderingIntent intent)
        {
            var evaled = TintFunction.Eval(values);
            return AlternateColorSpace.Process(evaled, intent);
        }

        /// <inheritdoc/>
        public override IColor GetColor(ReadOnlySpan<double> values, RenderingIntent intent)
        {
            if (values.Length != NumberOfColorComponents)
            {
                throw new ArgumentException($"Invalid number of inputs, expecting {NumberOfColorComponents} but got {values.Length}", nameof(values));
            }

            // TODO - use attributes

#if NET9_0_OR_GREATER
            var key = new TintKeyRef(values, intent);
            if (lookup.TryGetValue(key, out var color))
            {
                return color;
            }

            color = TintColorSpaceDetailsHelper.GetColorViaTint(TintFunction, AlternateColorSpace, values, intent);

            lookup.TryAdd(key, color);
#else
            var key = new TintKey(values.ToArray(), intent);
            if (cache.TryGetValue(key, out var color))
            {
                return color;
            }

            color = TintColorSpaceDetailsHelper.GetColorViaTint(TintFunction, AlternateColorSpace, values, intent);

            cache.TryAdd(key, color);
#endif
            return color;
        }

        /// <inheritdoc/>
        internal override Span<byte> Transform(Span<byte> decoded, RenderingIntent intent)
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
                    colors = Process(comps, intent);
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
        public override IColor GetInitializeColor(RenderingIntent intent)
        {
            // When this space is set to the current colour space (using the CS or cs operators), each component
            // shall be given an initial value of 1.0. The SCN and scn operators respectively shall set the current
            // stroking and nonstroking colour.
            Span<double> buffer = NumberOfColorComponents <= 32 ? stackalloc double[NumberOfColorComponents] : new double[NumberOfColorComponents];
            buffer.Fill(1.0);
            return GetColor(buffer, intent);
        }

        /// <inheritdoc/>
        public override void GetRgb(ReadOnlySpan<double> values, RenderingIntent intent,
            out double r, out double g, out double b)
        {
            TintColorSpaceDetailsHelper.GetRgbViaTint(TintFunction, AlternateColorSpace, values, intent, out r, out g, out b);
        }

        private readonly struct TintKey
        {
            public TintKey(double[] values, RenderingIntent intent)
            {
                Values = values;
                Intent = intent;
            }

            public readonly double[] Values;

            public readonly RenderingIntent Intent;
        }

#if NET9_0_OR_GREATER
        private readonly ref struct TintKeyRef
        {
            public TintKeyRef(ReadOnlySpan<double> values, RenderingIntent intent)
            {
                Values = values;
                Intent = intent;
            }

            public readonly ReadOnlySpan<double> Values;

            public readonly RenderingIntent Intent;
        }
#endif

        private sealed class TintKeyEqualityComparer : IEqualityComparer<TintKey>
#if NET9_0_OR_GREATER
        , IAlternateEqualityComparer<TintKeyRef, TintKey>
#endif
        {
            public static readonly TintKeyEqualityComparer Instance = new TintKeyEqualityComparer();

            public bool Equals(TintKey x, TintKey y)
            {
                if (x.Intent != y.Intent)
                {
                    return false;
                }

                if (ReferenceEquals(x.Values, y.Values))
                {
                    return true;
                }

                if (x.Values is null || y.Values is null)
                {
                    return false;
                }

                return x.Values.AsSpan().SequenceEqual(y.Values);
            }

            public int GetHashCode(TintKey obj)
            {
                var hash = new HashCode();
                hash.Add(obj.Intent);
                foreach (var value in obj.Values)
                {
                    hash.Add(value);
                }

                return hash.ToHashCode();
            }

#if NET9_0_OR_GREATER
            public bool Equals(TintKeyRef alternate, TintKey other)
            {
                return alternate.Intent == other.Intent &&
                       alternate.Values.SequenceEqual(other.Values);
            }

            public int GetHashCode(TintKeyRef alternate)
            {
                var hash = new HashCode();
                hash.Add(alternate.Intent);
                foreach (var value in alternate.Values)
                {
                    hash.Add(value);
                }

                return hash.ToHashCode();
            }

            public TintKey Create(TintKeyRef alternate)
            {
                return new TintKey(alternate.Values.ToArray(), alternate.Intent);
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
