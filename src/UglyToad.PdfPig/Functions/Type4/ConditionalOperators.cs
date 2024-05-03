namespace UglyToad.PdfPig.Functions.Type4
{
    /// <summary>
    /// Provides the conditional operators such as "if" and "ifelse".
    /// </summary>
    internal sealed class ConditionalOperators
    {
        private ConditionalOperators()
        {
            // Private constructor.
        }

        /// <summary>
        /// Implements the "if" operator.
        /// </summary>
        internal sealed class If : Operator
        {
            public void Execute(ExecutionContext context)
            {
                InstructionSequence proc = (InstructionSequence)context.Stack.Pop();
                bool condition = (bool)context.Stack.Pop();
                if (condition)
                {
                    proc.Execute(context);
                }
            }
        }

        /// <summary>
        /// Implements the "ifelse" operator.
        /// </summary>
        internal sealed class IfElse : Operator
        {
            public void Execute(ExecutionContext context)
            {
                InstructionSequence proc2 = (InstructionSequence)context.Stack.Pop();
                InstructionSequence proc1 = (InstructionSequence)context.Stack.Pop();
                bool condition = Convert.ToBoolean(context.Stack.Pop());
                if (condition)
                {
                    proc1.Execute(context);
                }
                else
                {
                    proc2.Execute(context);
                }
            }
        }
    }
}
