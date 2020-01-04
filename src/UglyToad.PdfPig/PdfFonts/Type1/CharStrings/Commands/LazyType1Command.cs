namespace UglyToad.PdfPig.PdfFonts.Type1.CharStrings.Commands
{
    using System;
    using System.Diagnostics;

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

        [DebuggerStepThrough]
        public void Run(Type1BuildCharContext context)
        {
            runCommand(context);
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
