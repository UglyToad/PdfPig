namespace UglyToad.PdfPig.Tests.Functions.Type4
{
    using UglyToad.PdfPig.Functions.Type4;

    /// <summary>
    /// Testing helper class for testing type 4 functions from the PDF specification.
    /// </summary>
    public sealed class Type4Tester
    {
        private readonly List<Operand> stack; // bottom first, top last

        private Type4Tester(List<Operand> stack)
        {
            this.stack = stack;
        }

        /// <summary>
        /// Creates a new instance for the given type 4 function.
        /// </summary>
        /// <param name="text">the text of the type 4 function</param>
        /// <returns>the tester instance</returns>
        public static Type4Tester Create(string text)
        {
            Type4Program program = Type4Compiler.Parse(text.Trim());
            var operandStack = new OperandStack(new Operand[100]);
            program.Execute(ref operandStack);
            return new Type4Tester(new List<Operand>(operandStack.ToArray()));
        }

        /// <summary>
        /// Pops the raw operand from the top of the stack so kind checks can be performed.
        /// </summary>
        internal Operand PopOperand()
        {
            Operand op = stack[stack.Count - 1];
            stack.RemoveAt(stack.Count - 1);
            return op;
        }

        /// <summary>
        /// Pops a bool value from the stack and checks it against the expected result.
        /// </summary>
        public Type4Tester Pop(bool expected)
        {
            Operand op = PopOperand();
            Assert.Equal(OperandKind.Boolean, op.Kind);
            Assert.Equal(expected, op.AsBoolean);
            return this;
        }

        /// <summary>
        /// Pops a real value from the stack and checks it against the expected result.
        /// </summary>
        public Type4Tester PopReal(double expected)
        {
            return PopReal(expected, 0.0000001);
        }

        /// <summary>
        /// Pops a real value from the stack and checks it against the expected result.
        /// </summary>
        public Type4Tester PopReal(double expected, double delta)
        {
            // Like the previous Convert.ToDouble-based implementation this accepts any number.
            Operand op = PopOperand();
            Assert.True(op.IsNumber);
            Assert.True(new DoubleComparer(delta).Equals(expected, op.Value));
            return this;
        }

        /// <summary>
        /// Pops an int value from the stack and checks it against the expected result.
        /// </summary>
        public Type4Tester Pop(int expected)
        {
            Operand op = PopOperand();
            Assert.Equal(OperandKind.Integer, op.Kind);
            Assert.Equal(expected, op.AsInteger);
            return this;
        }

        /// <summary>
        /// Pops a numeric value from the stack and checks it against the expected result.
        /// </summary>
        public Type4Tester Pop(double expected)
        {
            return Pop(expected, 0.0000001);
        }

        /// <summary>
        /// Pops a numeric value from the stack and checks it against the expected result.
        /// </summary>
        public Type4Tester Pop(double expected, double delta)
        {
            Operand op = PopOperand();
            Assert.True(op.IsNumber);
            Assert.True(new DoubleComparer(delta).Equals(expected, op.Value));
            return this;
        }

        /// <summary>
        /// Checks that the stack is empty at this point.
        /// </summary>
        public Type4Tester IsEmpty()
        {
            Assert.Empty(stack);
            return this;
        }
    }
}
