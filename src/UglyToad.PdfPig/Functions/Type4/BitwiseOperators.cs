namespace UglyToad.PdfPig.Functions.Type4
{
    internal sealed class BitwiseOperators
    {
        private BitwiseOperators()
        {
            // Private constructor.
        }

        /// <summary>
        /// Abstract base class for logical operators.
        /// </summary>
        internal abstract class AbstractLogicalOperator : Operator
        {
            public void Execute(ExecutionContext context)
            {
                object op2 = context.Stack.Pop();
                object op1 = context.Stack.Pop();
                if (op1 is bool bool1 && op2 is bool bool2)
                {
                    bool result = ApplyForBoolean(bool1, bool2);
                    context.Stack.Push(result);
                }
                else if (op1 is int int1 && op2 is int int2)
                {
                    int result = ApplyForInt(int1, int2);
                    context.Stack.Push(result);
                }
                else
                {
                    throw new InvalidCastException("Operands must be bool/bool or int/int");
                }
            }

            protected abstract bool ApplyForBoolean(bool bool1, bool bool2);

            protected abstract int ApplyForInt(int int1, int int2);
        }

        /// <summary>
        /// Implements the "and" operator.
        /// </summary>
        internal sealed class And : AbstractLogicalOperator
        {
            protected override bool ApplyForBoolean(bool bool1, bool bool2)
            {
                return bool1 && bool2;
            }

            protected override int ApplyForInt(int int1, int int2)
            {
                return int1 & int2;
            }
        }

        /// <summary>
        /// Implements the "bitshift" operator.
        /// </summary>
        internal sealed class Bitshift : Operator
        {
            public void Execute(ExecutionContext context)
            {
                int shift = Convert.ToInt32(context.Stack.Pop());
                int int1 = Convert.ToInt32(context.Stack.Pop());
                if (shift < 0)
                {
                    int result = int1 >> Math.Abs(shift);
                    context.Stack.Push(result);
                }
                else
                {
                    int result = int1 << shift;
                    context.Stack.Push(result);
                }
            }
        }

        /// <summary>
        /// Implements the "false" operator.
        /// </summary>
        internal sealed class False : Operator
        {
            public void Execute(ExecutionContext context)
            {
                context.Stack.Push(false);
            }
        }

        /// <summary>
        /// Implements the "not" operator.
        /// </summary>
        internal sealed class Not : Operator
        {
            public void Execute(ExecutionContext context)
            {
                object op1 = context.Stack.Pop();
                if (op1 is bool bool1)
                {
                    bool result = !bool1;
                    context.Stack.Push(result);
                }
                else if (op1 is int int1)
                {
                    int result = -int1;
                    context.Stack.Push(result);
                }
                else
                {
                    throw new InvalidCastException("Operand must be bool or int");
                }
            }
        }

        /// <summary>
        /// Implements the "or" operator.
        /// </summary>
        internal sealed class Or : AbstractLogicalOperator
        {
            protected override bool ApplyForBoolean(bool bool1, bool bool2)
            {
                return bool1 || bool2;
            }

            protected override int ApplyForInt(int int1, int int2)
            {
                return int1 | int2;
            }
        }

        /// <summary>
        /// Implements the "true" operator.
        /// </summary>
        internal sealed class True : Operator
        {
            public void Execute(ExecutionContext context)
            {
                context.Stack.Push(true);
            }
        }

        /// <summary>
        /// Implements the "xor" operator.
        /// </summary>
        internal sealed class Xor : AbstractLogicalOperator
        {
            protected override bool ApplyForBoolean(bool bool1, bool bool2)
            {
                return bool1 ^ bool2;
            }

            protected override int ApplyForInt(int int1, int int2)
            {
                return int1 ^ int2;
            }
        }
    }
}
