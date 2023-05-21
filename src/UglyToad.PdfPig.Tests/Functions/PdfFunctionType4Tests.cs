namespace UglyToad.PdfPig.Tests.Functions
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using UglyToad.PdfPig.Functions;
    using UglyToad.PdfPig.Tests.Tokens;
    using UglyToad.PdfPig.Tokens;
    using UglyToad.PdfPig.Util;
    using Xunit;

    public class PdfFunctionType4Tests
    {
        private static PdfFunctionType4 CreateFunction(string function, double[] domain, double[] range)
        {
            DictionaryToken dictionaryToken = new DictionaryToken(new Dictionary<NameToken, IToken>()
            {
                { NameToken.FunctionType, new NumericToken(4) },
                { NameToken.Domain, new ArrayToken(domain.Select(v => new NumericToken((decimal)v)).ToArray()) },
                { NameToken.Range, new ArrayToken(range.Select(v => new NumericToken((decimal)v)).ToArray()) },
            });

            var data = Encoding.ASCII.GetBytes(function); // OtherEncodings.Iso88591.GetBytes(function);
            StreamToken stream = new StreamToken(dictionaryToken, data);

            var func = PdfFunctionParser.Create(stream, new TestPdfTokenScanner(), new TestFilterProvider());
            Assert.Equal(FunctionTypes.PostScript, func.FunctionType);

            return func as PdfFunctionType4;
        }

        /// <summary>
        /// Checks the <see cref="PdfFunctionType4"/>.
        /// </summary>
        [Fact]
        public void FunctionSimple()
        {
            const string functionText = "{ add }";
            //Simply adds the two arguments and returns the result

            PdfFunctionType4 function = CreateFunction(functionText,
                        new double[] { -1.0, 1.0, -1.0, 1.0 },
                        new double[] { -1.0, 1.0 });

            Assert.Equal(FunctionTypes.PostScript, function.FunctionType);

            double[] input = new double[] { 0.8, 0.1 };
            double[] output = function.Eval(input);

            Assert.Single(output);
            Assert.Equal(0.9, output[0], 4);

            input = new double[] { 0.8, 0.3 }; //results in 1.1f being outside Range
            output = function.Eval(input);

            Assert.Single(output);
            Assert.Equal(1, output[0]);

            input = new double[] { 0.8, 1.2 }; //input argument outside Dimension
            output = function.Eval(input);

            Assert.Single(output);
            Assert.Equal(1, output[0]);
        }

        /// <summary>
        /// Checks the handling of the argument order for a <see cref="PdfFunctionType4"/>.
        /// </summary>
        [Fact]
        public void FunctionArgumentOrder()
        {
            const string functionText = "{ pop }";
            // pops an argument (2nd) and returns the next argument (1st)

            PdfFunctionType4 function = CreateFunction(functionText,
                        new double[] { -1.0, 1.0, -1.0, 1.0 },
                        new double[] { -1.0, 1.0 });

            double[] input = new double[] { -0.7, 0.0 };
            double[] output = function.Eval(input);

            Assert.Single(output);
            Assert.Equal(-0.7, output[0], 4);
        }

        [Fact]
        public void Advanced()
        {
            const string functionText = "{ dup 0.0 mul 1 exch sub 2 index 1.0 mul 1 exch sub mul  1 exch sub 3 1 roll dup 0.75 mul 1 exch sub 2 index 0.723 mul 1 exch sub mul  1 exch sub 3 1 roll dup 0.9 mul 1 exch sub 2 index 0.0 mul 1 exch sub mul  1 exch sub 3 1 roll dup 0.0 mul 1 exch sub 2 index 0.02 mul 1 exch sub mul  1 exch sub 3 1 roll pop pop }";

            PdfFunctionType4 function = CreateFunction(functionText,
                        new double[] { 0, 1, 0, 1 },
                        new double[] { 0, 1, 0, 1, 0, 1, 0, 1 });

            double[] input = new double[] { 1.0, 1.0 };
            double[] output = function.Eval(input);

            Assert.Equal(4, output.Length);
        }
    }
}
