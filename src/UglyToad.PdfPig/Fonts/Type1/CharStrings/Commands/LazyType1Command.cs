using System;

namespace UglyToad.PdfPig.Fonts.Type1.CharStrings.Commands
{
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
        public decimal PopTop()
        {
            throw new NotImplementedException();
        }

        public decimal PopBottom()
        {
            throw new NotImplementedException();
        }

        public void Push(decimal value)
        {

        }

        public void Clear()
        {

        }
    }
}
