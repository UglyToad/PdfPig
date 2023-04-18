namespace UglyToad.PdfPig.Functions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using UglyToad.PdfPig.Core;
    using UglyToad.PdfPig.Tokens;

    /// <summary>
    /// Stitching function
    /// </summary>
    internal sealed class PdfFunctionType3 : PdfFunction
    {
        private ArrayToken functions;
        private ArrayToken encode;
        private ArrayToken bounds;
        private double[] boundsValues;

        /// <summary>
        /// Stitching function
        /// </summary>
        internal PdfFunctionType3(DictionaryToken function, IReadOnlyList<PdfFunction> functionsArray)
            : base(function)
        {
            if (functionsArray == null || functionsArray.Count == 0)
            {
                throw new ArgumentNullException(nameof(functionsArray));
            }
            this.FunctionsArray = functionsArray;
        }

        /// <summary>
        /// Stitching function
        /// </summary>
        internal PdfFunctionType3(StreamToken function, IReadOnlyList<PdfFunction> functionsArray)
            : base(function)
        {
            if (functionsArray == null || functionsArray.Count == 0)
            {
                throw new ArgumentNullException(nameof(functionsArray));
            }
            this.FunctionsArray = functionsArray;
        }

        public override FunctionTypes FunctionType
        {
            get
            {
                return FunctionTypes.Stitching;
            }
        }

        public override double[] Eval(params double[] input)
        {
            // This function is known as a "stitching" function. Based on the input, it decides which child function to call.
            // All functions in the array are 1-value-input functions
            // See PDF Reference section 3.9.3.
            PdfFunction function = null;
            double x = input[0];
            PdfRange domain = GetDomainForInput(0);
            // clip input value to domain
            x = ClipToRange(x, domain.Min, domain.Max);

            if (FunctionsArray.Count == 1)
            {
                // This doesn't make sense but it may happen...
                function = FunctionsArray[0];
                PdfRange encRange = GetEncodeForParameter(0);
                x = Interpolate(x, domain.Min, domain.Max, encRange.Min, encRange.Max);
            }
            else
            {
                if (boundsValues == null)
                {
                    boundsValues = Bounds.Data.OfType<NumericToken>().Select(t => t.Double).ToArray();
                }

                int boundsSize = boundsValues.Length;
                // create a combined array containing the domain and the bounds values
                // domain.min, bounds[0], bounds[1], ...., bounds[boundsSize-1], domain.max
                double[] partitionValues = new double[boundsSize + 2];
                int partitionValuesSize = partitionValues.Length;
                partitionValues[0] = domain.Min;
                partitionValues[partitionValuesSize - 1] = domain.Max;
                Array.Copy(boundsValues, 0, partitionValues, 1, boundsSize); // System.arraycopy(boundsValues, 0, partitionValues, 1, boundsSize);
                // find the partition 
                for (int i = 0; i < partitionValuesSize - 1; i++)
                {
                    if (x >= partitionValues[i] &&
                            (x < partitionValues[i + 1] || (i == partitionValuesSize - 2 && x == partitionValues[i + 1])))
                    {
                        function = FunctionsArray[i];
                        PdfRange encRange = GetEncodeForParameter(i);
                        x = Interpolate(x, partitionValues[i], partitionValues[i + 1], encRange.Min, encRange.Max);
                        break;
                    }
                }
            }
            if (function == null)
            {
                throw new IOException("partition not found in type 3 function");
            }
            double[] functionValues = new double[] { x };
            // calculate the output values using the chosen function
            double[] functionResult = function.Eval(functionValues);
            // clip to range if available
            return ClipToRange(functionResult);
        }

        public IReadOnlyList<PdfFunction> FunctionsArray { get; }

        /// <summary>
        /// Returns all functions values as <see cref="ArrayToken"/>.
        /// </summary>
        /// <returns>the functions array. </returns>
        public ArrayToken Functions
        {
            get
            {
                if (functions == null && !GetDictionary().TryGet<ArrayToken>(NameToken.Functions, out functions))
                {
                    throw new ArgumentNullException(NameToken.Functions);
                }
                return functions;
            }
        }

        /// <summary>
        /// Returns all bounds values as <see cref="ArrayToken"/>.
        /// </summary>
        /// <returns>the bounds array.</returns>
        public ArrayToken Bounds
        {
            get
            {
                if (bounds == null && !GetDictionary().TryGet<ArrayToken>(NameToken.Bounds, out bounds))
                {
                    throw new ArgumentNullException(NameToken.Bounds);
                }
                return bounds;
            }
        }

        /// <summary>
        /// Returns all encode values as <see cref="ArrayToken"/>.
        /// </summary>
        /// <returns>the encode array.</returns>
        public ArrayToken Encode
        {
            get
            {
                if (encode == null && !GetDictionary().TryGet<ArrayToken>(NameToken.Encode, out encode))
                {
                    throw new ArgumentNullException(NameToken.Encode);
                }
                return encode;
            }
        }

        /// <summary>
        /// Get the encode for the input parameter.
        /// </summary>
        /// <param name="n">The function parameter number.</param>
        /// <returns>The encode parameter range or null if none is set.</returns>
        private PdfRange GetEncodeForParameter(int n)
        {
            ArrayToken encodeValues = Encode;
            return new PdfRange(encodeValues.Data.OfType<NumericToken>().Select(t => t.Double), n);
        }
    }
}
