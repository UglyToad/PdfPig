using System;

namespace UglyToad.PdfPig.Fonts.Type1.CharStrings.Commands
{
    using System.Collections.Generic;

    /// <summary>
    /// Represents the deferred execution of a Type 1 Build Char command.
    /// </summary>
    internal class LazyType1Command
    {
        private readonly Action<Type1BuildCharContext> runCommand;

        public string Name { get; }

        public LazyType1Command(string name, Action<Type1BuildCharContext> runCommand)
        {
            Name = name;
            this.runCommand = runCommand ?? throw new ArgumentNullException(nameof(runCommand));
        }

        public void Run(Type1BuildCharContext context)
        {
            runCommand(context);
        }

        public override string ToString()
        {
            return Name;
        }
    }

    internal class Type1Stack
    {
        private readonly List<decimal> stack = new List<decimal>();

        public decimal PopTop()
        {
            var result = stack[stack.Count - 1];
            stack.RemoveAt(stack.Count - 1);
            return result;
        }

        public decimal PopBottom()
        {
            var result = stack[0];
            stack.RemoveAt(0);
            return result;
        }

        public void Push(decimal value)
        {
            stack.Add(value);
        }

        public void Clear()
        {
            stack.Clear();
        }
    }
}
