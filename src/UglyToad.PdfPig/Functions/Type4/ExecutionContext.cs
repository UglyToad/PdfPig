namespace UglyToad.PdfPig.Functions.Type4
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    internal sealed class ExecutionContext
    {
        private readonly Operators operators;

        /// <summary>
        /// The stack used by this execution context.
        /// </summary>
        public Stack<object> Stack { get; private set; } = new Stack<object>();

        /// <summary>
        /// Creates a new execution context.
        /// </summary>
        /// <param name="operatorSet">the operator set</param>
        public ExecutionContext(Operators operatorSet)
        {
            this.operators = operatorSet;
        }

        internal void AddAllToStack(IEnumerable<object> values)
        {
            var valuesList = values.ToList();
            valuesList.AddRange(Stack);
            valuesList.Reverse();
            this.Stack = new Stack<object>(valuesList);
        }

        /// <summary>
        /// Returns the operator set used by this execution context.
        /// </summary>
        /// <returns>the operator set</returns>
        public Operators GetOperators()
        {
            return this.operators;
        }

        /// <summary>
        /// Pops a number (int or real) from the stack. If it's neither data type, a <see cref="InvalidCastException"/> is thrown.
        /// </summary>
        /// <returns>the number</returns>
        public object PopNumber()
        {
            object popped = this.Stack.Pop();
            if (popped is int || popped is double || popped is float)
            {
                return popped;
            }
            throw new InvalidCastException("The object popped is neither an integer or a real.");
        }

        /// <summary>
        /// Pops a value of type int from the stack. If the value is not of type int, a <see cref="InvalidCastException"/> is thrown.
        /// </summary>
        /// <returns>the int value</returns>
        public int PopInt()
        {
            object popped = Stack.Pop();
            if (popped is int poppedInt)
            {
                return poppedInt;
            }
            throw new InvalidCastException("PopInt cannot be done as the value is not integer");
        }

        /// <summary>
        /// Pops a number from the stack and returns it as a real value. If the value is not of a numeric type,
        /// a <see cref="InvalidCastException"/> is thrown.
        /// </summary>
        /// <returns>the real value</returns>
        public double PopReal()
        {
            return Convert.ToDouble(Stack.Pop());
        }
    }
}
