namespace UglyToad.PdfPig.Functions.Type4
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Provides the relational operators such as "eq" and "le".
    /// </summary>
    internal sealed class RelationalOperators
    {
        private RelationalOperators()
        {
            // Private constructor.
        }

        /// <summary>
        /// Implements the "eq" operator.
        /// </summary>
        internal class Eq : Operator
        {
            public void Execute(ExecutionContext context)
            {
                object op2 = context.Stack.Pop();
                object op1 = context.Stack.Pop();
                bool result = IsEqual(op1, op2);
                context.Stack.Push(result);
            }

            protected virtual bool IsEqual(object op1, object op2)
            {
                bool result;
                if (op1 is double num1 && op2 is double num2)
                {
                    result = num1.Equals(num2);
                }
                else
                {
                    result = op1.Equals(op2);
                }
                return result;
            }
        }

        /// <summary>
        /// Abstract base class for number comparison operators.
        /// </summary>
        internal abstract class AbstractNumberComparisonOperator : Operator
        {
            public void Execute(ExecutionContext context)
            {
                object op2 = context.Stack.Pop();
                object op1 = context.Stack.Pop();
                double num1 = Convert.ToDouble(op1);
                double num2 = Convert.ToDouble(op2);
                bool result = Compare(num1, num2);
                context.Stack.Push(result);
            }

            protected abstract bool Compare(double num1, double num2);
        }

        /// <summary>
        /// Implements the "ge" operator.
        /// </summary>
        internal sealed class Ge : AbstractNumberComparisonOperator
        {
            protected override bool Compare(double num1, double num2)
            {
                return num1 >= num2;
            }
        }

        /// <summary>
        /// Implements the "gt" operator.
        /// </summary>
        internal sealed class Gt : AbstractNumberComparisonOperator
        {
            protected override bool Compare(double num1, double num2)
            {
                return num1 > num2;
            }
        }

        /// <summary>
        /// Implements the "le" operator.
        /// </summary>
        internal sealed class Le : AbstractNumberComparisonOperator
        {
            protected override bool Compare(double num1, double num2)
            {
                return num1 <= num2;
            }
        }

        /// <summary>
        /// Implements the "lt" operator.
        /// </summary>
        internal sealed class Lt : AbstractNumberComparisonOperator
        {
            protected override bool Compare(double num1, double num2)
            {
                return num1 < num2;
            }
        }

        /// <summary>
        /// Implements the "ne" operator.
        /// </summary>
        internal sealed class Ne : Eq
        {
            protected override bool IsEqual(object op1, object op2)
            {
                bool result = base.IsEqual(op1, op2);
                return !result;
            }
        }
    }
}
