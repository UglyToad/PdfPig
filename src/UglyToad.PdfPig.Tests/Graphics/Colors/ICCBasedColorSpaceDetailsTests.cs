namespace UglyToad.PdfPig.Tests.Graphics.Colors
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using UglyToad.PdfPig.Graphics.Colors;
    using UglyToad.PdfPig.Graphics.Colors.Icc;
    using UglyToad.PdfPig.Graphics.Core;
    using UglyToad.PdfPig.Logging;
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

        private sealed class StubService : IIccProfileService
        {
            private readonly IIccProfile? profile;

            public StubService(IIccProfile? profile) { this.profile = profile; }

            public bool TryGetProfile(ReadOnlyMemory<byte> profileBytes,
                [NotNullWhen(true)] out IIccProfile? profile)
            {
                profile = this.profile;
                return profile is not null;
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
        public void WithService_BaseTypeIsDeviceRgbAndComponentsIsThree()
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

            Assert.Equal(ColorSpace.DeviceRGB, details.BaseType);
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

        /// <param name="isLabInput">
        /// What the resolved profile reports for <see cref="IIccProfile.IsLabInput"/>. The colour space takes
        /// this from the profile rather than inspecting the ICC header itself, so the stub is where the
        /// distinction is made; deriving it from the real header is the backend's job
        /// (<c>UnicolourIccProfile</c>).
        /// </param>
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
            // Process -- unlike GetColor/GetRgb -- is not reached through TintColorSpaceDetailsHelper's
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
        /// out cleanly and then throws is a supported implementation, not a broken one - PDFBox documents five
        /// separate profiles that behave exactly this way under the platform CMM.
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

        /// <summary>A transform that survives validation and throws on the first conversion after it.</summary>
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
        public void AFailedConversionLatchesSoTheProfileIsNotRetried()
        {
            // Retrying a profile that has thrown costs one exception per colour for the rest of the
            // document, and cannot start working. One failure retires it.
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

            Assert.Equal(callsAfterFirstFailure, transform.ToRgbCalls);
            Assert.Null(details.GetTransformWithFallback(RenderingIntent.RelativeColorimetric));
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

            // DeviceGray passes its samples through unchanged.
            Assert.Equal(3, result.Length);
            Assert.Equal(10, result[0]);
            Assert.Equal(20, result[1]);
            Assert.Equal(30, result[2]);
        }

        [Fact]
        public void AFailedScalarConversion_AlsoRetiresTheByteTransform()
        {
            // The latch is per colour space, not per entry point: whatever made one conversion throw is a
            // property of the profile, so the image path must not go on to hit it too.
            var transform = ThrowsAfterValidation(1);
            var details = WithTransform(1, DeviceGrayColorSpaceDetails.Instance, transform);

            details.GetColor([0.5], RenderingIntent.RelativeColorimetric);
            int transformCallsBefore = transform.TransformCalls;

            Span<byte> input = stackalloc byte[2] { 7, 9 };
            details.Transform(input, RenderingIntent.RelativeColorimetric);

            Assert.Equal(transformCallsBefore, transform.TransformCalls);
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
