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

            public StubProfile(int components, Dictionary<RenderingIntent, IIccTransform> transforms)
            {
                NumberOfComponents = components;
                this.transforms = transforms;
            }

            public int NumberOfComponents { get; }

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

            public bool TryGetProfile(Memory<byte> profileBytes, int numberOfColorComponents,
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
                profile: new byte[] { 1, 2, 3 },
                iccProfileService: null);

            Assert.Equal(ColorSpace.DeviceRGB, details.BaseType);
            Assert.Equal(3, details.BaseNumberOfColorComponents);
            Assert.Null(details.IccProfile);
            Assert.Null(details.GetTransform(RenderingIntent.RelativeColorimetric));

            var (r, g, b) = details.GetColor(0.5, 0.5, 0.5).ToRGBValues();
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
                profile: new byte[] { 0x01 },
                iccProfileService: new StubService(profile));

            Assert.Equal(ColorSpace.DeviceRGB, details.BaseType);
            Assert.Equal(3, details.BaseNumberOfColorComponents);
            Assert.NotNull(details.IccProfile);
            Assert.NotNull(details.GetTransform(RenderingIntent.RelativeColorimetric));
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

            var (r, g, b) = details.GetColor(0.1, 0.2, 0.3, 0.4).ToRGBValues();
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
            var def = details.Transform(input);
            Assert.Equal(255, def[0]); Assert.Equal(0, def[1]); Assert.Equal(0, def[2]);

            // Explicit Saturation → green.
            var sat = details.Transform(input, RenderingIntent.Saturation);
            Assert.Equal(0, sat[0]); Assert.Equal(255, sat[1]); Assert.Equal(0, sat[2]);
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
            var def = indexed.Transform(input.ToArray());
            Assert.Equal(9, def.Length); // 3 pixels * 3 bytes RGB
            Assert.Equal(255, def[0]); Assert.Equal(0, def[1]); Assert.Equal(0, def[2]);

            // Saturation intent -> green. THIS is the case my first refactor missed:
            // ColorSpaceDetailsByteConverter -> Indexed.Transform -> BaseColorSpace.Transform
            // used to drop the intent at the Indexed boundary.
            var sat = ((ColorSpaceDetails)indexed).Transform(input.ToArray(), RenderingIntent.Saturation);
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

            // Direct GetTransform: returns null for unsupported intent.
            Assert.Null(details.GetTransform(RenderingIntent.Perceptual));

            // GetColor with unsupported intent: falls back to RelativeColorimetric internally.
            var (r, g, b) = details.GetColor(new double[] { 0.1, 0.2, 0.3 },
                RenderingIntent.Perceptual).ToRGBValues();
            Assert.Equal(0.4, r);
            Assert.Equal(0.5, g);
            Assert.Equal(0.6, b);
        }
    }
}
