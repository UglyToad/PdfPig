namespace UglyToad.PdfPig.Fonts.Type1.CharStrings
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Commands;
    using Util;

    internal class Type1CharStrings
    {
        private readonly object locker = new object();
        private readonly Dictionary<string, CommandSequence> charStrings = new Dictionary<string, CommandSequence>();

        public IReadOnlyDictionary<string, CommandSequence> CharStrings { get; }

        public IReadOnlyDictionary<int, CommandSequence> Subroutines { get; }
        
        public Type1CharStrings(IReadOnlyDictionary<string, CommandSequence> charStrings, IReadOnlyDictionary<int, CommandSequence> subroutines)
        {
            CharStrings = charStrings ?? throw new ArgumentNullException(nameof(charStrings));
            Subroutines = subroutines ?? throw new ArgumentNullException(nameof(subroutines));
        }

        public void Generate(string name)
        {
            lock (locker)
            {
                if (charStrings.TryGetValue(name, out var result))
                {
                    return;
                }
            }

            if (!CharStrings.TryGetValue(name, out var sequence))
            {
                throw new InvalidOperationException($"No charstring sequence with the name /{name} in this font.");
            }

            Run(sequence);

            lock (locker)
            {
                charStrings[name] = sequence;
            }
        }

        private void Run(CommandSequence sequence)
        {

        }

        public class CommandSequence
        {
            /// <summary>
            /// The ordered list of numbers and commands for a Type 1 charstring or subroutine.
            /// </summary>
            public IReadOnlyList<DiscriminatedUnion<decimal, LazyType1Command>> Commands { get; }

            public CommandSequence(IReadOnlyList<DiscriminatedUnion<decimal, LazyType1Command>> commands)
            {
                Commands = commands ?? throw new ArgumentNullException(nameof(commands));
            }

            public override string ToString()
            {
                return string.Join(", ", Commands.Select(x => x.ToString()));
            }
        }
    }
}
