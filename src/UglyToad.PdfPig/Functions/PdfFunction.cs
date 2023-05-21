namespace UglyToad.PdfPig.Functions
{
    using System.Linq;
    using UglyToad.PdfPig.Core;
    using UglyToad.PdfPig.Tokens;

    /// <summary>
    /// This class represents a function in a PDF document.
    /// </summary>
    public abstract class PdfFunction
    {
        /// <summary>
        /// The function dictionary.
        /// </summary>
        public DictionaryToken FunctionDictionary { get; }

        /// <summary>
        /// The function stream.
        /// </summary>
        public StreamToken FunctionStream { get; }

        private int numberOfInputValues = -1;
        private int numberOfOutputValues = -1;

        /// <summary>
        /// This class represents a function in a PDF document.
        /// </summary>
        public PdfFunction(DictionaryToken function, ArrayToken domain, ArrayToken range)
        {
            FunctionDictionary = function;
            DomainValues = domain;
            RangeValues = range;
        }

        /// <summary>
        /// This class represents a function in a PDF document.
        /// </summary>
        public PdfFunction(StreamToken function, ArrayToken domain, ArrayToken range)
        {
            FunctionStream = function;
            DomainValues = domain;
            RangeValues = range;
        }

        /// <summary>
        /// Returns the function type. Possible values are:
        /// <list type="bullet">
        /// <item><term>0</term><description>Sampled function</description></item>
        /// <item><term>2</term><description>Exponential interpolation function</description></item>
        /// <item><term>3</term><description>Stitching function</description></item>
        /// <item><term>4</term><description>PostScript calculator function</description></item>
        /// </list>
        /// </summary>
        /// <returns>the function type.</returns>
        public abstract FunctionTypes FunctionType { get; }

        /// <summary>
        /// Returns the function's dictionary. If <see cref="FunctionDictionary"/> is defined, it will be returned.
        /// If not, the <see cref="FunctionStream"/>'s StreamDictionary will be returned.
        /// </summary>
        public DictionaryToken GetDictionary()
        {
            if (FunctionStream != null)
            {
                return FunctionStream.StreamDictionary;
            }
            else
            {
                return FunctionDictionary;
            }
        }

        /// <summary>
        /// This will get the number of output parameters that
        /// have a range specified. A range for output parameters
        /// is optional so this may return zero for a function
        /// that does have output parameters, this will simply return the
        /// number that have the range specified.
        /// </summary>
        /// <returns>The number of output parameters that have a range specified.</returns>
        public int NumberOfOutputParameters
        {
            get
            {
                if (numberOfOutputValues == -1)
                {
                    if (RangeValues == null)
                    {
                        numberOfOutputValues = 0;
                    }
                    else
                    {
                        numberOfOutputValues = RangeValues.Length / 2;
                    }
                }
                return numberOfOutputValues;
            }
        }

        /// <summary>
        /// This will get the range for a certain output parameters. This is will never
        /// return null.  If it is not present then the range 0 to 0 will
        /// be returned.
        /// </summary>
        /// <param name="n">The output parameter number to get the range for.</param>
        /// <returns>The range for this component.</returns>
        public PdfRange GetRangeForOutput(int n)
        {
            return new PdfRange(RangeValues.Data.OfType<NumericToken>().Select(t => t.Double), n);
        }

        /// <summary>
        /// This will get the number of input parameters that
        /// have a domain specified.
        /// </summary>
        /// <returns>The number of input parameters that have a domain specified.</returns>
        public int NumberOfInputParameters
        {
            get
            {
                if (numberOfInputValues == -1)
                {
                    ArrayToken array = DomainValues;
                    numberOfInputValues = array.Length / 2;
                }
                return numberOfInputValues;
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
            ArrayToken domainValues = DomainValues;
            return new PdfRange(domainValues.Data.OfType<NumericToken>().Select(t => t.Double), n);
        }

        /// <summary>
        /// Evaluates the function at the given input.
        /// ReturnValue = f(input)
        /// </summary>
        /// <param name="input">The array of input values for the function.
        /// In many cases will be an array of a single value, but not always.</param>
        /// <returns>The of outputs the function returns based on those inputs.
        /// In many cases will be an array of a single value, but not always.</returns>
        public abstract double[] Eval(params double[] input);

        /// <summary>
        /// Returns all ranges for the output values as <see cref="ArrayToken"/>. Required for type 0 and type 4 functions.
        /// </summary>
        /// <returns>the ranges array.</returns>
        protected ArrayToken RangeValues { get; }

        /// <summary>
        /// Returns all domains for the input values as <see cref="ArrayToken"/>. Required for all function types.
        /// </summary>
        /// <returns>the domains array.</returns>
        private ArrayToken DomainValues { get; }

        /// <summary>
        /// Clip the given input values to the ranges.
        /// </summary>
        /// <param name="inputValues">inputValues the input values</param>
        /// <returns>the clipped values</returns>
        protected double[] ClipToRange(double[] inputValues)
        {
            ArrayToken rangesArray = RangeValues;
            double[] result;
            if (rangesArray != null && rangesArray.Length > 0)
            {
                double[] rangeValues = rangesArray.Data.OfType<NumericToken>().Select(t => t.Double).ToArray();
                int numberOfRanges = rangeValues.Length / 2;
                result = new double[numberOfRanges];
                for (int i = 0; i < numberOfRanges; i++)
                {
                    int index = i << 1;
                    result[i] = ClipToRange(inputValues[i], rangeValues[index], rangeValues[index + 1]);
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
        /// <param name="rangeMin">the min value of the range</param>
        /// <param name="rangeMax">the max value of the range</param>
        /// <returns>the clipped value</returns>
        public static double ClipToRange(double x, double rangeMin, double rangeMax)
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
        protected static double Interpolate(double x, double xRangeMin, double xRangeMax, double yRangeMin, double yRangeMax)
        {
            return yRangeMin + ((x - xRangeMin) * (yRangeMax - yRangeMin) / (xRangeMax - xRangeMin));
        }
    }

    /// <summary>
    /// Pdf function types.
    /// </summary>
    public enum FunctionTypes : byte
    {
        /// <summary>
        /// Sampled function.
        /// </summary>
        Sampled = 0,

        /// <summary>
        /// Exponential interpolation function.
        /// </summary>
        Exponential = 2,

        /// <summary>
        /// Stitching function.
        /// </summary>
        Stitching = 3,

        /// <summary>
        /// PostScript calculator function.
        /// </summary>
        PostScript = 4
    }
}
