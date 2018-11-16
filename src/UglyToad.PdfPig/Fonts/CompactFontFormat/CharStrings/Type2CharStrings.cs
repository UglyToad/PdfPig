namespace UglyToad.PdfPig.Fonts.CompactFontFormat.CharStrings
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Util;

    internal class Type2CharStrings
    {
        private readonly object locker = new object();
        private readonly Dictionary<string, CharacterPath> glyphs = new Dictionary<string, CharacterPath>();

        public IReadOnlyDictionary<int, CommandSequence> CharStrings { get; }

        public IReadOnlyDictionary<int, CommandSequence> Subroutines { get; }

        public Type2CharStrings(IReadOnlyDictionary<int, CommandSequence> charStrings, IReadOnlyDictionary<int, CommandSequence> subroutines)
        {
            CharStrings = charStrings ?? throw new ArgumentNullException(nameof(charStrings));
            Subroutines = subroutines ?? throw new ArgumentNullException(nameof(subroutines));
        }

        public CharacterPath Generate(string name)
        {
            CharacterPath glyph;
            lock (locker)
            {
                if (glyphs.TryGetValue(name, out var result))
                {
                    return result;
                }

                if (!CharStrings.TryGetValue(0, out var sequence))
                {
                    throw new InvalidOperationException($"No charstring sequence with the name /{name} in this font.");
                }

                glyph = Run(sequence);

                glyphs[name] = glyph;
            }

            return glyph;
        }

        private static CharacterPath Run(CommandSequence sequence)
        {
            var context = new Type2BuildCharContext();
            foreach (var command in sequence.Commands)
            {
                //command.Match(x => context.Stack.Push(x),
                //    x => x.Run(context));
            }

            throw new NotImplementedException();
        }

        public class CommandSequence
        {
            /// <summary>
            /// The ordered list of numbers and commands for a Type 2 charstring or subroutine.
            /// </summary>
            public IReadOnlyList<Union<decimal, LazyType2Command>> Commands { get; }

            public CommandSequence(IReadOnlyList<Union<decimal, LazyType2Command>> commands)
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
