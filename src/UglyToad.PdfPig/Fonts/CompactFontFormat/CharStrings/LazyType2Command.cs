namespace UglyToad.PdfPig.Fonts.CompactFontFormat.CharStrings
{
    using System;
    using System.Diagnostics;

    /// <summary>
    /// Represents the deferred execution of a Type 2 CharString command.
    /// </summary>
    internal class LazyType2Command
    {
        private readonly Action<Type2BuildCharContext> runCommand;

        public string Name { get; }

        public LazyType2Command(string name, Action<Type2BuildCharContext> runCommand)
        {
            Name = name;
            this.runCommand = runCommand ?? throw new ArgumentNullException(nameof(runCommand));
        }

        [DebuggerStepThrough]
        public void Run(Type2BuildCharContext context)
        {
            runCommand(context);
        }

        public override string ToString()
        {
            return Name;
        }
    }

    internal class Type2BuildCharContext
    {

    }
}
