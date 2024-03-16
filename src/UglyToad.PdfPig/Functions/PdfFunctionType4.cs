namespace UglyToad.PdfPig.Functions
{
    using System;
    using System.Linq;
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
            byte[] bytes = FunctionStream!.Data.ToArray();
            string str = OtherEncodings.Iso88591.GetString(bytes);
            this.instructions = InstructionSequenceBuilder.Parse(str);
        }

        public override FunctionTypes FunctionType
        {
            get
            {
                return FunctionTypes.PostScript;
            }
        }

        public override double[] Eval(params double[] input)
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
            double[] outputValues = new double[numberOfOutputValues];
            for (int i = numberOfOutputValues - 1; i >= 0; i--)
            {
                PdfRange range = GetRangeForOutput(i);
                outputValues[i] = context.PopReal();
                outputValues[i] = ClipToRange(outputValues[i], range.Min, range.Max);
            }

            //Return the resulting array
            return outputValues;
        }
    }
}
