namespace UglyToad.PdfPig.Tests.Functions.Type4
{
    using System.Globalization;
    using UglyToad.PdfPig.Functions.Type4;

    /// <summary>
    /// Testing helper class for testing type 4 functions from the PDF specification.
    /// </summary>
    public sealed class Type4Tester
    {
        private readonly ExecutionContext context;

        private Type4Tester(ExecutionContext ctxt)
        {
            this.context = ctxt;
        }

        /// <summary>
        /// Creates a new instance for the given type 4 function.
        /// </summary>
        /// <param name="text">the text of the type 4 function</param>
        /// <returns>the tester instance</returns>
        public static Type4Tester Create(string text)
        {
            InstructionSequence instructions = InstructionSequenceBuilder.Parse(text.Trim());

            ExecutionContext context = new ExecutionContext(new Operators());
            instructions.Execute(context);
            return new Type4Tester(context);
        }

        /// <summary>
        /// Pops a bool value from the stack and checks it against the expected result.
        /// </summary>
        /// <param name="expected">the expected bool value</param>
        /// <returns>this instance</returns>
        public Type4Tester Pop(bool expected)
        {
            bool value = (bool)context.Stack.Pop();
            Assert.Equal(expected, value);
            return this;
        }

        /// <summary>
        /// Pops a real value from the stack and checks it against the expected result.
        /// </summary>
        /// <param name="expected">the expected real value</param>
        /// <returns>this instance</returns>
        public Type4Tester PopReal(double expected)
        {
            return PopReal(expected, 0.0000001);
        }

        /// <summary>
        /// Pops a real value from the stack and checks it against the expected result.
        /// </summary>
        /// <param name="expected">the expected real value</param>
        /// <param name="delta">delta the allowed deviation of the value from the expected result</param>
        /// <returns>this instance</returns>
        public Type4Tester PopReal(double expected, double delta)
        {
            double value = Convert.ToDouble(context.Stack.Pop(), CultureInfo.InvariantCulture);
            DoubleComparer doubleComparer = new DoubleComparer(delta);
            Assert.True(doubleComparer.Equals(expected, value));//expected, value, delta);
            return this;
        }

        /// <summary>
        /// Pops an int value from the stack and checks it against the expected result.
        /// </summary>
        /// <param name="expected">the expected int value</param>
        /// <returns>this instance</returns>
        public Type4Tester Pop(int expected)
        {
            int value = context.PopInt();
            Assert.Equal(expected, value);
            return this;
        }

        /// <summary>
        /// Pops a numeric value from the stack and checks it against the expected result.
        /// </summary>
        /// <param name="expected">the expected numeric value</param>
        /// <returns>this instance</returns>
        public Type4Tester Pop(double expected)
        {
            return Pop(expected, 0.0000001);
        }

        /// <summary>
        /// Pops a numeric value from the stack and checks it against the expected result.
        /// </summary>
        /// <param name="expected">the expected numeric value</param>
        /// <param name="delta">the allowed deviation of the value from the expected result</param>
        /// <returns>this instance</returns>
        public Type4Tester Pop(double expected, double delta)
        {
            object value = context.PopNumber();
            DoubleComparer doubleComparer = new DoubleComparer(delta);
            Assert.True(doubleComparer.Equals(expected, Convert.ToDouble(value, CultureInfo.InvariantCulture)));
            return this;
        }

        /// <summary>
        /// Checks that the stack is empty at this point.
        /// </summary>
        /// <returns>this instance</returns>
        public Type4Tester IsEmpty()
        {
            Assert.Empty(context.Stack);
            return this;
        }

        /// <summary>
        /// Returns the execution context so some custom checks can be performed.
        /// </summary>
        /// <returns>the associated execution context</returns>
        internal ExecutionContext ToExecutionContext()
        {
            return this.context;
        }
    }
}
