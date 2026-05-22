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
        private readonly double[] boundsValues;
        private readonly double[] encodeValuesCache;

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
            encodeValuesCache = Encode.Data.OfType<NumericToken>().Select(t => t.Double).ToArray();
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
            encodeValuesCache = Encode.Data.OfType<NumericToken>().Select(t => t.Double).ToArray();
        }

        public override FunctionTypes FunctionType => FunctionTypes.Stitching;

        protected internal override int MaxOutputComponentCount
        {
            get
            {
                int max = NumberOfOutputParameters;
                for (int i = 0; i < FunctionsArray.Count; i++)
                {
                    int sub = FunctionsArray[i].MaxOutputComponentCount;
                    if (sub > max)
                    {
                        max = sub;
                    }
                }
                return max > 0 ? max : 1;
            }
        }

        public override int Eval(ReadOnlySpan<double> input, Span<double> output)
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
                PdfRange encRange = GetEncodeRangeFor(0);
                x = Interpolate(x, domain.Min, domain.Max, encRange.Min, encRange.Max);
            }
            else
            {
                int boundsSize = boundsValues.Length;
                // Inline the partition lookup: for index i, the partition is [partition_i, partition_{i+1}]
                // where partition_0 = domain.Min, partition_{boundsSize+1} = domain.Max, and partition_k = boundsValues[k-1] otherwise.
                int last = boundsSize; // last index in the "partition" sequence corresponds to the closing boundary
                for (int i = 0; i <= boundsSize; i++)
                {
                    double left = i == 0 ? domain.Min : boundsValues[i - 1];
                    double right = i == boundsSize ? domain.Max : boundsValues[i];
                    if (x >= left && (x < right || (i == last && x == right)))
                    {
                        function = FunctionsArray[i];
                        PdfRange encRange = GetEncodeRangeFor(i);
                        x = Interpolate(x, left, right, encRange.Min, encRange.Max);
                        break;
                    }
                }
            }
            if (function is null)
            {
                throw new IOException("partition not found in type 3 function");
            }
            Span<double> inputBuffer = stackalloc double[1];
            inputBuffer[0] = x;
            int written = function.Eval(inputBuffer, output);
            // clip to range if available
            ClipToRange(output.Slice(0, written));
            return written;
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
        /// Get the encode range for the input parameter.
        /// </summary>
        /// <param name="n">The function parameter number.</param>
        /// <returns>The encode parameter range or null if none is set.</returns>
        private PdfRange GetEncodeRangeFor(int n)
        {
            return new PdfRange(encodeValuesCache, n);
        }
    }
}
