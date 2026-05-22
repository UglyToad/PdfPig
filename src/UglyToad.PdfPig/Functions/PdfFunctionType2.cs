namespace UglyToad.PdfPig.Functions
{
    using System;
    using System.Linq;
    using UglyToad.PdfPig.Tokens;

    /// <summary>
    /// Exponential interpolation function
    /// </summary>
    internal sealed class PdfFunctionType2 : PdfFunction
    {
        private readonly double[] c0Values;
        private readonly double[] c1Values;
        private readonly int componentCount;

        /// <summary>
        /// Exponential interpolation function
        /// </summary>
        internal PdfFunctionType2(DictionaryToken function, ArrayToken domain, ArrayToken? range, ArrayToken c0, ArrayToken c1, double n)
            : base(function, domain, range)
        {
            C0 = c0;
            C1 = c1;
            N = n;
            c0Values = c0.Data.OfType<NumericToken>().Select(t => t.Double).ToArray();
            c1Values = c1.Data.OfType<NumericToken>().Select(t => t.Double).ToArray();
            componentCount = Math.Min(c0Values.Length, c1Values.Length);
        }

        internal PdfFunctionType2(StreamToken function, ArrayToken domain, ArrayToken? range, ArrayToken c0, ArrayToken c1, double n)
            : base(function, domain, range)
        {
            C0 = c0;
            C1 = c1;
            N = n;
            c0Values = c0.Data.OfType<NumericToken>().Select(t => t.Double).ToArray();
            c1Values = c1.Data.OfType<NumericToken>().Select(t => t.Double).ToArray();
            componentCount = Math.Min(c0Values.Length, c1Values.Length);
        }

        public override FunctionTypes FunctionType => FunctionTypes.Exponential;

        protected internal override int MaxOutputComponentCount => componentCount;

        public override int Eval(ReadOnlySpan<double> input, Span<double> output)
        {
            // exponential interpolation
            double xToN = Math.Pow(input[0], N); // x^exponent

            int count = componentCount;
            double[] c0 = c0Values;
            double[] c1 = c1Values;
            for (int j = 0; j < count; j++)
            {
                output[j] = c0[j] + xToN * (c1[j] - c0[j]);
            }

            ClipToRange(output.Slice(0, count));
            return count;
        }

        /// <summary>
        /// The C0 values of the function, 0 if empty.
        /// </summary>
        public ArrayToken C0 { get; }

        /// <summary>
        /// The C1 values of the function, 1 if empty.
        /// </summary>
        public ArrayToken C1 { get; }

        /// <summary>
        /// The exponent of the function.
        /// </summary>
        public double N { get; }

        public override string ToString()
        {
            return "FunctionType2{"
                + "C0: " + C0 + " "
                + "C1: " + C1 + " "
                + "N: " + N + "}";
        }
    }
}
