namespace UglyToad.PdfPig.Tests.Functions
{
    using System.Collections.Generic;
    using System.Linq;
    using UglyToad.PdfPig.Functions;
    using UglyToad.PdfPig.Tests.Tokens;
    using UglyToad.PdfPig.Tokens;
    using UglyToad.PdfPig.Util;
    using Xunit;

    public class PdfFunctionType2Tests
    {
        private static PdfFunctionType2 CreateFunction(double[] domain, double[] range, double[] c0, double[] c1, double n)
        {
            DictionaryToken dictionaryToken = new DictionaryToken(new Dictionary<NameToken, IToken>()
            {
                { NameToken.FunctionType, new NumericToken(2) },
                { NameToken.Domain, new ArrayToken(domain.Select(v => new NumericToken((decimal)v)).ToArray()) },
                { NameToken.Range, new ArrayToken(range.Select(v => new NumericToken((decimal)v)).ToArray()) },

                { NameToken.C0, new ArrayToken(c0.Select(v => new NumericToken((decimal)v)).ToArray()) },
                { NameToken.C1, new ArrayToken(c1.Select(v => new NumericToken((decimal)v)).ToArray()) },
                { NameToken.N, new NumericToken((decimal)n) },
            });

            var func = PdfFunctionParser.Create(dictionaryToken, new TestPdfTokenScanner(), new TestFilterProvider());
            Assert.Equal(FunctionTypes.Exponential, func.FunctionType);
            return func as PdfFunctionType2;
        }

        [Fact]
        public void Simple()
        {
            PdfFunctionType2 function = CreateFunction(
                        new double[] { -1.0, 1.0, -1.0, 1.0 },
                        new double[] { -1.0, 1.0 },
                        new double[] { 0.0 },
                        new double[] { 1.0 },
                        1);

            Assert.Equal(FunctionTypes.Exponential, function.FunctionType);

            double[] input = new double[] { -0.7 };
            double[] output = function.Eval(input);
            Assert.Single(output);
            Assert.Equal(-0.7, output[0], 4);

            input = new double[] { 0.7 };
            output = function.Eval(input);
            Assert.Single(output);
            Assert.Equal(0.7, output[0], 4);

            input = new double[] { -0.5 };
            output = function.Eval(input);
            Assert.Single(output);
            Assert.Equal(-0.5, output[0], 4);

            input = new double[] { 0.5 };
            output = function.Eval(input);
            Assert.Single(output);
            Assert.Equal(0.5, output[0], 4);

            input = new double[] { 0 };
            output = function.Eval(input);
            Assert.Single(output);
            Assert.Equal(0, output[0], 4);

            input = new double[] { 1 };
            output = function.Eval(input);
            Assert.Single(output);
            Assert.Equal(1, output[0], 4);

            input = new double[] { -1 };
            output = function.Eval(input);
            Assert.Single(output);
            Assert.Equal(-1, output[0], 4);
        }

        [Fact]
        public void SimpleClip()
        {
            PdfFunctionType2 function = CreateFunction(
                        new double[] { -1.0, 1.0, -1.0, 1.0 },
                        new double[] { -1.0, 1.0 },
                        new double[] { 0.0 },
                        new double[] { 1.0 },
                        1);

            double[] input = new double[] { -15 };
            double[] output = function.Eval(input);
            Assert.Single(output);
            Assert.Equal(-1, output[0], 4);

            input = new double[] { 15 };
            output = function.Eval(input);
            Assert.Single(output);
            Assert.Equal(1, output[0], 4);
        }

        [Fact]
        public void N2()
        {
            PdfFunctionType2 function = CreateFunction(
                        new double[] { -1.0, 1.0, -1.0, 1.0 },
                        new double[] { -10.0, 10.0 },
                        new double[] { 0.0 },
                        new double[] { 1.0 },
                        2);

            double[] input = new double[] { 1.12 };
            double[] output = function.Eval(input);
            Assert.Single(output);
            Assert.Equal(1.2544, output[0], 4);

            input = new double[] { -1.35 };
            output = function.Eval(input);
            Assert.Single(output);
            Assert.Equal(1.82250, output[0], 4);

            input = new double[] { 5 };
            output = function.Eval(input);
            Assert.Single(output);
            Assert.Equal(10, output[0], 4); // clip
        }

        [Fact]
        public void N3()
        {
            PdfFunctionType2 function = CreateFunction(
                        new double[] { -1.0, 1.0, -1.0, 1.0 },
                        new double[] { -10.0, 10.0 },
                        new double[] { 4.0 },
                        new double[] { 9.53 },
                        3);

            double[] input = new double[] { 1.0 };
            double[] output = function.Eval(input);
            Assert.Single(output);
            Assert.Equal(9.53, output[0], 4);

            input = new double[] { -1.236 };
            output = function.Eval(input);
            Assert.Single(output);
            Assert.Equal(-6.44192, output[0], 4);
        }

        [Fact]
        public void NSqrt()
        {
            PdfFunctionType2 function = CreateFunction(
                        new double[] { -1.0, 1.0, -1.0, 1.0 },
                        new double[] { -10.0, 10.0 },
                        new double[] { 2.589 },
                        new double[] { 10.58 },
                        0.5);

            double[] input = new double[] { 0.5 };
            double[] output = function.Eval(input);
            Assert.Single(output);
            Assert.Equal(8.23949, output[0], 4);

            input = new double[] { 0.78 };
            output = function.Eval(input);
            Assert.Single(output);
            Assert.Equal(9.64646, output[0], 4);

            input = new double[] { -0.78 };
            output = function.Eval(input);
            Assert.Single(output);
            Assert.True(double.IsNaN(output[0])); // negative input with sqrt
        }
    }
}
