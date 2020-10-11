namespace UglyToad.PdfPig.Function
{
    using System;
    using UglyToad.PdfPig.Tokenization.Scanner;
    using UglyToad.PdfPig.Tokens;

    /// <summary>
    /// This class represents a Type 2 (exponential interpolation) function in a PDF document.
    /// </summary>
    public class PdfFunctionType2 : PdfFunction
    {
        /// <summary>
        /// (Optional) An array of n numbers that shall define the function result when x = 0.0.
        /// Default value: [0.0].
        /// </summary>
        public ArrayToken C0 { get; }

        /// <summary>
        /// (Optional) An array of n numbers that shall define the function result when x = 1.0.
        /// Default value: [1.0].
        /// </summary>
        public ArrayToken C1 { get; }

        /// <summary>
        /// (Required) The interpolation exponent.
        /// </summary>
        public double N { get; }

        /// <inheritdoc/>
        public override int FunctionType => 2;

        /// <inheritdoc/>
        public PdfFunctionType2(DictionaryToken functionDictionary, IPdfTokenScanner pdfTokenScanner)
            : base(functionDictionary, pdfTokenScanner)
        {
            if (functionDictionary.TryGet<ArrayToken>(NameToken.C0, pdfTokenScanner, out var c0))
            {
                C0 = c0;
            }
            else
            {
                // set default: [0.0]
                C0 = new ArrayToken(new[] { new NumericToken(0) });
            }

            if (functionDictionary.TryGet<ArrayToken>(NameToken.C1, pdfTokenScanner, out var c1))
            {
                C1 = c1;
            }
            else
            {
                // set default: [1.0]
                C1 = new ArrayToken(new[] { new NumericToken(1) });
            }

            if (functionDictionary.TryGet<NumericToken>(NameToken.N, pdfTokenScanner, out var n))
            {
                N = n.Double;
            }
            else
            {
                throw new ArgumentException("N is Required.");
            }
        }

        /// <inheritdoc/>
        public override float[] Eval(float[] input)
        {
            throw new NotImplementedException();
        }
    }
}
