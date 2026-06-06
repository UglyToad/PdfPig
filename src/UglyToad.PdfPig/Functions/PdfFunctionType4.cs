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
        private readonly Operators operators = new Operators();
        private readonly InstructionSequence instructions;
        
        /// <summary>
        /// PostScript calculator function
        /// </summary>
        internal PdfFunctionType4(StreamToken function, ArrayToken domain, ArrayToken range)
            : base(function, domain, range)
        {
            string str = OtherEncodings.Iso88591.GetString(FunctionStream!.Data.Span);
            this.instructions = InstructionSequenceBuilder.Parse(str);
        }

        public override FunctionTypes FunctionType => FunctionTypes.PostScript;

        public override int Eval(ReadOnlySpan<double> input, Span<double> output)
        {
            //Setup the input values
            ExecutionContext context = new ExecutionContext(operators);
            for (int i = 0; i < input.Length; i++)
            {
                PdfRange domain = GetDomainForInput(i);
                double value = ClipToRange(input[i], domain.Min, domain.Max);
                context.Stack.Push(value);
            }

            //Execute the type 4 function.
            instructions.Execute(context);

            //Extract the output values
            int numberOfOutputValues = NumberOfOutputParameters;
            int numberOfActualOutputValues = context.Stack.Count;
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
                double v = context.PopReal();
                output[i] = ClipToRange(v, range.Min, range.Max);
            }

            return numberOfOutputValues;
        }
    }
}
