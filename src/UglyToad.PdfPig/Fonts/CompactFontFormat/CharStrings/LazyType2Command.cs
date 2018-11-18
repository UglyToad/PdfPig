namespace UglyToad.PdfPig.Fonts.CompactFontFormat.CharStrings
{
    using System;
    using System.Diagnostics;
    using Util.JetBrains.Annotations;

    /// <summary>
    /// Represents the deferred execution of a Type 2 charstring command.
    /// </summary>
    internal class LazyType2Command
    {
        [NotNull]
        private readonly Action<Type2BuildCharContext> runCommand;

        /// <summary>
        /// The name of the command to run. See the Type 2 charstring specification for the possible command names.
        /// </summary>
        [NotNull]
        public string Name { get; }

        /// <summary>
        /// Create a new <see cref="LazyType2Command"/>.
        /// </summary>
        /// <param name="name">The name of the command.</param>
        /// <param name="runCommand">The action to execute when evaluating the command. This modifies the <see cref="Type2BuildCharContext"/>.</param>
        public LazyType2Command([NotNull] string name, [NotNull] Action<Type2BuildCharContext> runCommand)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            this.runCommand = runCommand ?? throw new ArgumentNullException(nameof(runCommand));
        }

        /// <summary>
        /// Evaluate the command.
        /// </summary>
        /// <param name="context">The current <see cref="Type2BuildCharContext"/>.</param>
        [DebuggerStepThrough]
        public void Run([NotNull] Type2BuildCharContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            runCommand(context);
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
