namespace UglyToad.PdfPig.Function
{
    using System;
    using System.Collections.Generic;
    using UglyToad.PdfPig.Tokenization.Scanner;
    using UglyToad.PdfPig.Tokens;

    /// <summary>
    /// PdfFunctionType0
    /// </summary>
    public class PdfFunctionType0 : PdfFunction
    {
        /// <summary>
        /// (Required) An array of m positive integers that shall specify the number of samples in each input dimension of the sample table.
        /// </summary>
        public ArrayToken Size { get; }

        /// <summary>
        /// (Required) The number of bits that shall represent each sample. (If the function has multiple output values, each one shall occupy BitsPerSample bits.)
        /// Valid values shall be 1, 2, 4, 8, 12, 16, 24, and 32.
        /// </summary>
        public int BitsPerSample { get; }

        /// <summary>
        /// (Optional) The order of interpolation between samples. Valid values shall be 1 and 3, specifying linear and cubic spline interpolation, respectively.
        /// Default value: 1.
        /// </summary>
        public int Order { get; }

        /// <summary>
        /// (Optional) An array of 2 × m numbers specifying the linear mapping of input values into the domain of the function’s sample table.
        /// Default value: [0 (Size0 − 1) 0 (Size1 − 1) …].
        /// </summary>
        public ArrayToken Encode { get; }

        /// <summary>
        /// (Optional) An array of 2 × n numbers specifying the linear mapping of sample values into the range appropriate for the function’s output values.
        /// Default value: same as the value of Range.
        /// </summary>
        public ArrayToken Decode { get; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public override int FunctionType => 0;

        /// <summary>
        /// PdfFunctionType0
        /// </summary>
        /// <param name="functionDictionary"></param>
        /// <param name="pdfTokenScanner"></param>
        public PdfFunctionType0(DictionaryToken functionDictionary, IPdfTokenScanner pdfTokenScanner)
            : base(functionDictionary, pdfTokenScanner)
        {
            if (functionDictionary.TryGet<ArrayToken>(NameToken.Size, pdfTokenScanner, out var size))
            {
                Size = size;
            }
            else
            {
                throw new ArgumentException("Size is Required.");
            }

            if (functionDictionary.TryGet<NumericToken>(NameToken.BitsPerSample, pdfTokenScanner, out var bitsPerSample))
            {
                BitsPerSample = bitsPerSample.Int;
            }
            else
            {
                throw new ArgumentException("BitsPerSample is Required.");
            }

            if (functionDictionary.TryGet<NumericToken>(NameToken.Order, pdfTokenScanner, out var order))
            {
                Order = order.Int;
            }
            else
            {
                // set default: 1.
                Order = 1;
            }

            if (functionDictionary.TryGet<ArrayToken>(NameToken.Encode, pdfTokenScanner, out var encode))
            {
                Encode = encode;
            }
            else
            {
                // set default: [0 (Size0 − 1) 0 (Size1 − 1) …].
                List<IToken> values = new List<IToken>();
                for (int i = 0; i < Size.Length; i++)
                {
                    values.Add(new NumericToken(0));
                    values.Add(new NumericToken(((NumericToken)Size[i]).Int - 1));
                }
                Encode = new ArrayToken(values);
            }

            if (functionDictionary.TryGet<ArrayToken>(NameToken.Decode, pdfTokenScanner, out var decode))
            {
                Decode = decode;
            }
            else
            {
                // set default
                Decode = Range;
            }
        }

        /// <summary>
        /// PdfFunctionType0
        /// </summary>
        /// <param name="functionStream"></param>
        /// <param name="pdfTokenScanner"></param>
        public PdfFunctionType0(StreamToken functionStream, IPdfTokenScanner pdfTokenScanner)
            : this(functionStream.StreamDictionary, pdfTokenScanner)
        { }

        /// <summary>
        /// Get the encode for the input parameter.
        /// </summary>
        /// <param name="paramNum">The function parameter number.</param>
        /// <returns>The encode parameter range or null if none is set.</returns>
        public PdfRange GetEncodeForParameter(int paramNum)
        {
            PdfRange retval = null;
            if (Encode != null && Encode.Length >= paramNum * 2 + 1)
            {
                retval = new PdfRange(Encode, paramNum);
            }
            return retval;
        }

        /// <inheritdoc/>
        public override float[] Eval(float[] input)
        {
            throw new NotImplementedException("Eval");
            /*
            //This involves linear interpolation based on a set of sample points.
            //Theoretically it's not that difficult ... see section 3.9.1 of the PDF Reference.

            float[] sizeValues = Size.Data.Select(x => (float)((NumericToken)x).Double).ToArray();
            int bitsPerSample = BitsPerSample;
            float maxSample = (float)(Math.Pow(2, bitsPerSample) - 1.0);
            int numberOfInputValues = input.Length;
            int numberOfOutputValues = NumberOfOutputValues; // getNumberOfOutputParameters();

            int[] inputPrev = new int[numberOfInputValues];
            int[] inputNext = new int[numberOfInputValues];
            input = input.ToArray(); //.clone(); // PDFBOX-4461

            for (int i = 0; i < numberOfInputValues; i++)
            {
                PdfRange domain = GetDomainForInput(i);
                PdfRange encodeValues = GetEncodeForParameter(i);
                input[i] = clipToRange(input[i], (float)domain.Min, (float)domain.Max);
                input[i] = interpolate(input[i], (float)domain.Min, (float)domain.Max, (float)encodeValues.Min, (float)encodeValues.Max);
                input[i] = clipToRange(input[i], 0, sizeValues[i] - 1);
                inputPrev[i] = (int)Math.Floor(input[i]);
                inputNext[i] = (int)Math.Ceiling(input[i]);
            }

            float[] outputValues = new Rinterpol(input, inputPrev, inputNext).rinterpolate();

            for (int i = 0; i < numberOfOutputValues; i++)
            {
                PdfRange range = GetRangeForOutput(i);
                PdfRange decodeValues = GetDecodeForParameter(i);
                outputValues[i] = interpolate(outputValues[i], 0, maxSample, (float)decodeValues.Min, (float)decodeValues.Max);
                outputValues[i] = clipToRange(outputValues[i], (float)range.Min, (float)range.Max);
            }

            return outputValues;
            */
        }
    }
}