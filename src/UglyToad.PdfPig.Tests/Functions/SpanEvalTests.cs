namespace UglyToad.PdfPig.Tests.Functions
{
    using System;
    using UglyToad.PdfPig.Functions;
    using UglyToad.PdfPig.Graphics.Colors;
    using UglyToad.PdfPig.Tests.Tokens;
    using UglyToad.PdfPig.Tokens;
    using UglyToad.PdfPig.Util;

    public class SpanEvalTests
    {
        private static ArrayToken ArrayOf(params double[] values)
        {
            return new ArrayToken(values.Select(v => new NumericToken(v)).ToArray());
        }

        private static PdfFunctionType2 CreateType2(double[] domain, double[]? range, double[] c0, double[] c1, double n)
        {
            var dict = new Dictionary<NameToken, IToken>
            {
                { NameToken.FunctionType, new NumericToken(2) },
                { NameToken.Domain, ArrayOf(domain) },
                { NameToken.C0, ArrayOf(c0) },
                { NameToken.C1, ArrayOf(c1) },
                { NameToken.N, new NumericToken(n) },
            };
            if (range != null)
            {
                dict[NameToken.Range] = ArrayOf(range);
            }

            var func = PdfFunctionParser.Create(new DictionaryToken(dict), new TestPdfTokenScanner(), new TestFilterProvider());
            return (PdfFunctionType2)func;
        }

        private static PdfFunctionType4 CreateType4(string body, double[] domain, double[] range)
        {
            var dict = new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                { NameToken.FunctionType, new NumericToken(4) },
                { NameToken.Domain, ArrayOf(domain) },
                { NameToken.Range, ArrayOf(range) },
            });
            var stream = new StreamToken(dict, System.Text.Encoding.ASCII.GetBytes(body));
            return (PdfFunctionType4)PdfFunctionParser.Create(stream, new TestPdfTokenScanner(), new TestFilterProvider());
        }

        private static PdfFunctionType0 CreateType0RedBlueGradient()
        {
            // 2-sample 1-D table interpolating between red [1,0,0] and blue [0,0,1] in DeviceRGB.
            var dict = new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                { NameToken.FunctionType, new NumericToken(0) },
                { NameToken.Domain, ArrayOf(0, 1) },
                { NameToken.Range, ArrayOf(0, 1, 0, 1, 0, 1) },
                { NameToken.BitsPerSample, new NumericToken(8) },
                { NameToken.Size, ArrayOf(2) },
            });
            byte[] data = new byte[] { 255, 0, 0, 0, 0, 255 };
            return (PdfFunctionType0)PdfFunctionParser.Create(new StreamToken(dict, data), new TestPdfTokenScanner(), new TestFilterProvider());
        }

        // Type 2 — exponential interpolation

        [Fact]
        public void Type2_Span_WritesExpectedValuesAndReturnsCount()
        {
            // Linear interpolation from [0,0,0] to [1,1,1] at t = 0.5.
            var fn = CreateType2(
                domain: new[] { 0.0, 1.0 },
                range: new[] { 0.0, 1.0, 0.0, 1.0, 0.0, 1.0 },
                c0: new[] { 0.0, 0.0, 0.0 },
                c1: new[] { 1.0, 1.0, 1.0 },
                n: 1);

            Span<double> output = stackalloc double[3];
            int written = fn.Eval(stackalloc double[] { 0.5 }, output);

            Assert.Equal(3, written);
            Assert.Equal(0.5, output[0], 6);
            Assert.Equal(0.5, output[1], 6);
            Assert.Equal(0.5, output[2], 6);
        }

        [Fact]
        public void Type2_Span_OverSizedOutputBuffer_LeavesTrailingSlotsUntouched()
        {
            var fn = CreateType2(
                domain: new[] { 0.0, 1.0 },
                range: new[] { 0.0, 1.0, 0.0, 1.0, 0.0, 1.0 },
                c0: new[] { 0.0, 0.0, 0.0 },
                c1: new[] { 1.0, 1.0, 1.0 },
                n: 1);

            const double Sentinel = -999.0;
            Span<double> output = stackalloc double[6];
            output.Fill(Sentinel);

            int written = fn.Eval(stackalloc double[] { 0.25 }, output);

            Assert.Equal(3, written);
            // Trailing slots beyond [written] are untouched.
            Assert.Equal(Sentinel, output[3]);
            Assert.Equal(Sentinel, output[4]);
            Assert.Equal(Sentinel, output[5]);
        }

        [Fact]
        public void Type2_Span_AppliesRangeClamp()
        {
            // Range [0, 0.5] clamps the t=1 endpoint of c1 down to 0.5.
            var fn = CreateType2(
                domain: new[] { 0.0, 1.0 },
                range: new[] { 0.0, 0.5 },
                c0: new[] { 0.0 },
                c1: new[] { 1.0 },
                n: 1);

            Span<double> output = stackalloc double[1];
            int written = fn.Eval(stackalloc double[] { 1.0 }, output);
            Assert.Equal(1, written);
            Assert.Equal(0.5, output[0], 6); // Clamped to range max.
        }

        [Fact]
        public void Type2_Span_NoRange_ReportsComponentCount()
        {
            // No Range entry: NumberOfOutputParameters is 0, but Eval still writes one value per C0/C1 pair.
            var fn = CreateType2(
                domain: new[] { 0.0, 1.0 },
                range: null,
                c0: new[] { 0.0, 0.0 },
                c1: new[] { 1.0, 1.0 },
                n: 1);

            Assert.Equal(2, fn.MaxOutputComponentCount);

            Span<double> output = stackalloc double[2];
            int written = fn.Eval(stackalloc double[] { 0.5 }, output);
            Assert.Equal(2, written);
            Assert.Equal(0.5, output[0], 6);
            Assert.Equal(0.5, output[1], 6);
        }

        [Fact]
        public void Type2_ArrayOverload_MatchesSpanOverload()
        {
            var fn = CreateType2(
                domain: new[] { 0.0, 1.0 },
                range: new[] { 0.0, 1.0, 0.0, 1.0, 0.0, 1.0 },
                c0: new[] { 0.2, 0.4, 0.6 },
                c1: new[] { 0.9, 0.7, 0.5 },
                n: 2);

            double[] input = { 0.3 };
            double[] arrayResult = fn.Eval(input);

            Span<double> spanResult = stackalloc double[3];
            int written = fn.Eval(input, spanResult);

            Assert.Equal(arrayResult.Length, written);
            for (int i = 0; i < written; i++)
            {
                Assert.Equal(arrayResult[i], spanResult[i], 9);
            }
        }

        // Type 4 — PostScript calculator

        [Fact]
        public void Type4_Span_WritesExpectedValueAndReturnsCount()
        {
            var fn = CreateType4("{ add }",
                domain: new[] { -1.0, 1.0, -1.0, 1.0 },
                range: new[] { -1.0, 1.0 });

            Span<double> output = stackalloc double[1];
            int written = fn.Eval(stackalloc double[] { 0.4, 0.3 }, output);

            Assert.Equal(1, written);
            Assert.Equal(0.7, output[0], 6);
        }

        [Fact]
        public void Type4_Span_AppliesRangeClamp()
        {
            // 0.8 + 0.6 = 1.4, clamped to Range max of 1.0.
            var fn = CreateType4("{ add }",
                domain: new[] { -1.0, 1.0, -1.0, 1.0 },
                range: new[] { -1.0, 1.0 });

            Span<double> output = stackalloc double[1];
            int written = fn.Eval(stackalloc double[] { 0.8, 0.6 }, output);
            Assert.Equal(1, written);
            Assert.Equal(1.0, output[0], 6);
        }

        [Fact]
        public void Type4_Span_RepeatedInvocationsDoNotLeakStackState()
        {
            var fn = CreateType4("{ add }",
                domain: new[] { -1.0, 1.0, -1.0, 1.0 },
                range: new[] { -1.0, 1.0 });

            Span<double> output = stackalloc double[1];

            int first = fn.Eval(stackalloc double[] { 0.1, 0.2 }, output);
            Assert.Equal(1, first);
            Assert.Equal(0.3, output[0], 6);

            int second = fn.Eval(stackalloc double[] { 0.4, 0.1 }, output);
            Assert.Equal(1, second);
            Assert.Equal(0.5, output[0], 6);

            int third = fn.Eval(stackalloc double[] { -0.2, 0.7 }, output);
            Assert.Equal(1, third);
            Assert.Equal(0.5, output[0], 6);
        }

        [Fact]
        public void Type4_Span_OverSizedOutputBuffer_LeavesTrailingSlotsUntouched()
        {
            var fn = CreateType4("{ add }",
                domain: new[] { -1.0, 1.0, -1.0, 1.0 },
                range: new[] { -1.0, 1.0 });

            const double Sentinel = -999.0;
            Span<double> output = stackalloc double[4];
            output.Fill(Sentinel);

            int written = fn.Eval(stackalloc double[] { 0.1, 0.2 }, output);
            Assert.Equal(1, written);
            Assert.Equal(Sentinel, output[1]);
            Assert.Equal(Sentinel, output[2]);
            Assert.Equal(Sentinel, output[3]);
        }

        // Type 3 — stitching (built on top of Type 2 sub-functions)

        [Fact]
        public void Type3_Span_DispatchesToSelectedSubFunction()
        {
            // Stitch two Type 2 sub-functions over domain [0, 1]:
            //   t in [0, 0.5] -> sub0: c0 [0,0,0], c1 [1,0,0]  (red ramp)
            //   t in (0.5, 1] -> sub1: c0 [0,1,0], c1 [0,0,1]  (green-to-blue)
            var sub0 = CreateType2(new[] { 0.0, 1.0 }, new[] { 0.0, 1.0, 0.0, 1.0, 0.0, 1.0 },
                new[] { 0.0, 0.0, 0.0 }, new[] { 1.0, 0.0, 0.0 }, 1);
            var sub1 = CreateType2(new[] { 0.0, 1.0 }, new[] { 0.0, 1.0, 0.0, 1.0, 0.0, 1.0 },
                new[] { 0.0, 1.0, 0.0 }, new[] { 0.0, 0.0, 1.0 }, 1);

            var fn = new PdfFunctionType3(
                new DictionaryToken(new Dictionary<NameToken, IToken>()),
                domain: ArrayOf(0, 1),
                range: ArrayOf(0, 1, 0, 1, 0, 1),
                functionsArray: new PdfFunction[] { sub0, sub1 },
                bounds: ArrayOf(0.5),
                encode: ArrayOf(0, 1, 0, 1));

            // t = 0.25 -> sub0 at t=0.5 -> [0.5, 0, 0]
            Span<double> output = stackalloc double[3];
            int written = fn.Eval(stackalloc double[] { 0.25 }, output);
            Assert.Equal(3, written);
            Assert.Equal(0.5, output[0], 6);
            Assert.Equal(0.0, output[1], 6);
            Assert.Equal(0.0, output[2], 6);

            // t = 0.75 -> sub1 at t=0.5 -> [0, 0.5, 0.5]
            written = fn.Eval(stackalloc double[] { 0.75 }, output);
            Assert.Equal(3, written);
            Assert.Equal(0.0, output[0], 6);
            Assert.Equal(0.5, output[1], 6);
            Assert.Equal(0.5, output[2], 6);
        }

        [Fact]
        public void Type3_MaxOutputComponentCount_AggregatesOverSubFunctions()
        {
            // sub0 returns 3 components; declared Range is also 3. Type3 should report 3.
            var sub0 = CreateType2(new[] { 0.0, 1.0 }, new[] { 0.0, 1.0, 0.0, 1.0, 0.0, 1.0 },
                new[] { 0.0, 0.0, 0.0 }, new[] { 1.0, 1.0, 1.0 }, 1);

            var fn = new PdfFunctionType3(
                new DictionaryToken(new Dictionary<NameToken, IToken>()),
                domain: ArrayOf(0, 1),
                range: ArrayOf(0, 1, 0, 1, 0, 1),
                functionsArray: new PdfFunction[] { sub0 },
                bounds: ArrayOf(),
                encode: ArrayOf(0, 1));

            Assert.Equal(3, fn.MaxOutputComponentCount);
        }

        // Type 0 — sampled table

        [Fact]
        public void Type0_Span_WritesExpectedValuesAndReturnsCount()
        {
            var fn = CreateType0RedBlueGradient();

            Span<double> output = stackalloc double[3];
            int written = fn.Eval(stackalloc double[] { 0 }, output);
            Assert.Equal(3, written);
            Assert.Equal(1.0, output[0], 6);
            Assert.Equal(0.0, output[1], 6);
            Assert.Equal(0.0, output[2], 6);

            written = fn.Eval(stackalloc double[] { 1 }, output);
            Assert.Equal(3, written);
            Assert.Equal(0.0, output[0], 6);
            Assert.Equal(0.0, output[1], 6);
            Assert.Equal(1.0, output[2], 6);

            written = fn.Eval(stackalloc double[] { 0.5 }, output);
            Assert.Equal(3, written);
            Assert.Equal(0.5, output[0], 6);
            Assert.Equal(0.0, output[1], 6);
            Assert.Equal(0.5, output[2], 6);
        }

        [Fact]
        public void Type0_ArrayOverload_MatchesSpanOverload()
        {
            var fn = CreateType0RedBlueGradient();
            double[] input = { 0.3 };
            double[] arrayResult = fn.Eval(input);

            Span<double> spanResult = stackalloc double[3];
            int written = fn.Eval(input, spanResult);
            Assert.Equal(arrayResult.Length, written);
            for (int i = 0; i < written; i++)
            {
                Assert.Equal(arrayResult[i], spanResult[i], 9);
            }
        }

        // Shading.Eval(span, span)

        [Fact]
        public void Shading_Span_SingleFunction_ClampsToZeroOne()
        {
            // Range allows -0.5..1.5 so clamping is observable.
            var fn = CreateType2(
                domain: new[] { 0.0, 1.0 },
                range: new[] { -0.5, 1.5 },
                c0: new[] { -0.5 },
                c1: new[] { 1.5 },
                n: 1);

            var shading = new AxialShading(
                antiAlias: false,
                shadingDictionary: new DictionaryToken(new Dictionary<NameToken, IToken>()),
                colorSpace: DeviceGrayColorSpaceDetails.Instance,
                bbox: null,
                background: null,
                coords: new[] { 0.0, 0.0, 1.0, 0.0 },
                domain: new[] { 0.0, 1.0 },
                functions: new PdfFunction[] { fn },
                extend: new[] { false, false });

            Span<double> output = stackalloc double[1];

            int writtenLow = shading.Eval(stackalloc double[] { 0.0 }, output);
            Assert.Equal(1, writtenLow);
            Assert.Equal(0.0, output[0], 6); // Function returns -0.5 -> Shading clamps to 0.

            int writtenHigh = shading.Eval(stackalloc double[] { 1.0 }, output);
            Assert.Equal(1, writtenHigh);
            Assert.Equal(1.0, output[0], 6); // Function returns 1.5 -> Shading clamps to 1.

            int writtenMid = shading.Eval(stackalloc double[] { 0.5 }, output);
            Assert.Equal(1, writtenMid);
            Assert.Equal(0.5, output[0], 6); // Mid-range value passes through unclamped.
        }

        [Fact]
        public void Shading_Span_MultiFunctionFanOut_PicksFirstValueOfEach()
        {
            // Three 1-out functions returning constants 0.1, 0.7, 0.4. AxialShading multi-function fan-out
            // should pull the first output of each into the buffer and clamp.
            var fn0 = CreateType2(new[] { 0.0, 1.0 }, new[] { 0.0, 1.0 }, new[] { 0.1 }, new[] { 0.1 }, 1);
            var fn1 = CreateType2(new[] { 0.0, 1.0 }, new[] { 0.0, 1.0 }, new[] { 0.7 }, new[] { 0.7 }, 1);
            var fn2 = CreateType2(new[] { 0.0, 1.0 }, new[] { 0.0, 1.0 }, new[] { 0.4 }, new[] { 0.4 }, 1);

            var shading = new AxialShading(
                antiAlias: false,
                shadingDictionary: new DictionaryToken(new Dictionary<NameToken, IToken>()),
                colorSpace: DeviceRgbColorSpaceDetails.Instance,
                bbox: null,
                background: null,
                coords: new[] { 0.0, 0.0, 1.0, 0.0 },
                domain: new[] { 0.0, 1.0 },
                functions: new PdfFunction[] { fn0, fn1, fn2 },
                extend: new[] { false, false });

            Span<double> output = stackalloc double[3];
            int written = shading.Eval(stackalloc double[] { 0.5 }, output);
            Assert.Equal(3, written);
            Assert.Equal(0.1, output[0], 6);
            Assert.Equal(0.7, output[1], 6);
            Assert.Equal(0.4, output[2], 6);
        }

        [Fact]
        public void Shading_Span_OverSizedOutputBuffer_LeavesTrailingSlotsUntouched()
        {
            var fn = CreateType2(
                domain: new[] { 0.0, 1.0 },
                range: new[] { 0.0, 1.0 },
                c0: new[] { 0.25 },
                c1: new[] { 0.25 },
                n: 1);

            var shading = new AxialShading(
                antiAlias: false,
                shadingDictionary: new DictionaryToken(new Dictionary<NameToken, IToken>()),
                colorSpace: DeviceGrayColorSpaceDetails.Instance,
                bbox: null,
                background: null,
                coords: new[] { 0.0, 0.0, 1.0, 0.0 },
                domain: new[] { 0.0, 1.0 },
                functions: new PdfFunction[] { fn },
                extend: new[] { false, false });

            const double Sentinel = -999.0;
            Span<double> output = stackalloc double[4];
            output.Fill(Sentinel);

            int written = shading.Eval(stackalloc double[] { 0.5 }, output);
            Assert.Equal(1, written);
            Assert.Equal(0.25, output[0], 6);
            Assert.Equal(Sentinel, output[1]);
            Assert.Equal(Sentinel, output[2]);
            Assert.Equal(Sentinel, output[3]);
        }
    }
}
