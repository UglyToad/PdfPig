namespace UglyToad.PdfPig.Tests.Graphics.Colors
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using UglyToad.PdfPig.Graphics.Colors;
    using UglyToad.PdfPig.Graphics.Colors.Icc;
    using UglyToad.PdfPig.Graphics.Core;
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

            public StubProfile(int components, Dictionary<RenderingIntent, IIccTransform> transforms,
                bool isLabInput = false)
            {
                NumberOfComponents = components;
                IsLabInput = isLabInput;
                this.transforms = transforms;
            }

            public int NumberOfComponents { get; }

            public bool IsLabInput { get; }

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
                profileData: new byte[] { 1, 2, 3 },
                iccService: null);

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
                profileData: new byte[] { 0x01 },
                iccService: new StubService(profile));

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
                null, null, new byte[] { 0xAB }, new StubService(profile));

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
                null, null, new byte[] { 0xAB }, new StubService(profile));

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
                null, null, new byte[] { 0xCD }, new StubService(profile));

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
                null, null, new byte[] { 0x01 }, new StubService(profile));

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
                null, null, new byte[] { 0x99 }, new StubService(profile));

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
                new byte[] { 0x01 }, new StubService(profile));

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
                range, null, new byte[] { 0x01 }, iccService: null);
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
                range: null, metadata: null, profileData: new byte[] { 0x01 }, iccService: null);

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
                null, null, new byte[] { 0x01 }, new StubService(profile));

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
                null, null, new byte[] { 0x01 }, new StubService(profile));

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
    }
}
