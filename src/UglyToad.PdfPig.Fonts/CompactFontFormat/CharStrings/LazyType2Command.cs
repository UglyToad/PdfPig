namespace UglyToad.PdfPig.Fonts.CompactFontFormat.CharStrings
{
    using System;
    using System.Diagnostics;

    /// <summary>
    /// Represents the deferred execution of a Type 2 charstring command.
    /// </summary>
    internal class LazyType2Command
    {
        private readonly int minimumStackParameters;
        private readonly Action<Type2BuildCharContext> runCommand;

        /// <summary>
        /// The name of the command to run. See the Type 2 charstring specification for the possible command names.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Create a new <see cref="LazyType2Command"/>.
        /// </summary>
        /// <param name="name">The name of the command.</param>
        /// <param name="minimumStackParameters">Minimum number of argument which must be on the stack or -1 if no checking</param>
        /// <param name="runCommand">The action to execute when evaluating the command. This modifies the <see cref="Type2BuildCharContext"/>.</param>
        public LazyType2Command(string name, int minimumStackParameters, Action<Type2BuildCharContext> runCommand)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            this.minimumStackParameters = minimumStackParameters;
            this.runCommand = runCommand ?? throw new ArgumentNullException(nameof(runCommand));
        }

        /// <summary>
        /// Evaluate the command.
        /// </summary>
        /// <param name="context">The current <see cref="Type2BuildCharContext"/>.</param>
        [DebuggerStepThrough]
        public void Run(Type2BuildCharContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (context.Stack.Length < minimumStackParameters)
            {
                Debug.WriteLine($"Warning: CFF CharString command '{Name}' expected {minimumStackParameters} arguments. Got: {context.Stack.Length}. Command ignored and stack cleared.");
                context.Stack.Clear();
                return;
            }

            runCommand(context);
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
