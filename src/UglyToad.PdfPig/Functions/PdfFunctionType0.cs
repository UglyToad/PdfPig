namespace UglyToad.PdfPig.Functions
{
    using System;
    using System.Collections;
    using System.IO;
    using System.Linq;
    using UglyToad.PdfPig.Core;
    using UglyToad.PdfPig.Tokens;

    internal sealed class PdfFunctionType0 : PdfFunction
    {
        /// <summary>
        /// The samples of the function.
        /// </summary>
        private int[][]? samples;

        /// <summary>
        /// Cached numeric values for Size, Encode and Decode entries (parsed once at construction).
        /// </summary>
        private readonly double[] sizeValues;
        private readonly double[] encodeValuesCache;
        private readonly double[] decodeValuesCache;

        /// <summary>
        /// Stitching function
        /// </summary>
        internal PdfFunctionType0(DictionaryToken function, ArrayToken domain, ArrayToken range, ArrayToken size, int bitsPerSample, int order, ArrayToken encode, ArrayToken decode)
            : base(function, domain, range)
        {
            Size = size;
            BitsPerSample = bitsPerSample;
            Order = order;
            EncodeValues = encode;
            DecodeValues = decode;
            sizeValues = size.Data.OfType<NumericToken>().Select(t => t.Double).ToArray();
            encodeValuesCache = encode.Data.OfType<NumericToken>().Select(t => t.Double).ToArray();
            decodeValuesCache = decode.Data.OfType<NumericToken>().Select(t => t.Double).ToArray();
        }

        /// <summary>
        /// Stitching function
        /// </summary>
        internal PdfFunctionType0(StreamToken function, ArrayToken domain, ArrayToken range, ArrayToken size, int bitsPerSample, int order, ArrayToken encode, ArrayToken decode)
            : base(function, domain, range)
        {
            Size = size;
            BitsPerSample = bitsPerSample;
            Order = order;
            EncodeValues = encode;
            DecodeValues = decode;
            sizeValues = size.Data.OfType<NumericToken>().Select(t => t.Double).ToArray();
            encodeValuesCache = encode.Data.OfType<NumericToken>().Select(t => t.Double).ToArray();
            decodeValuesCache = decode.Data.OfType<NumericToken>().Select(t => t.Double).ToArray();
        }

        public override FunctionTypes FunctionType => FunctionTypes.Sampled;

        /// <summary>
        /// The "Size" entry, which is the number of samples in each input dimension of the sample table.
        /// <para>An array of m positive integers specifying the number of samples in each input dimension of the sample table.</para>
        /// </summary>
        public ArrayToken Size { get; }

        /// <summary>
        /// Get the number of bits that the output value will take up.
        /// <para>Valid values are 1,2,4,8,12,16,24,32.</para>
        /// </summary>
        /// <returns>Number of bits for each output value.</returns>
        public int BitsPerSample { get; }

        /// <summary>
        /// Get the order of interpolation between samples. Valid values are 1 and 3,
        /// specifying linear and cubic spline interpolation, respectively. Default
        /// is 1. See p.170 in PDF spec 1.7.
        /// </summary>
        /// <returns>order of interpolation.</returns>
        public int Order { get; }

        /// <summary>
        /// An array of 2 x m numbers specifying the linear mapping of input values
        /// into the domain of the function's sample table. Default value: [ 0 (Size0
        /// - 1) 0 (Size1 - 1) ...].
        /// </summary>
        private ArrayToken? EncodeValues { get; }

        /// <summary>
        /// An array of 2 x n numbers specifying the linear mapping of sample values
        /// into the range appropriate for the function's output values. Default
        /// value: same as the value of Range.
        /// </summary>
        private ArrayToken? DecodeValues { get; }

        /// <summary>
        /// Get the encode for the input parameter.
        /// </summary>
        /// <param name="paramNum">The function parameter number.</param>
        /// <returns>The encode parameter range or null if none is set.</returns>
        public PdfRange? GetEncodeForParameter(int paramNum)
        {
            ArrayToken? encodeValues = EncodeValues;
            if (encodeValues is not null && encodeValues.Length >= paramNum * 2 + 1)
            {
                return new PdfRange(encodeValuesCache, paramNum);
            }
            return null;
        }

        /// <summary>
        /// Get the decode for the input parameter.
        /// </summary>
        /// <param name="paramNum">The function parameter number.</param>
        /// <returns>The decode parameter range or null if none is set.</returns>
        public PdfRange? GetDecodeForParameter(int paramNum)
        {
            ArrayToken? decodeValues = DecodeValues;
            if (decodeValues is not null && decodeValues.Length >= paramNum * 2 + 1)
            {
                return new PdfRange(decodeValuesCache, paramNum);
            }
            return null;
        }

        /// <summary>
        /// Inner class do to an interpolation in the Nth dimension by comparing the
        /// content size of N-1 dimensional objects.This is done with the help of
        /// recursive calls.
        /// <para>To understand the algorithm without recursion, see <see href="http://harmoniccode.blogspot.de/2011/04/bilinear-color-interpolation.html"/> for a bilinear interpolation
        /// and <see href="https://en.wikipedia.org/wiki/Trilinear_interpolation"/> for trilinear interpolation.
        /// </para>
        /// </summary>
        private readonly ref struct RInterpol
        {
            // coordinate that is to be interpolated
            private readonly ReadOnlySpan<double> in_;
            // coordinate of the "ceil" point
            private readonly ReadOnlySpan<int> inPrev;
            // coordinate of the "floor" point
            private readonly ReadOnlySpan<int> inNext;
            private readonly int numberOfInputValues;
            private readonly int numberOfOutputValues;
            private readonly ReadOnlySpan<double> sizeValues;

            private readonly int[][] samples;

            /// <summary>
            /// Constructor.
            /// </summary>
            internal RInterpol(ReadOnlySpan<double> input,
                ReadOnlySpan<int> inputPrev,
                ReadOnlySpan<int> inputNext,
                int numberOfOutputValues,
                ReadOnlySpan<double> sizeValues,
                int[][] samples)
            {
                in_ = input;
                inPrev = inputPrev;
                inNext = inputNext;
                numberOfInputValues = input.Length;
                this.numberOfOutputValues = numberOfOutputValues;
                this.sizeValues = sizeValues;
                this.samples = samples;
            }

            /// <summary>
            /// Calculate the interpolation. Writes <see cref="numberOfOutputValues"/> doubles into
            /// <paramref name="result"/>. Uses <paramref name="buffer"/> as working space across the
            /// recursion (must hold <c>2 * numberOfInputValues * numberOfOutputValues</c> doubles).
            /// </summary>
            internal void RInterpolate(Span<double> result, Span<double> buffer, Span<int> coord)
            {
                coord.Clear();
                InternalRInterpol(coord, 0, result, buffer);
            }

            /// <summary>
            /// Do a linear interpolation if the two coordinates can be known, or
            /// call itself recursively twice. Writes the interpolated sample into <paramref name="result"/>.
            /// At each branching level, the first two output-sized slots of <paramref name="buffer"/> hold
            /// sample1 and sample2 for the current level; the remainder is forwarded to deeper levels.
            /// </summary>
            private void InternalRInterpol(Span<int> coord, int step, Span<double> result, Span<double> buffer)
            {
                if (step == numberOfInputValues - 1)
                {
                    // leaf
                    if (inPrev[step] == inNext[step])
                    {
                        coord[step] = inPrev[step];
                        int[] tmpSample = samples[CalcSampleIndex(coord)];
                        for (int i = 0; i < numberOfOutputValues; ++i)
                        {
                            result[i] = tmpSample[i];
                        }
                        return;
                    }
                    coord[step] = inPrev[step];
                    int[] sample1Leaf = samples[CalcSampleIndex(coord)];
                    coord[step] = inNext[step];
                    int[] sample2Leaf = samples[CalcSampleIndex(coord)];
                    for (int i = 0; i < numberOfOutputValues; ++i)
                    {
                        result[i] = Interpolate(in_[step], inPrev[step], inNext[step], sample1Leaf[i], sample2Leaf[i]);
                    }
                    return;
                }

                // branch
                if (inPrev[step] == inNext[step])
                {
                    coord[step] = inPrev[step];
                    InternalRInterpol(coord, step + 1, result, buffer);
                    return;
                }

                // Carve sample1/sample2 slots out of buffer; forward the remainder to deeper levels.
                Span<double> sample1 = buffer.Slice(0, numberOfOutputValues);
                Span<double> sample2 = buffer.Slice(numberOfOutputValues, numberOfOutputValues);
                Span<double> deeperBuffer = buffer.Slice(2 * numberOfOutputValues);

                coord[step] = inPrev[step];
                InternalRInterpol(coord, step + 1, sample1, deeperBuffer);
                coord[step] = inNext[step];
                InternalRInterpol(coord, step + 1, sample2, deeperBuffer);

                for (int i = 0; i < numberOfOutputValues; ++i)
                {
                    result[i] = Interpolate(in_[step], inPrev[step], inNext[step], sample1[i], sample2[i]);
                }
            }

            /// <summary>
            /// calculate array index (structure described in p.171 PDF spec 1.7) in multiple dimensions.
            /// </summary>
            private int CalcSampleIndex(ReadOnlySpan<int> vector)
            {
                int index = 0;
                int sizeProduct = 1;
                int dimension = vector.Length;
                for (int i = dimension - 2; i >= 0; --i)
                {
                    sizeProduct = (int)(sizeProduct * sizeValues[i]);
                }
                for (int i = dimension - 1; i >= 0; --i)
                {
                    index += sizeProduct * vector[i];
                    if (i - 1 >= 0)
                    {
                        sizeProduct = (int)(sizeProduct / sizeValues[i - 1]);
                    }
                }
                return index;
            }
        }

        /// <summary>
        /// Get all sample values of this function.
        /// </summary>
        /// <returns>an array with all samples.</returns>
        private int[][] GetSamples()
        {
            if (samples is null)
            {
                int arraySize = 1;
                int nIn = NumberOfInputParameters;
                int nOut = NumberOfOutputParameters;
                for (int i = 0; i < nIn; i++)
                {
                    arraySize *= (int)sizeValues[i];
                }
                samples = new int[arraySize][];
                int bitsPerSample = BitsPerSample;

                // PDF spec 1.7 p.171:
                // Each sample value is represented as a sequence of BitsPerSample bits.
                // Successive values are adjacent in the bit stream; there is no padding at byte boundaries.
                var bits = new BitArray(FunctionStream!.Data.ToArray());
                
                for (int i = 0; i < arraySize; i++)
                {
                    samples[i] = new int[nOut];
                    for (int k = 0; k < nOut; k++)
                    {
                        long accum = 0L;
                        for (int l = bitsPerSample - 1; l >= 0; l--)
                        {
                            accum <<= 1;
                            accum |= bits[i * nOut * bitsPerSample + (k * bitsPerSample) + l] ? (uint)1 : 0;
                        }

                        // TODO will this cast work properly for 32 bitsPerSample or should we use long[]?
                        samples[i][k] = (int)accum;
                    }
                }
            }

            return samples;
        }

        public override int Eval(ReadOnlySpan<double> input, Span<double> output)
        {
            //This involves linear interpolation based on a set of sample points.
            //Theoretically it's not that difficult ... see section 3.9.1 of the PDF Reference.

            int bitsPerSample = BitsPerSample;
            double maxSample = Math.Pow(2, bitsPerSample) - 1.0;
            int numberOfInputValues = input.Length;
            int numberOfOutputValues = NumberOfOutputParameters;

            Span<double> workInput = numberOfInputValues <= 16 ? stackalloc double[numberOfInputValues] : new double[numberOfInputValues];
            Span<int> inputPrev = numberOfInputValues <= 16 ? stackalloc int[numberOfInputValues] : new int[numberOfInputValues];
            Span<int> inputNext = numberOfInputValues <= 16 ? stackalloc int[numberOfInputValues] : new int[numberOfInputValues];


            for (int i = 0; i < numberOfInputValues; i++)
            {
                PdfRange domain = GetDomainForInput(i);
                PdfRange encodeValues = GetEncodeForParameter(i)!.Value;
                double v = ClipToRange(input[i], domain.Min, domain.Max);
                v = Interpolate(v, domain.Min, domain.Max, encodeValues.Min, encodeValues.Max);
                v = ClipToRange(v, 0, sizeValues[i] - 1);
                workInput[i] = v;
                inputPrev[i] = (int)Math.Floor(v);
                inputNext[i] = (int)Math.Ceiling(v);
            }
            
            Span<double> interpolated = numberOfOutputValues <= 128 ? stackalloc double[numberOfOutputValues] : new double[numberOfOutputValues];
            int bufferDoubleSize = 2 * numberOfInputValues * numberOfOutputValues;
            Span<double> rInterpolBuffer = bufferDoubleSize <= 128 ? stackalloc double[bufferDoubleSize] : new double[bufferDoubleSize];
            Span<int> coord = numberOfInputValues <= 16 ? stackalloc int[numberOfInputValues] : new int[numberOfInputValues];

            new RInterpol(workInput, inputPrev, inputNext, numberOfOutputValues, sizeValues, GetSamples())
                .RInterpolate(interpolated, rInterpolBuffer, coord);

            for (int i = 0; i < numberOfOutputValues; i++)
            {
                PdfRange range = GetRangeForOutput(i);
                PdfRange? decodeValues = GetDecodeForParameter(i);
                if (!decodeValues.HasValue)
                {
                    throw new IOException("Range missing in function /Decode entry");
                }
                double v = Interpolate(interpolated[i], 0, maxSample, decodeValues.Value.Min, decodeValues.Value.Max);
                output[i] = ClipToRange(v, range.Min, range.Max);
            }

            return numberOfOutputValues;
        }
    }
}
