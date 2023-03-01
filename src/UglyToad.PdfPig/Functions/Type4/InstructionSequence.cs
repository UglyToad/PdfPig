namespace UglyToad.PdfPig.Functions.Type4
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    internal sealed class InstructionSequence
    {
        private readonly List<object> instructions = new List<object>();

        /// <summary>
        /// Add a name (ex. an operator)
        /// </summary>
        /// <param name="name">the name</param>
        public void AddName(string name)
        {
            this.instructions.Add(name);
        }

        /// <summary>
        /// Adds an int value.
        /// </summary>
        /// <param name="value">the value</param>
        public void AddInteger(int value)
        {
            this.instructions.Add(value);
        }

        /// <summary>
        /// Adds a real value.
        /// </summary>
        /// <param name="value">the value</param>
        public void AddReal(double value)
        {
            this.instructions.Add(value);
        }

        /// <summary>
        /// Adds a bool value.
        /// </summary>
        /// <param name="value">the value</param>
        public void AddBoolean(bool value)
        {
            this.instructions.Add(value);
        }

        /// <summary>
        /// Adds a proc (sub-sequence of instructions).
        /// </summary>
        /// <param name="child">the child proc</param>
        public void AddProc(InstructionSequence child)
        {
            this.instructions.Add(child);
        }

        /// <summary>
        /// Executes the instruction sequence.
        /// </summary>
        /// <param name="context">the execution context</param>
        public void Execute(ExecutionContext context)
        {
            foreach (object o in instructions)
            {
                if (o is string name)
                {
                    Operator cmd = context.GetOperators().GetOperator(name);
                    if (cmd != null)
                    {
                        cmd.Execute(context);
                    }
                    else
                    {
                        throw new InvalidOperationException("Unknown operator or name: " + name);
                    }
                }
                else
                {
                    context.Stack.Push(o);
                }
            }

            //Handles top-level procs that simply need to be executed
            while (context.Stack.Any() && context.Stack.Peek() is InstructionSequence)
            {
                InstructionSequence nested = (InstructionSequence)context.Stack.Pop();
                nested.Execute(context);
            }
        }
    }
}
