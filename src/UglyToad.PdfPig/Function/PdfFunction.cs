namespace UglyToad.PdfPig.Function
{
    using System;
    using System.Linq;
    using UglyToad.PdfPig.Tokenization.Scanner;
    using UglyToad.PdfPig.Tokens;

    /// <summary>
    /// PdfFunction
    /// </summary>
    public abstract class PdfFunction
    {
        /// <summary>
        /// The function dictionary.
        /// </summary>
        public DictionaryToken FunctionDictionary { get; }

        /// <summary>
        /// (Required) An array of 2 × m numbers, where m shall be the number of input values. For each i from 0 to m − 1,
        /// Domain2i shall be less than or equal to Domain2i+1, and the ith input value, xi, shall lie in the interval
        /// Domain2i ≤ xi ≤ Domain2i+1. Input values outside the declared domain shall be clipped to the nearest boundary value.
        /// </summary>
        public ArrayToken Domain { get; }

        /// <summary>
        /// (Required for type 0 and type 4 functions, optional otherwise; see below)
        /// An array of 2 × n numbers, where n shall be the number of output values.
        /// For each j from 0 to n − 1, Range2j shall be less than or equal to Range2j+1,
        /// and the jth output value, yj , shall lie in the interval Range2j ≤ yj ≤ Range2j+1.
        /// Output values outside the declared range shall be clipped to the nearest boundary value.
        /// If this entry is absent, no clipping shall be done.
        /// </summary>
        public ArrayToken Range { get; }

        private int numberOfInputValues;
        /// <summary>
        /// NumberOfInputValues
        /// </summary>
        public int NumberOfInputValues
        {
            get
            {
                return numberOfInputValues;
            }
        }

        private int numberOfOutputValues;
        /// <summary>
        /// This will get the number of output parameters that
        /// have a range specified. A range for output parameters
        /// is optional so this may return zero for a function
        /// that does have output parameters, this will simply return the
        /// number that have the range specified.
        /// </summary>
        public int NumberOfOutputValues
        {
            get
            {
                if (numberOfOutputValues == -1)
                {
                    numberOfOutputValues = Range.Length / 2;
                }

                return numberOfOutputValues;
            }
        }

        /// <summary>
        /// This will get the range for a certain input parameter.  This is will never
        /// return null.  If it is not present then the range 0 to 0 will
        /// be returned.
        /// </summary>
        /// <param name="n">The parameter number to get the domain for.</param>
        /// <returns>The domain range for this component.</returns>
        public PdfRange GetDomainForInput(int n)
        {
            return new PdfRange(Domain, n);
        }

        /// <summary>
        /// (Required) The function type:
        /// <para>0 Sampled function</para>
        /// <para>2 Exponential interpolation function</para>
        /// <para>3 Stitching function</para>
        /// <para>4 PostScript calculator function</para>
        /// </summary>
        public abstract int FunctionType { get; }

        /// <summary>
        /// PdfFunction
        /// </summary>
        /// <param name="functionDictionary"></param>
        /// <param name="pdfTokenScanner"></param>
        public PdfFunction(DictionaryToken functionDictionary, IPdfTokenScanner pdfTokenScanner)
        {
            numberOfOutputValues = -1;
            numberOfInputValues = -1;

            if (functionDictionary != null)
            {
                FunctionDictionary = functionDictionary;
                if (functionDictionary.TryGet<ArrayToken>(NameToken.Domain, pdfTokenScanner, out var domain))
                {
                    Domain = domain;
                }
                else
                {
                    throw new ArgumentException("Domain is Required.");
                }

                if (functionDictionary.TryGet<ArrayToken>(NameToken.Range, pdfTokenScanner, out var range))
                {
                    Range = range;
                }
            }
        }

        /// <summary>
        /// Clip the given input values to the ranges.
        /// </summary>
        /// <param name="inputValues">the input values</param>
        /// <returns>the clipped values</returns>
        protected float[] clipToRange(float[] inputValues)
        {
            ArrayToken rangesArray = Range;
            float[] result;
            if (rangesArray?.Length > 0)
            {
                float[] rangeValues = rangesArray.Data.Select(x => (float)((NumericToken)x).Double).ToArray();
                int numberOfRanges = rangeValues.Length / 2;
                result = new float[numberOfRanges];
                for (int i = 0; i < numberOfRanges; i++)
                {
                    int index = i << 1;
                    result[i] = clipToRange(inputValues[i], rangeValues[index], rangeValues[index + 1]);
                }
            }
            else
            {
                result = inputValues;
            }
            return result;
        }

        /// <summary>
        /// Clip the given input value to the given range.
        /// </summary>
        /// <param name="x">x the input value</param>
        /// <param name="rangeMin">rangeMin the min value of the range</param>
        /// <param name="rangeMax">rangeMax the max value of the range</param>
        /// <returns>the clipped value</returns>
        protected float clipToRange(float x, float rangeMin, float rangeMax)
        {
            if (x < rangeMin)
            {
                return rangeMin;
            }
            else if (x > rangeMax)
            {
                return rangeMax;
            }
            return x;
        }

        /// <summary>
        /// For a given value of x, interpolate calculates the y value
        /// on the line defined by the two points (xRangeMin, xRangeMax)
        /// and (yRangeMin, yRangeMax).
        /// </summary>
        /// <param name="x">the value to be interpolated value.</param>
        /// <param name="xRangeMin">the min value of the x range</param>
        /// <param name="xRangeMax">the max value of the x range</param>
        /// <param name="yRangeMin">the min value of the y range</param>
        /// <param name="yRangeMax">the max value of the y range</param>
        /// <returns>the interpolated y value</returns>
        protected float interpolate(float x, float xRangeMin, float xRangeMax, float yRangeMin, float yRangeMax)
        {
            return yRangeMin + ((x - xRangeMin) * (yRangeMax - yRangeMin) / (xRangeMax - xRangeMin));
        }

        /// <summary>
        /// Evaluates the function at the given input.
        /// </summary>
        /// <param name="input">The array of input values for the function.
        /// In many cases will be an array of a single value, but not always.</param>
        /// <returns>ReturnValue = f(input) The of outputs the function returns based on those inputs.
        /// In many cases will be an array of a single value, but not always.</returns>
        public abstract float[] Eval(float[] input);

        /// <summary>
        /// parse
        /// </summary>
        /// <param name="functionDictionary"></param>
        /// <param name="pdfTokenScanner"></param>
        /// <returns></returns>
        public static PdfFunction Parse(DictionaryToken functionDictionary, IPdfTokenScanner pdfTokenScanner)
        {
            // PdfFunctionTypeIdentity???


            if (functionDictionary.TryGet<NumericToken>(NameToken.FunctionType, pdfTokenScanner, out var functionType))
            {
                // page 95
                switch (functionType.Int)
                {
                    case 0: // 0 Sampled function
                        return new PdfFunctionType0(functionDictionary, pdfTokenScanner);
                    case 2: // 2 Exponential interpolation function
                        throw new NotImplementedException();
                    case 3: // 3 Stitching function
                        return new PdfFunctionType3(functionDictionary, pdfTokenScanner);
                    case 4: // 4 PostScript calculator function
                        throw new NotImplementedException();
                    default:
                        throw new ArgumentException($"Error: Unknown function type {functionType}");
                }
            }
            else
            {
                throw new ArgumentException("FunctionType is Required.");
            }
        }
    }
}
