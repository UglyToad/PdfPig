namespace UglyToad.PdfPig.Tests.Graphics.Colors
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using UglyToad.PdfPig.Functions;
    using UglyToad.PdfPig.Graphics.Colors;
    using UglyToad.PdfPig.Graphics.Colors.Icc;
    using UglyToad.PdfPig.Graphics.Core;
    using UglyToad.PdfPig.Logging;
    using UglyToad.PdfPig.Tokens;
    using Xunit;

    public class ICCBasedColorSpaceDetailsTests
    {
        private sealed class StubTransform : IIccTransform
        {
            private readonly (double r, double g, double b) fixedOut;

            public StubTransform(int components, (double r, double g, double b) fixedOut)
            {
                NumberOfComponents = components;
                this.fixedOut = fixedOut;
            }

            public int NumberOfComponents { get; }

            public (double r, double g, double b) ToRgb(ReadOnlySpan<double> values) => fixedOut;

            public void Transform(ReadOnlySpan<byte> src, Span<byte> dstRgb)
            {
                int pixels = src.Length / NumberOfComponents;
                for (int p = 0; p < pixels; p++)
                {
                    dstRgb[p * 3] = (byte)Math.Round(fixedOut.r * 255);
                    dstRgb[p * 3 + 1] = (byte)Math.Round(fixedOut.g * 255);
                    dstRgb[p * 3 + 2] = (byte)Math.Round(fixedOut.b * 255);
                }
            }
        }

        private sealed class StubProfile : IIccProfile
        {
            private readonly Dictionary<RenderingIntent, IIccTransform> transforms;

            /// <summary>
            /// The ICC.1 encoding range of an L*a*b* data colour space, which is the case the
            /// <see cref="IIccProfile.ComponentRanges"/> contract exists for.
            /// </summary>
            public static readonly double[] LabRanges = [0.0, 100.0, -128.0, 127.0, -128.0, 127.0];

            public StubProfile(int components, Dictionary<RenderingIntent, IIccTransform> transforms,
                bool isLabInput = false)
                : this(components, transforms, isLabInput ? LabRanges : UnitRanges(components))
            {
            }

            public StubProfile(int components, Dictionary<RenderingIntent, IIccTransform> transforms,
                IReadOnlyList<double> componentRanges)
            {
                NumberOfComponents = components;
                ComponentRanges = componentRanges;
                this.transforms = transforms;
            }

            private static double[] UnitRanges(int components)
                => Enumerable.Repeat(new[] { 0.0, 1.0 }, components).SelectMany(x => x).ToArray();

            public int NumberOfComponents { get; }

            public IReadOnlyList<double> ComponentRanges { get; }

            public bool TryGetTransform(RenderingIntent intent, [NotNullWhen(true)] out IIccTransform? transform)
            {
                if (transforms.TryGetValue(intent, out var t))
                {
                    transform = t;
                    return true;
                }
                transform = null;
                return false;
            }
        }

        [Fact]
        public void WithoutService_FallsBackToAlternateColorSpace()
        {
            var details = new ICCBasedColorSpaceDetails(
                numberOfColorComponents: 3,
                alternateColorSpaceDetails: DeviceRgbColorSpaceDetails.Instance,
                range: null,
                metadata: null,
                profile: null);

            Assert.Equal(ColorSpace.DeviceRGB, details.BaseType);
            Assert.Equal(3, details.BaseNumberOfColorComponents);
            Assert.Null(details.IccProfile);
            Assert.Null(details.GetTransformWithFallback(RenderingIntent.RelativeColorimetric));

            var (r, g, b) = details.GetColor([0.5, 0.5, 0.5]).ToRGBValues();
            Assert.Equal(0.5, r);
            Assert.Equal(0.5, g);
            Assert.Equal(0.5, b);
        }

        [Fact]
        public void WithService_BaseTypeStaysIccBasedAndComponentsIsThree()
        {
            var profile = new StubProfile(4, new Dictionary<RenderingIntent, IIccTransform>
            {
                [RenderingIntent.RelativeColorimetric] = new StubTransform(4, (0, 0, 0)),
            });

            var details = new ICCBasedColorSpaceDetails(
                numberOfColorComponents: 4,
                alternateColorSpaceDetails: DeviceCmykColorSpaceDetails.Instance,
                range: null,
                metadata: null,
                profile: profile);

            // The profile converts to sRGB, so the width is three - but the colours are now placed
            // absolutely, so BaseType does not hand them to anything that treats device colours as
            // reinterpretable.
            Assert.Equal(ColorSpace.ICCBased, details.BaseType);
            Assert.Equal(3, details.BaseNumberOfColorComponents);
            Assert.NotNull(details.IccProfile);
            Assert.NotNull(details.GetTransformWithFallback(RenderingIntent.RelativeColorimetric));
        }

        [Fact]
        public void WithService_GetColorWithoutIntent_UsesRelativeColorimetric()
        {
            var profile = new StubProfile(4, new Dictionary<RenderingIntent, IIccTransform>
            {
                [RenderingIntent.RelativeColorimetric] = new StubTransform(4, (0.25, 0.5, 0.75)),
                [RenderingIntent.Perceptual] = new StubTransform(4, (0.10, 0.10, 0.10)),
            });

            var details = new ICCBasedColorSpaceDetails(4, DeviceCmykColorSpaceDetails.Instance,
                null, null, profile);

            var (r, g, b) = details.GetColor([0.1, 0.2, 0.3, 0.4]).ToRGBValues();
            Assert.Equal(0.25, r);
            Assert.Equal(0.50, g);
            Assert.Equal(0.75, b);
        }

        [Fact]
        public void WithService_GetColorWithIntent_RoutesThroughThatIntent()
        {
            var profile = new StubProfile(4, new Dictionary<RenderingIntent, IIccTransform>
            {
                [RenderingIntent.RelativeColorimetric] = new StubTransform(4, (0.25, 0.5, 0.75)),
                [RenderingIntent.Perceptual] = new StubTransform(4, (0.10, 0.10, 0.10)),
            });

            var details = new ICCBasedColorSpaceDetails(4, DeviceCmykColorSpaceDetails.Instance,
                null, null, profile);

            var (r, g, b) = details.GetColor(new double[] { 0.1, 0.2, 0.3, 0.4 },
                RenderingIntent.Perceptual).ToRGBValues();

            Assert.Equal(0.10, r);
            Assert.Equal(0.10, g);
            Assert.Equal(0.10, b);
        }

        [Fact]
        public void GetInitializeColor_RoutesThroughTheGivenIntent()
        {
            // The colour a cs/CS operator installs is a colour like any other, and an ICC profile answers a
            // different transform per intent, so the intent in force when the colour space was set is what
            // decides it. PDFBox never has to make this choice - its getInitialColor hands back unconverted
            // components - but every other conversion entry point here is intent-aware, and this one was not.
            var profile = new StubProfile(4, new Dictionary<RenderingIntent, IIccTransform>
            {
                [RenderingIntent.RelativeColorimetric] = new StubTransform(4, (0.25, 0.5, 0.75)),
                [RenderingIntent.Perceptual] = new StubTransform(4, (0.10, 0.10, 0.10)),
            });

            var details = new ICCBasedColorSpaceDetails(4, DeviceCmykColorSpaceDetails.Instance,
                null, null, profile);

            var (pr, pg, pb) = details.GetInitializeColor(RenderingIntent.Perceptual)!.ToRGBValues();
            Assert.Equal(0.10, pr);
            Assert.Equal(0.10, pg);
            Assert.Equal(0.10, pb);

            // The parameterless overload still means RelativeColorimetric, the PDF default.
            var (dr, dg, db) = details.GetInitializeColor()!.ToRGBValues();
            Assert.Equal(0.25, dr);
            Assert.Equal(0.50, dg);
            Assert.Equal(0.75, db);
        }

        [Fact]
        public void GetInitializeColor_SubstitutesPerComponent_WithoutAProfile()
        {
            // 8.6.5.5: every component initializes to 0.0 "unless the range of valid values FOR A GIVEN
            // COMPONENT does not include 0.0". A single clip broadcast across the buffer would push the
            // first component's substitute into the other two, turning (0.2, 0, 0) into 20% grey.
            var details = new ICCBasedColorSpaceDetails(3, DeviceRgbColorSpaceDetails.Instance,
                [0.2, 1.0, 0.0, 1.0, 0.0, 1.0], null, null);

            var (r, g, b) = details.GetInitializeColor()!.ToRGBValues();

            Assert.Equal(0.2, r);
            Assert.Equal(0.0, g);
            Assert.Equal(0.0, b);
        }

        [Fact]
        public void GetInitializeColor_SubstitutesPerComponent_OnTheProfilePath()
        {
            // The same rule as seen by the transform, and in both directions: a component whose range
            // starts above 0.0 substitutes its minimum, one whose range ends below 0.0 substitutes its
            // maximum, and one straddling 0.0 keeps it.
            var (details, transform) = WithRange(3, DeviceRgbColorSpaceDetails.Instance,
                [0.2, 1.0, 0.0, 1.0, -1.0, -0.25]);

            details.GetInitializeColor(RenderingIntent.RelativeColorimetric);

            Assert.Equal(new double[] { 0.2, 0.0, -0.25 }, transform.LastValues);
        }

        [Fact]
        public void WithService_TransformIntentOverloadProducesIntentSpecificBuffer()
        {
            var profile = new StubProfile(4, new Dictionary<RenderingIntent, IIccTransform>
            {
                [RenderingIntent.RelativeColorimetric] = new StubTransform(4, (1.0, 0.0, 0.0)),
                [RenderingIntent.Saturation] = new StubTransform(4, (0.0, 1.0, 0.0)),
            });

            var details = new ICCBasedColorSpaceDetails(4, DeviceCmykColorSpaceDetails.Instance,
                null, null, profile);

            Span<byte> input = stackalloc byte[8] { 10, 20, 30, 40, 50, 60, 70, 80 };

            // Default intent (RelativeColorimetric) → red.
            var def = details.Transform(input, RenderingIntent.RelativeColorimetric);
            Assert.Equal(255, def[0]); Assert.Equal(0, def[1]); Assert.Equal(0, def[2]);

            // Explicit Saturation → green.
            var sat = details.Transform(input, RenderingIntent.Saturation);
            Assert.Equal(0, sat[0]); Assert.Equal(255, sat[1]); Assert.Equal(0, sat[2]);

            // Missing intent, default to RelativeColorimetric → red.
            var perc = details.Transform(input, RenderingIntent.Perceptual);
            Assert.Equal(255, perc[0]); Assert.Equal(0, perc[1]); Assert.Equal(0, perc[2]);
        }

        [Fact]
        public void IndexedWithIccBase_TransformWithIntent_RoutesThroughThatIntent()
        {
            // /Indexed [/ICCBased ...] palette image with 2 entries:
            //   index 0 -> CMYK (0.1, 0.2, 0.3, 0.4)
            //   index 1 -> CMYK (0.5, 0.6, 0.7, 0.8)
            // ICC profile stub maps any input to a fixed RGB per intent.
            var profile = new StubProfile(4, new Dictionary<RenderingIntent, IIccTransform>
            {
                [RenderingIntent.RelativeColorimetric] = new StubTransform(4, (1.0, 0.0, 0.0)), // red
                [RenderingIntent.Saturation] = new StubTransform(4, (0.0, 1.0, 0.0)), // green
            });

            var iccBase = new ICCBasedColorSpaceDetails(4, DeviceCmykColorSpaceDetails.Instance,
                null, null, profile);

            // 2-entry CMYK palette (8 bytes).
            byte[] colorTable =
            [
                (byte)(0.1 * 255), (byte)(0.2 * 255), (byte)(0.3 * 255), (byte)(0.4 * 255),
                (byte)(0.5 * 255), (byte)(0.6 * 255), (byte)(0.7 * 255), (byte)(0.8 * 255),
            ];
            var indexed = new IndexedColorSpaceDetails(iccBase, hiVal: 1, colorTable: colorTable);

            // Image bytes: 3 pixels of index 0, 1, 0.
            Span<byte> input = stackalloc byte[3] { 0, 1, 0 };

            // Default intent -> red.
            var def = indexed.Transform(input, RenderingIntent.RelativeColorimetric);
            Assert.Equal(9, def.Length); // 3 pixels * 3 bytes RGB
            Assert.Equal(255, def[0]); Assert.Equal(0, def[1]); Assert.Equal(0, def[2]);

            // Saturation intent -> green.
            var sat = indexed.Transform(input, RenderingIntent.Saturation);
            Assert.Equal(9, sat.Length);
            Assert.Equal(0, sat[0]); Assert.Equal(255, sat[1]); Assert.Equal(0, sat[2]);
        }

        [Fact]
        public void WithService_GetTransformFallsBackToRelativeColorimetricForUnsupportedIntent()
        {
            // Profile only supports RelativeColorimetric; ask for Perceptual.
            var profile = new StubProfile(3, new Dictionary<RenderingIntent, IIccTransform>
            {
                [RenderingIntent.RelativeColorimetric] = new StubTransform(3, (0.4, 0.5, 0.6)),
            });

            var details = new ICCBasedColorSpaceDetails(3, DeviceRgbColorSpaceDetails.Instance,
                null, null, profile);

            // The unsupported intent resolves to the relative colorimetric transform rather than to null,
            // which is what lets the conversion paths treat a non-null IccProfile as always convertible.
            var fallback = details.GetTransformWithFallback(RenderingIntent.Perceptual);
            Assert.NotNull(fallback);

            // GetColor with unsupported intent: falls back to RelativeColorimetric internally.
            var (r, g, b) = details.GetColor([0.1, 0.2, 0.3], RenderingIntent.Perceptual).ToRGBValues();
            Assert.Equal(0.4, r);
            Assert.Equal(0.5, g);
            Assert.Equal(0.6, b);
        }

        /// <summary>
        /// Captures the components handed to <see cref="IIccTransform.ToRgb"/> so the tests can assert
        /// what the profile path actually receives, rather than only what it returns.
        /// </summary>
        private sealed class RecordingTransform : IIccTransform
        {
            public double[]? LastValues { get; private set; }

            public RecordingTransform(int components) => NumberOfComponents = components;

            public int NumberOfComponents { get; }

            public (double r, double g, double b) ToRgb(ReadOnlySpan<double> values)
            {
                LastValues = values.ToArray();
                return (0, 0, 0);
            }

            public void Transform(ReadOnlySpan<byte> src, Span<byte> dstRgb) => dstRgb.Clear();
        }

        private static (ICCBasedColorSpaceDetails Details, RecordingTransform Transform) WithRange(
            int components, ColorSpaceDetails alternate, IReadOnlyList<double> range,
            bool isLabInput = false)
        {
            var transform = new RecordingTransform(components);
            var profile = new StubProfile(components, new Dictionary<RenderingIntent, IIccTransform>
            {
                [RenderingIntent.RelativeColorimetric] = transform
            }, isLabInput);

            var details = new ICCBasedColorSpaceDetails(components, alternate, range, null,
                profile);

            return (details, transform);
        }

        [Fact]
        public void ProfilePath_ClipsComponentsToRange()
        {
            // Range narrower than the operands: 8.6.5.5 makes these the valid bounds, and the profile
            // path used to hand the raw values straight to the transform.
            var (details, transform) = WithRange(3, DeviceRgbColorSpaceDetails.Instance,
                [0.0, 0.5, 0.0, 0.5, 0.0, 0.5]);

            details.GetColor([0.9, -0.4, 0.25], RenderingIntent.RelativeColorimetric);

            Assert.Equal(new double[] { 0.5, 0.0, 0.25 }, transform.LastValues);
        }

        [Fact]
        public void ProfilePath_GetRgbClipsComponentsToRange()
        {
            var (details, transform) = WithRange(3, DeviceRgbColorSpaceDetails.Instance,
                [0.0, 0.5, 0.0, 0.5, 0.0, 0.5]);

            details.GetRgb([0.9, -0.4, 0.25], RenderingIntent.RelativeColorimetric, out _, out _, out _);

            Assert.Equal(new double[] { 0.5, 0.0, 0.25 }, transform.LastValues);
        }

        [Fact]
        public void ProfilePath_ProcessClipsComponentsToRange()
        {
            var (details, transform) = WithRange(3, DeviceRgbColorSpaceDetails.Instance,
                [0.0, 0.5, 0.0, 0.5, 0.0, 0.5]);

            details.Process([0.9, -0.4, 0.25], RenderingIntent.RelativeColorimetric);

            Assert.Equal(new double[] { 0.5, 0.0, 0.25 }, transform.LastValues);
        }

        [Fact]
        public void ProfileAndFallbackPathsAgreeOnOutOfRangeComponents()
        {
            // The profile resolves or not must not change what an out-of-range operand means.
            // Both routes see the same clipped components, so an identity-like transform and
            // the alternate space produce the same colour.
            double[] range = [0.0, 0.5, 0.0, 0.5, 0.0, 0.5];
            double[] operands = [0.9, -0.4, 0.25];

            var (withProfile, transform) = WithRange(3, DeviceRgbColorSpaceDetails.Instance, range);
            withProfile.GetColor(operands, RenderingIntent.RelativeColorimetric);

            var withoutProfile = new ICCBasedColorSpaceDetails(3, DeviceRgbColorSpaceDetails.Instance,
                range, null, profile: null);
            var (r, g, b) = withoutProfile.GetColor(operands, RenderingIntent.RelativeColorimetric).ToRGBValues();

            Assert.Equal(new double[] { r, g, b }, transform.LastValues);
        }

        [Fact]
        public void ProfilePath_LeavesLabComponentsUnnormalised()
        {
            // Clipping must bound the components to the Lab domain without rescaling them: applying the
            // ICC.1 Lab encoding is the transform's job, since it is what actually decodes the profile.
            var (details, transform) = WithRange(3, DeviceRgbColorSpaceDetails.Instance,
                [0.0, 100.0, -128.0, 127.0, -128.0, 127.0], isLabInput: true);
            
            details.GetColor([55.0, -20.0, 300.0], RenderingIntent.RelativeColorimetric);

            Assert.Equal(new double[] { 55.0, -20.0, 127.0 }, transform.LastValues);
        }

        [Fact]
        public void ProfilePath_DefaultRangeIsUnchangedForInRangeComponents()
        {
            // The overwhelmingly common case: default [0 1] range, operands already valid. Clipping
            // must be a no-op here, which is why no rendered output moves.
            var (details, transform) = WithRange(4, DeviceCmykColorSpaceDetails.Instance, range: null!);

            double[] expected = [0.1, 0.2, 0.3, 0.4];
            details.GetColor(expected, RenderingIntent.RelativeColorimetric);

            Assert.Equal(expected, transform.LastValues);
        }

        [Fact]
        public void ProfilePath_LabProfileIgnoresAnAbsentRange()
        {
            // Regression guard for GHOSTSCRIPT-702013-1: an ICCBased stream of <</N 3/Length 1972>> whose
            // embedded profile is Lab, with the real Lab range declared only on the Separation tint
            // functions feeding it. Range therefore defaults to [0 1 ...], which is a lie about this space.
            // Clipping against that default would flatten L* = 100 to 1 and destroy every spot colour on
            // the page, so the profile header has to win.
            var (details, transform) = WithRange(3, DeviceRgbColorSpaceDetails.Instance, range: null!,
                isLabInput: true);

            details.GetColor([100.0, 0.0, 0.0], RenderingIntent.RelativeColorimetric);
            Assert.Equal(new double[] { 100.0, 0.0, 0.0 }, transform.LastValues);

            double[] expected = [40.7843, 55.0, 33.0];
            details.GetColor(expected, RenderingIntent.RelativeColorimetric);
            Assert.Equal(expected, transform.LastValues);
        }

        [Fact]
        public void ProfilePath_LabProfileIgnoresAnExplicitlyDeclaredDefaultRange()
        {
            var (details, transform) = WithRange(3, DeviceRgbColorSpaceDetails.Instance,
                [0.0, 1.0, 0.0, 1.0, 0.0, 1.0], isLabInput: true);

            double[] expected = [100.0, 55.0, 33.0];
            details.GetColor(expected, RenderingIntent.RelativeColorimetric);

            Assert.Equal(expected, transform.LastValues);
        }

        [Fact]
        public void ProfilePath_NonLabProfileStillHonoursTheDeclaredRange()
        {
            // The Lab override is scoped by what the profile reports, not applied to every profile: a
            // CMYK profile with a declared range keeps being clipped against that range.
            var (details, transform) = WithRange(4, DeviceCmykColorSpaceDetails.Instance,
                [0.0, 0.5, 0.0, 0.5, 0.0, 0.5, 0.0, 0.5]);

            details.GetColor([0.9, 0.1, -1.0, 0.5], RenderingIntent.RelativeColorimetric);

            Assert.Equal(new double[] { 0.5, 0.1, 0.0, 0.5 }, transform.LastValues);
        }

        [Fact]
        public void FallbackPath_StillClipsAgainstTheDefaultRange()
        {
            // The abstention above is scoped to the profile path only; the alternate space is a genuine
            // device space, and clipping into it against the default range is long-standing behaviour.
            var details = new ICCBasedColorSpaceDetails(3, DeviceRgbColorSpaceDetails.Instance,
                range: null, metadata: null, profile: null);

            var (r, g, b) = details.GetColor([100.0, -5.0, 0.5], RenderingIntent.RelativeColorimetric)
                .ToRGBValues();

            Assert.Equal(1.0, r);
            Assert.Equal(0.0, g);
            Assert.Equal(0.5, b);
        }

        /// <summary>
        /// Reads exactly <see cref="NumberOfComponents"/> entries, the way a real backend does, so that a
        /// short operand array is a hard error here rather than a silently truncated conversion.
        /// </summary>
        private sealed class StrictTransform : IIccTransform
        {
            public StrictTransform(int components) => NumberOfComponents = components;

            public int NumberOfComponents { get; }

            public (double r, double g, double b) ToRgb(ReadOnlySpan<double> values)
            {
                double last = values[NumberOfComponents - 1];
                return (last, last, last);
            }

            public void Transform(ReadOnlySpan<byte> src, Span<byte> dstRgb) => dstRgb.Clear();
        }

        [Fact]
        public void ProfileThatResolvesNoTransform_IsNotAdopted()
        {
            // IIccProfile.TryGetTransform is explicitly allowed to fail for every intent ("the caller may
            // ... fall back to the alternate color space"). BaseType and BaseNumberOfColorComponents are
            // fixed once at construction while the conversion path is chosen per call, so a profile that
            // can never produce a transform must not claim a 3-component DeviceRGB base it cannot deliver:
            // callers size their buffers from that property and fill them from Process.
            var profile = new StubProfile(4, new Dictionary<RenderingIntent, IIccTransform>());

            var details = new ICCBasedColorSpaceDetails(4, DeviceCmykColorSpaceDetails.Instance,
                null, null, profile);

            var processed = details.Process([0.1, 0.2, 0.3, 0.4], RenderingIntent.RelativeColorimetric);

            Assert.Equal(details.BaseNumberOfColorComponents, processed.Length);
        }

        [Fact]
        public void ProfilePath_NeverHandsTheTransformFewerComponentsThanItReads()
        {
            // A Separation/DeviceN tint function may emit fewer values than its alternate consumes, and
            // Process (unlike GetColor/GetRgb) is not reached through TintColorSpaceDetailsHelper's
            // pad/trim. A real transform reads NumberOfComponents entries, so a short operand array read
            // off the end of the caller's buffer.
            var profile = new StubProfile(4, new Dictionary<RenderingIntent, IIccTransform>
            {
                [RenderingIntent.RelativeColorimetric] = new StrictTransform(4)
            });

            var details = new ICCBasedColorSpaceDetails(4, DeviceCmykColorSpaceDetails.Instance,
                null, null, profile);

            var processed = details.Process([0.9, 0.1], RenderingIntent.RelativeColorimetric);

            Assert.Equal(details.BaseNumberOfColorComponents, processed.Length);
        }

        [Fact]
        public void ProfilePath_PadsAMalformedOperandCountToTheProfilesWidth()
        {
            // Zero-filling the missing slots is what EvalTint already does on the GetColor/GetRgb path,
            // so normalising here is what keeps a colour space rendering the same whichever route it is
            // reached through, and keeps Process's result BaseNumberOfColorComponents wide.
            var (details, transform) = WithRange(4, DeviceCmykColorSpaceDetails.Instance, range: null!);

            details.Process([0.9, 0.1], RenderingIntent.RelativeColorimetric);

            Assert.Equal(new double[] { 0.9, 0.1, 0.0, 0.0 }, transform.LastValues);
        }

        /// <summary>
        /// Throws from the conversion entry points, each after its own number of successful calls. An
        /// <see cref="IIccProfileService"/> is free to build its transforms lazily, so a handle that is handed
        /// out cleanly and then throws is a supported implementation, not a broken one.
        /// <para>
        /// Construction probes each entry point exactly once, so a grace of 1 is what lets a transform survive
        /// validation and fail on the first real conversion; a grace of 0 makes it fail during validation.
        /// </para>
        /// </summary>
        private sealed class ThrowingTransform : IIccTransform
        {
            private readonly int toRgbGrace;
            private readonly int transformGrace;

            public ThrowingTransform(int components, int toRgbGrace = 0, int transformGrace = 0)
            {
                NumberOfComponents = components;
                this.toRgbGrace = toRgbGrace;
                this.transformGrace = transformGrace;
            }

            public int NumberOfComponents { get; }

            public int ToRgbCalls { get; private set; }

            public int TransformCalls { get; private set; }

            public (double r, double g, double b) ToRgb(ReadOnlySpan<double> values)
            {
                if (ToRgbCalls++ < toRgbGrace)
                {
                    return (1.0, 1.0, 1.0);
                }

                throw new InvalidOperationException("Simulated CMM failure.");
            }

            public void Transform(ReadOnlySpan<byte> src, Span<byte> dstRgb)
            {
                if (TransformCalls++ < transformGrace)
                {
                    dstRgb.Fill(255);
                    return;
                }

                throw new InvalidOperationException("Simulated CMM failure.");
            }
        }

        /// <summary>
        /// A transform that survives validation and throws on the first conversion after it.
        /// </summary>
        private static ThrowingTransform ThrowsAfterValidation(int components)
            => new ThrowingTransform(components, toRgbGrace: 1, transformGrace: 1);

        private static ICCBasedColorSpaceDetails WithTransform(int components, ColorSpaceDetails alternate,
            IIccTransform transform)
        {
            var profile = new StubProfile(components, new Dictionary<RenderingIntent, IIccTransform>
            {
                [RenderingIntent.RelativeColorimetric] = transform
            });

            return new ICCBasedColorSpaceDetails(components, alternate, null, null,
                profile);
        }

        [Fact]
        public void ProfileThatThrowsOnConversion_IsRejectedAtConstruction()
        {
            // Obtaining an IIccTransform proves nothing about whether it converts. Validation therefore has
            // to run a conversion while construction can still choose the alternate, which is what PDFBox
            // does in loadICCProfile for PDFBOX-1295 / 1740 / 3610 / 4015 / 5563.
            var details = WithTransform(4, DeviceCmykColorSpaceDetails.Instance, new ThrowingTransform(4));

            Assert.Null(details.IccProfile);
            Assert.Equal(ColorSpace.DeviceCMYK, details.BaseType);
            Assert.Equal(4, details.BaseNumberOfColorComponents);
            Assert.Null(details.GetTransformWithFallback(RenderingIntent.RelativeColorimetric));
        }

        [Fact]
        public void ProfileThatThrowsOnlyOnTheByteTransform_IsRejectedAtConstruction()
        {
            // ToRgb and Transform are separate implementations and fail separately, so both are probed -
            // the analogue of PDFBox constructing a ComponentColorModel alongside its toRGB call.
            var transform = new ThrowingTransform(3, toRgbGrace: 1, transformGrace: 0);
            var details = WithTransform(3, DeviceRgbColorSpaceDetails.Instance, transform);

            Assert.Equal(1, transform.ToRgbCalls); // the scalar probe succeeded
            Assert.Null(details.IccProfile); // the byte probe did not
        }

        /// <summary>
        /// Records the buffer lengths it is handed, so the probe's operand width is assertable rather than
        /// merely survivable - every other fake here ignores its input and would pass any width at all.
        /// </summary>
        private sealed class WidthRecordingTransform : IIccTransform
        {
            public WidthRecordingTransform(int components) => NumberOfComponents = components;

            public int NumberOfComponents { get; }

            public int? ScalarWidth { get; private set; }

            public int? ByteWidth { get; private set; }

            public (double r, double g, double b) ToRgb(ReadOnlySpan<double> values)
            {
                ScalarWidth = values.Length;
                return (0, 0, 0);
            }

            public void Transform(ReadOnlySpan<byte> src, Span<byte> dstRgb)
            {
                ByteWidth = src.Length;
                dstRgb.Clear();
            }
        }

        [Fact]
        public void Validation_ProbesTheProfileAtTheReconciledComponentWidth()
        {
            // The probe stackallocs NumberOfColorComponents wide, so it has to run after /N has been
            // reconciled against the profile and after that count has been checked as one of 1, 3 or 4.
            // Run any earlier and it reads the get-only auto-property before assignment - zero - and hands
            // a real backend empty spans, discarding every profile in the document over a fault of ours.
            // /N says 3 here and the profile says 4; the profile wins, so 4 is what the probe allocates.
            var transform = new WidthRecordingTransform(4);
            var profile = new StubProfile(4, new Dictionary<RenderingIntent, IIccTransform>
            {
                [RenderingIntent.RelativeColorimetric] = transform
            });

            var details = new ICCBasedColorSpaceDetails(3, null, null, null, profile);

            Assert.Equal(4, details.NumberOfColorComponents);
            Assert.Equal(4, transform.ScalarWidth);
            Assert.Equal(4, transform.ByteWidth);
            Assert.NotNull(details.IccProfile);
        }

        [Fact]
        public void ProfileThatStartsThrowingAfterConstruction_FallsBackInsteadOfPropagating()
        {
            // Validation cannot prove a profile converts every input, only the one it probed. A later
            // failure must degrade to the alternate rather than escape through page processing.
            var transform = ThrowsAfterValidation(4);
            var details = WithTransform(4, DeviceCmykColorSpaceDetails.Instance, transform);

            Assert.NotNull(details.IccProfile);

            var color = details.GetColor([0.0, 0.0, 0.0, 1.0], RenderingIntent.RelativeColorimetric);

            // DeviceCMYK black through the alternate, not an InvalidOperationException.
            var (r, g, b) = color.ToRGBValues();
            Assert.Equal(0.0, r);
            Assert.Equal(0.0, g);
            Assert.Equal(0.0, b);
        }

        [Fact]
        public void AFailedConversionDoesNotRetireTheProfile()
        {
            // Construction already probes both entry points, so a profile that can never convert is gone
            // before any colour reaches it. What is left is a profile that converts and throws on some
            // particular input, and there the failure belongs to the input, not to the profile: retiring it
            // would hand the rest of the document to the alternate over one bad colour, and make the page
            // depend on which colour happened to be painted first.
            var transform = ThrowsAfterValidation(4);
            var details = WithTransform(4, DeviceCmykColorSpaceDetails.Instance, transform);

            details.GetColor([0.1, 0.2, 0.3, 0.4], RenderingIntent.RelativeColorimetric);
            int callsAfterFirstFailure = transform.ToRgbCalls;

            for (int i = 0; i < 5; i++)
            {
                details.GetColor([0.1, 0.2, 0.3, 0.4], RenderingIntent.RelativeColorimetric);
                details.GetRgb([0.1, 0.2, 0.3, 0.4], RenderingIntent.RelativeColorimetric, out _, out _, out _);
                details.Process([0.1, 0.2, 0.3, 0.4], RenderingIntent.RelativeColorimetric);
            }

            // Every one of the three entry points went back to the profile.
            Assert.Equal(callsAfterFirstFailure + 15, transform.ToRgbCalls);
            Assert.NotNull(details.GetTransformWithFallback(RenderingIntent.RelativeColorimetric));
        }

        /// <summary>
        /// Throws for one nominated first component and converts everything else, the way a backend that
        /// trips over a single out-of-gamut or degenerate sample does.
        /// </summary>
        private sealed class InputSensitiveTransform : IIccTransform
        {
            private readonly double throwOn;

            public InputSensitiveTransform(int components, double throwOn)
            {
                NumberOfComponents = components;
                this.throwOn = throwOn;
            }

            public int NumberOfComponents { get; }

            public (double r, double g, double b) ToRgb(ReadOnlySpan<double> values)
            {
                if (values[0] == throwOn)
                {
                    throw new InvalidOperationException("Simulated CMM failure for this input.");
                }

                return (1.0, 1.0, 1.0);
            }

            public void Transform(ReadOnlySpan<byte> src, Span<byte> dstRgb) => dstRgb.Fill(255);
        }

        [Fact]
        public void AColourThatFailsFallsBackAndTheNextColourIsStillColourManaged()
        {
            // The point of not latching, stated as output rather than call counts: one colour the profile
            // cannot convert must not turn the colours after it into alternate-space colours too.
            var details = WithTransform(3, DeviceRgbColorSpaceDetails.Instance,
                new InputSensitiveTransform(3, throwOn: 0.25));

            // The profile throws on this one, so DeviceRGB answers it.
            var (fr, fg, fb) = details.GetColor([0.25, 0.25, 0.25], RenderingIntent.RelativeColorimetric)
                .ToRGBValues();
            Assert.Equal(0.25, fr);
            Assert.Equal(0.25, fg);
            Assert.Equal(0.25, fb);

            // The next colour is one it can convert, and it is still asked.
            var (r, g, b) = details.GetColor([0.5, 0.5, 0.5], RenderingIntent.RelativeColorimetric)
                .ToRGBValues();
            Assert.Equal(1.0, r);
            Assert.Equal(1.0, g);
            Assert.Equal(1.0, b);
        }

        [Fact]
        public void AFailedByteTransform_FallsBackWithTheSourceIntact()
        {
            // IIccTransform.Transform takes a ReadOnlySpan, so a partially completed conversion cannot have
            // consumed the samples: the alternate must still see the original image bytes.
            var transform = ThrowsAfterValidation(1);
            var details = WithTransform(1, DeviceGrayColorSpaceDetails.Instance, transform);

            Assert.NotNull(details.IccProfile);

            Span<byte> input = stackalloc byte[3] { 10, 20, 30 };
            var result = details.Transform(input, RenderingIntent.RelativeColorimetric);

            // Three samples out as three RGB pixels, because a profile in use pins the base to DeviceRGB
            // whichever path produced the colour. Each carries its original grey level across all three
            // components, which is what says the alternate saw the samples unconsumed.
            Assert.Equal(9, result.Length);
            Assert.Equal(new byte[] { 10, 10, 10, 20, 20, 20, 30, 30, 30 }, result.ToArray());
        }

        [Fact]
        public void AFailedScalarConversion_DoesNotRetireTheByteTransform()
        {
            // ToRgb and Transform are separate implementations that fail separately - construction probes
            // them separately for exactly that reason - so a colour the scalar path could not convert says
            // nothing about the image path, which must still be tried.
            var transform = ThrowsAfterValidation(1);
            var details = WithTransform(1, DeviceGrayColorSpaceDetails.Instance, transform);

            details.GetColor([0.5], RenderingIntent.RelativeColorimetric);
            int transformCallsBefore = transform.TransformCalls;

            Span<byte> input = stackalloc byte[2] { 7, 9 };
            var result = details.Transform(input, RenderingIntent.RelativeColorimetric);

            Assert.Equal(transformCallsBefore + 1, transform.TransformCalls);

            // This one throws as well, so the samples come back through DeviceGray, as RGB.
            Assert.Equal(6, result.Length);
            Assert.Equal(new byte[] { 7, 7, 7, 9, 9, 9 }, result.ToArray());
        }

        /// <summary>
        /// A type 2 (exponential interpolation) tint function over domain [0 1] with exponent 1,
        /// i.e. f(t) = c0 + t x (c1 - c0). The number of outputs is the length of <paramref name="c0"/>.
        /// </summary>
        private static PdfFunction Tint(double[] c0, double[] c1)
        {
            var domain = new ArrayToken([new NumericToken(0), new NumericToken(1)]);
            var dictionary = new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                { NameToken.FunctionType, new NumericToken(2) }
            });

            return new PdfFunctionType2(dictionary, domain, null, Numbers(c0), Numbers(c1), 1);

            static ArrayToken Numbers(double[] values)
            {
                var tokens = new IToken[values.Length];
                for (int i = 0; i < values.Length; i++)
                {
                    tokens[i] = new NumericToken(values[i]);
                }

                return new ArrayToken(tokens);
            }
        }

        /// <summary>
        /// Enforces <see cref="IIccTransform.Transform"/>'s documented contract - <c>src.Length</c> is
        /// exactly <c>pixelCount x NumberOfComponents</c> for the <c>pixelCount</c> that
        /// <paramref name="dstRgb"/> has room for - the way a backend that indexes its input from its output
        /// position does. Converts every pixel to a distinctive colour so that its output is
        /// distinguishable from the alternate's.
        /// </summary>
        private sealed class PixelContractTransform : IIccTransform
        {
            public PixelContractTransform(int components) => NumberOfComponents = components;

            public int NumberOfComponents { get; }

            public (double r, double g, double b) ToRgb(ReadOnlySpan<double> values) => (0.0, 0.0, 0.0);

            public void Transform(ReadOnlySpan<byte> src, Span<byte> dstRgb)
            {
                int pixelCount = dstRgb.Length / 3;
                if (src.Length != pixelCount * NumberOfComponents)
                {
                    throw new ArgumentException(
                        $"Expected {pixelCount * NumberOfComponents} source bytes for {pixelCount} pixels, got {src.Length}.");
                }

                for (int p = 0; p < pixelCount; p++)
                {
                    dstRgb[p * 3] = 1;
                    dstRgb[p * 3 + 1] = 2;
                    dstRgb[p * 3 + 2] = 3;
                }
            }
        }

        [Fact]
        public void ATrailingPartialPixel_IsNotHandedToTheProfileTransform()
        {
            // pixelCount truncates, so a sample run that does not divide into whole pixels - which the
            // stride padding in ColorSpaceDetailsByteConverter.Convert can leave behind - used to be passed
            // whole against a destination sized for the pixels that fit. A transform holding to its
            // contract then throws, quietly demoting the entire image to the alternate.
            var details = WithTransform(4, DeviceCmykColorSpaceDetails.Instance, new PixelContractTransform(4));

            Assert.NotNull(details.IccProfile);

            // Two whole CMYK pixels and one byte over.
            Span<byte> input = stackalloc byte[9] { 0, 0, 0, 0, 0, 0, 0, 0, 7 };
            var result = details.Transform(input, RenderingIntent.RelativeColorimetric);

            // The packed path's colour, not the black a demotion to the per-colour path would produce.
            Assert.Equal(6, result.Length);
            Assert.Equal(new byte[] { 1, 2, 3, 1, 2, 3 }, result.ToArray());
        }

        [Fact]
        public void ProfileFallback_ProcessReturnsBaseNumberOfColorComponents()
        {
            // BaseNumberOfColorComponents is three whenever a profile is in use, and Process is contracted
            // to produce the base colour space's components. The alternate is reached for the colours the
            // profile cannot convert, but that is an implementation detail of how the colour was obtained -
            // it cannot change how many components the caller is handed.
            var transform = ThrowsAfterValidation(4);
            var details = WithTransform(4, DeviceCmykColorSpaceDetails.Instance, transform);

            Assert.NotNull(details.IccProfile);
            Assert.Equal(3, details.BaseNumberOfColorComponents);

            var processed = details.Process([0.0, 1.0, 1.0, 0.0], RenderingIntent.RelativeColorimetric);

            Assert.Equal(3, processed.Length);

            // CMYK (0, 1, 1, 0) through the alternate is red.
            Assert.Equal(1.0, processed[0], 12);
            Assert.Equal(0.0, processed[1], 12);
            Assert.Equal(0.0, processed[2], 12);
        }

        [Fact]
        public void ProfileFallback_TransformReturnsThreeBytesPerPixel()
        {
            // The image path has the same contract as Process: PngFromPdfImageFactory reads
            // BaseNumberOfColorComponents straight after Convert, so four bytes a pixel out of a fallback
            // is read as RGB and the image is garbled.
            var transform = ThrowsAfterValidation(4);
            var details = WithTransform(4, DeviceCmykColorSpaceDetails.Instance, transform);

            // Two CMYK pixels: red, then black.
            Span<byte> input = stackalloc byte[8] { 0, 255, 255, 0, 0, 0, 0, 255 };
            var result = details.Transform(input, RenderingIntent.RelativeColorimetric);

            Assert.Equal(6, result.Length);

            Assert.Equal(255, result[0]);
            Assert.Equal(0, result[1]);
            Assert.Equal(0, result[2]);

            Assert.Equal(0, result[3]);
            Assert.Equal(0, result[4]);
            Assert.Equal(0, result[5]);
        }

        [Fact]
        public void ProfileFallback_SeparationOverIccBased_DoesNotOverrunItsTransformBuffer()
        {
            // SeparationColorSpaceDetails.Transform sizes its output from BaseNumberOfColorComponents and
            // then writes however many components Process hands back, so a base whose two disagree walks
            // off the end of the buffer on the first image.
            var iccBased = WithTransform(4, DeviceCmykColorSpaceDetails.Instance, ThrowsAfterValidation(4));
            var separation = new SeparationColorSpaceDetails(NameToken.Create("Spot"), iccBased,
                Tint([0, 0, 0, 0], [0, 1, 1, 0]));

            Span<byte> input = stackalloc byte[2] { 0, 255 };
            var result = separation.Transform(input, RenderingIntent.RelativeColorimetric);

            Assert.Equal(6, result.Length);

            // Tint 0 is CMYK white, tint 1 is CMYK red.
            Assert.Equal(255, result[0]);
            Assert.Equal(255, result[1]);
            Assert.Equal(255, result[2]);

            Assert.Equal(255, result[3]);
            Assert.Equal(0, result[4]);
            Assert.Equal(0, result[5]);
        }

        [Fact]
        public void ProfileRanges_DriveTheDefaultDecodeArray()
        {
            // 8.9.5.10, Table 89: the profile's own encoding is what an image's samples decode into. Only
            // the profile knows an L*a*b* space runs L* to 100 - /Range is routinely left at [0 1].
            var (details, _) = WithRange(3, DeviceRgbColorSpaceDetails.Instance, range: null!,
                isLabInput: true);

            var decode = new double[6];
            details.GetDefaultDecode(8, decode);

            Assert.Equal(StubProfile.LabRanges, decode);
        }

        [Fact]
        public void UnitProfileRanges_LeaveTheDefaultDecodeAtUnit()
        {
            var (details, _) = WithRange(4, DeviceCmykColorSpaceDetails.Instance, range: null!);

            var decode = new double[8];
            details.GetDefaultDecode(8, decode);

            Assert.Equal(new[] { 0.0, 1.0, 0.0, 1.0, 0.0, 1.0, 0.0, 1.0 }, decode);
        }

        [Fact]
        public void WithoutAProfile_DefaultDecodeComesFromTheAlternate()
        {
            // Mirrors PDFBox's PDICCBased.getDefaultDecode, which delegates to the alternate whenever the
            // profile could not be loaded. Here the alternate is Lab, so its ranges must show through.
            var lab = new LabColorSpaceDetails([0.9505, 1.0, 1.089], null, [-90.0, 90.0, -80.0, 80.0]);
            var details = new ICCBasedColorSpaceDetails(3, lab, null, null,
                profile: null);

            var decode = new double[6];
            details.GetDefaultDecode(8, decode);

            Assert.Equal(new[] { 0.0, 100.0, -90.0, 90.0, -80.0, 80.0 }, decode);
        }

        [Fact]
        public void IndexedOverALabProfile_DecodesTableBytesIntoTheProfilesRanges()
        {
            // The colour-table counterpart of the image path: a palette entry over an L*a*b* profile has to
            // reach the transform as L* in [0, 100], not [0, 1]. Decoding it as a device space is what
            // renders such a palette near-black - the bug already fixed for a direct Lab base, which an
            // ICCBased base did not inherit because it did not override DecodeRawComponents.
            var (iccBase, transform) = WithRange(3, DeviceRgbColorSpaceDetails.Instance, range: null!,
                isLabInput: true);

            // One entry: L* = 0xFF, a* = b* = 0x80.
            var indexed = new IndexedColorSpaceDetails(iccBase, hiVal: 0, colorTable: [0xFF, 0x80, 0x80]);

            indexed.GetColor([0], RenderingIntent.RelativeColorimetric);

            Assert.NotNull(transform.LastValues);
            Assert.Equal(100.0, transform.LastValues![0], 6); // 255/255 * 100
            Assert.Equal(0.0, transform.LastValues[1], 1); // -128 + (128/255) * 255
            Assert.Equal(0.0, transform.LastValues[2], 1);
        }

        [Fact]
        public void IndexedOverAUnitProfile_StillDecodesTableBytesToUnit()
        {
            var (iccBase, transform) = WithRange(4, DeviceCmykColorSpaceDetails.Instance, range: null!);
            var indexed = new IndexedColorSpaceDetails(iccBase, hiVal: 0,
                colorTable: [0x00, 0xFF, 0x80, 0x40]);

            indexed.GetColor([0], RenderingIntent.RelativeColorimetric);

            Assert.NotNull(transform.LastValues);
            Assert.Equal(0.0, transform.LastValues![0], 6);
            Assert.Equal(1.0, transform.LastValues[1], 6);
            Assert.Equal(128 / 255.0, transform.LastValues[2], 6);
            Assert.Equal(64 / 255.0, transform.LastValues[3], 6);
        }

        [Fact]
        public void WithoutAProfile_BaseComponentCountComesFromTheAlternatesBase()
        {
            // The alternate is what Transform delegates to, so its base width is what comes back - not /N.
            // An Indexed alternate consumes one component and emits its base's four; a caller sizing a
            // buffer from BaseNumberOfColorComponents needs the four.
            var indexedAlternate = new IndexedColorSpaceDetails(DeviceCmykColorSpaceDetails.Instance,
                hiVal: 0, colorTable: [0x10, 0x20, 0x30, 0x40]);

            var details = new ICCBasedColorSpaceDetails(1, indexedAlternate, null, null,
                profile: null);

            Assert.Equal(1, details.NumberOfColorComponents);
            Assert.Equal(4, details.BaseNumberOfColorComponents);
            Assert.Equal(ColorSpace.DeviceCMYK, details.BaseType);

            // The property has to describe what Transform actually produces.
            Span<byte> oneSample = stackalloc byte[1] { 0 };
            var transformed = details.Transform(oneSample, RenderingIntent.RelativeColorimetric);
            Assert.Equal(details.BaseNumberOfColorComponents, transformed.Length);
        }

        /// <summary>
        /// Captures what the colour space reported while degrading, so the tests can assert that a silent
        /// fallback stopped being silent.
        /// </summary>
        private sealed class RecordingLog : ILog
        {
            public List<string> Warnings { get; } = new List<string>();

            public void Debug(string message) { }

            public void Debug(string message, Exception ex) { }

            public void Warn(string message) => Warnings.Add(message);

            public void Error(string message) => Warnings.Add(message);

            public void Error(string message, Exception ex) => Warnings.Add(message);
        }

        [Fact]
        public void ProfileDisagreeingWithN_CorrectsNRatherThanDroppingTheProfile()
        {
            // PDFBOX-4801: /N and the profile disagree, and the profile is the one that cannot be wrong
            // about itself. Discarding it left such a file rendering unmanaged here while PDFBox rendered it
            // colour-managed.
            var profile = new StubProfile(4, new Dictionary<RenderingIntent, IIccTransform>
            {
                [RenderingIntent.RelativeColorimetric] = new StubTransform(4, (0.25, 0.5, 0.75))
            });

            var log = new RecordingLog();

            // /N says 3; the profile says 4.
            var details = new ICCBasedColorSpaceDetails(3, null, null, null, profile, log);

            Assert.NotNull(details.IccProfile);
            Assert.Equal(4, details.NumberOfColorComponents);
            Assert.Same(DeviceCmykColorSpaceDetails.Instance, details.AlternateColorSpace);
            Assert.Contains(log.Warnings, w => w.Contains("4 components from the ICC profile"));

            // The corrected width is the one the colour space actually accepts.
            var (r, g, b) = details.GetColor([0.1, 0.2, 0.3, 0.4]).ToRGBValues();
            Assert.Equal(0.25, r);
            Assert.Equal(0.5, g);
            Assert.Equal(0.75, b);
        }

        [Fact]
        public void CorrectingN_DropsAnAlternateChosenAgainstTheDeclaredWidth()
        {
            // The alternate was picked while /N still said 3, so correcting to 4 leaves it unusable; the
            // device space implied by the corrected width takes over.
            var profile = new StubProfile(4, new Dictionary<RenderingIntent, IIccTransform>
            {
                [RenderingIntent.RelativeColorimetric] = new StubTransform(4, (0, 0, 0))
            });

            var details = new ICCBasedColorSpaceDetails(3, DeviceRgbColorSpaceDetails.Instance,
                null, null, profile);

            Assert.Equal(4, details.NumberOfColorComponents);
            Assert.Same(DeviceCmykColorSpaceDetails.Instance, details.AlternateColorSpace);
        }

        [Fact]
        public void CorrectingN_FallsBackToADefaultRangeSizedForTheCorrectedWidth()
        {
            // /Range was written for the declared /N, so a correction makes a mismatch expected rather than
            // exceptional. PDFBox's getRangeForComponent likewise falls back to 0..1 instead of refusing.
            var profile = new StubProfile(4, new Dictionary<RenderingIntent, IIccTransform>
            {
                [RenderingIntent.RelativeColorimetric] = new StubTransform(4, (0, 0, 0))
            });

            var details = new ICCBasedColorSpaceDetails(3, null, [0.0, 0.5, 0.0, 0.5, 0.0, 0.5], null, profile);

            Assert.Equal(4, details.NumberOfColorComponents);
            Assert.Equal(new[] { 0.0, 1.0, 0.0, 1.0, 0.0, 1.0, 0.0, 1.0 }, details.Range);
        }

        [Fact]
        public void AMismatchedRangeIsIgnoredRatherThanThrown()
        {
            // Refusing the colour space cost the whole page over an entry that has a usable default.
            var log = new RecordingLog();
            var details = new ICCBasedColorSpaceDetails(3, DeviceRgbColorSpaceDetails.Instance,
                [0.0, 0.5], null, null, log);

            Assert.Equal(new[] { 0.0, 1.0, 0.0, 1.0, 0.0, 1.0 }, details.Range);
            Assert.Contains(log.Warnings, w => w.Contains("/Range"));
        }

        [Fact]
        public void ProfileWithAComponentCountNoIccBasedSpaceMayHave_IsIgnored()
        {
            // 2 is not 1, 3 or 4, so the profile cannot be believed and /N stands.
            var profile = new StubProfile(2, new Dictionary<RenderingIntent, IIccTransform>
            {
                [RenderingIntent.RelativeColorimetric] = new StubTransform(2, (0, 0, 0))
            });

            var log = new RecordingLog();
            var details = new ICCBasedColorSpaceDetails(3, DeviceRgbColorSpaceDetails.Instance,
                null, null, profile, log);

            Assert.Null(details.IccProfile);
            Assert.Equal(3, details.NumberOfColorComponents);
            Assert.Same(DeviceRgbColorSpaceDetails.Instance, details.AlternateColorSpace);
            Assert.Contains(log.Warnings, w => w.Contains("2 components"));
        }

        [Fact]
        public void AnUnusableProfileIsReportedRatherThanDroppedSilently()
        {
            var log = new RecordingLog();
            var profile = new StubProfile(3, new Dictionary<RenderingIntent, IIccTransform>
            {
                [RenderingIntent.RelativeColorimetric] = new ThrowingTransform(3)
            });

            var details = new ICCBasedColorSpaceDetails(3, DeviceRgbColorSpaceDetails.Instance,
                null, null, profile, log);

            Assert.Null(details.IccProfile);
            Assert.Contains(log.Warnings, w => w.Contains("could not convert a colour"));
        }

        [Fact]
        public void AMismatchedAlternateIsReportedRatherThanDroppedSilently()
        {
            var log = new RecordingLog();
            var details = new ICCBasedColorSpaceDetails(4, DeviceRgbColorSpaceDetails.Instance,
                null, null, null, log);

            Assert.Same(DeviceCmykColorSpaceDetails.Instance, details.AlternateColorSpace);
            Assert.Contains(log.Warnings, w => w.Contains("/Alternate"));
        }

        [Fact]
        public void WithADeviceAlternate_BaseComponentCountIsUnchanged()
        {
            // The common case, where the alternate's base width and /N agree; guards the change above
            // against moving anything that was already right.
            Assert.Equal(1, new ICCBasedColorSpaceDetails(1, DeviceGrayColorSpaceDetails.Instance,
                null, null, null, null).BaseNumberOfColorComponents);
            Assert.Equal(3, new ICCBasedColorSpaceDetails(3, DeviceRgbColorSpaceDetails.Instance,
                null, null, null, null).BaseNumberOfColorComponents);
            Assert.Equal(4, new ICCBasedColorSpaceDetails(4, DeviceCmykColorSpaceDetails.Instance,
                null, null, null, null).BaseNumberOfColorComponents);
        }
    }
}
