namespace UglyToad.PdfPig.Fonts.Type1.CharStrings
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Commands;
    using Util;

    internal class Type1CharStrings
    {
        public IReadOnlyDictionary<string, CommandSequence> CharStrings { get; }

        public IReadOnlyDictionary<int, CommandSequence> Subroutines { get; }

        public Type1CharStrings(IReadOnlyDictionary<string, CommandSequence> charStrings, IReadOnlyDictionary<int, CommandSequence> subroutines)
        {
            CharStrings = charStrings ?? throw new ArgumentNullException(nameof(charStrings));
            Subroutines = subroutines ?? throw new ArgumentNullException(nameof(subroutines));
        }

        public class CommandSequence
        {
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
