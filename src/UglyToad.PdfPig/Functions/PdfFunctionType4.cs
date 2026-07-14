namespace UglyToad.PdfPig.Functions
{
    using System;
    using System.Text;
    using UglyToad.PdfPig.Core;
    using UglyToad.PdfPig.Functions.Type4;
    using UglyToad.PdfPig.Tokens;

    /// <summary>
    /// PostScript calculator function
    /// </summary>
    internal sealed class PdfFunctionType4 : PdfFunction
    {
        /// <summary>
        /// PDF 32000-1:2008 (7.10.5.1) limits the Type 4 operand stack to 100 values, so this
        /// stack-allocated buffer covers all conforming programs; non-conforming programs grow
        /// into the array pool inside <see cref="OperandStack"/>.
        /// </summary>
        private const int InitialStackCapacity = 100;

        private readonly Type4Program program;

        /// <summary>
        /// PostScript calculator function
        /// </summary>
        internal PdfFunctionType4(StreamToken function, ArrayToken domain, ArrayToken range)
            : base(function, domain, range)
        {
            string str = OtherEncodings.Iso88591.GetString(FunctionStream!.Data.Span);
            this.program = Type4Compiler.Parse(str);
        }

        public override FunctionTypes FunctionType => FunctionTypes.PostScript;

        public override int Eval(ReadOnlySpan<double> input, Span<double> output)
        {
            Span<Operand> initialBuffer = stackalloc Operand[InitialStackCapacity];
            var stack = new OperandStack(initialBuffer);
            try
            {
                //Setup the input values
                for (int i = 0; i < input.Length; i++)
                {
                    PdfRange domain = GetDomainForInput(i);
                    double value = ClipToRange(input[i], domain.Min, domain.Max);
                    stack.Push(Operand.Real(value));
                }

                //Execute the type 4 function.
                program.Execute(ref stack);

                //Extract the output values
                int numberOfOutputValues = NumberOfOutputParameters;
                int numberOfActualOutputValues = stack.Count;
                if (numberOfActualOutputValues < numberOfOutputValues)
                {
                    throw new ArgumentOutOfRangeException("The type 4 function returned "
                            + numberOfActualOutputValues
                            + " values but the Range entry indicates that "
                            + numberOfOutputValues + " values be returned.");
                }
                for (int i = numberOfOutputValues - 1; i >= 0; i--)
                {
                    PdfRange range = GetRangeForOutput(i);
                    double v = stack.PopReal();
                    output[i] = ClipToRange(v, range.Min, range.Max);
                }

                return numberOfOutputValues;
            }
            finally
            {
                stack.Dispose();
            }
        }
    }
}
