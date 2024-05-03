namespace UglyToad.PdfPig.Functions
{
    using System.IO;
    using System.Linq;
    using UglyToad.PdfPig.Core;
    using UglyToad.PdfPig.Tokens;

    /// <summary>
    /// Stitching function
    /// </summary>
    internal sealed class PdfFunctionType3 : PdfFunction
    {
        private readonly double[] boundsValues;

        /// <summary>
        /// Stitching function
        /// </summary>
        internal PdfFunctionType3(DictionaryToken function, ArrayToken domain, ArrayToken? range, IReadOnlyList<PdfFunction> functionsArray, ArrayToken bounds, ArrayToken encode)
            : base(function, domain, range)
        {
            if (functionsArray is null || functionsArray.Count == 0)
            {
                throw new ArgumentNullException(nameof(functionsArray));
            }
            this.FunctionsArray = functionsArray;
            Bounds = bounds;
            Encode = encode;
            boundsValues = Bounds.Data.OfType<NumericToken>().Select(t => t.Double).ToArray();
        }

        /// <summary>
        /// Stitching function
        /// </summary>
        internal PdfFunctionType3(StreamToken function, ArrayToken domain, ArrayToken range, IReadOnlyList<PdfFunction> functionsArray, ArrayToken bounds, ArrayToken encode)
            : base(function, domain, range)
        {
            if (functionsArray is null || functionsArray.Count == 0)
            {
                throw new ArgumentNullException(nameof(functionsArray));
            }
            this.FunctionsArray = functionsArray;
            Bounds = bounds;
            Encode = encode;
            boundsValues = Bounds.Data.OfType<NumericToken>().Select(t => t.Double).ToArray();
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
            PdfFunction? function = null;
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
                int boundsSize = boundsValues.Length;
                // create a combined array containing the domain and the bounds values
                // domain.min, bounds[0], bounds[1], ...., bounds[boundsSize-1], domain.max
                var partitionValues = new double[boundsSize + 2];
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
            if (function is null)
            {
                throw new IOException("partition not found in type 3 function");
            }
            var functionValues = new double[] { x };
            // calculate the output values using the chosen function
            double[] functionResult = function.Eval(functionValues);
            // clip to range if available
            return ClipToRange(functionResult);
        }

        /// <summary>
        /// Returns all functions values.
        /// </summary>
        public IReadOnlyList<PdfFunction> FunctionsArray { get; }

        /// <summary>
        /// Returns all bounds values as <see cref="ArrayToken"/>.
        /// </summary>
        /// <returns>the bounds array.</returns>
        public ArrayToken Bounds { get; }

        /// <summary>
        /// Returns all encode values as <see cref="ArrayToken"/>.
        /// </summary>
        /// <returns>the encode array.</returns>
        public ArrayToken Encode { get; }

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
