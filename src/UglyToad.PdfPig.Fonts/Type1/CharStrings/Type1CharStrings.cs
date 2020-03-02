namespace UglyToad.PdfPig.Fonts.Type1.CharStrings
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Commands;
    using Core;

    internal class Type1CharStrings
    {
        private readonly IReadOnlyDictionary<int, string> charStringIndexToName;
        private readonly object locker = new object();
        private readonly Dictionary<string, PdfPath> glyphs = new Dictionary<string, PdfPath>();

        public IReadOnlyDictionary<string, CommandSequence> CharStrings { get; }

        public IReadOnlyDictionary<int, CommandSequence> Subroutines { get; }

        public Type1CharStrings(IReadOnlyDictionary<string, CommandSequence> charStrings, IReadOnlyDictionary<int, string> charStringIndexToName,
            IReadOnlyDictionary<int, CommandSequence> subroutines)
        {
            this.charStringIndexToName = charStringIndexToName ?? throw new ArgumentNullException(nameof(charStringIndexToName));
            CharStrings = charStrings ?? throw new ArgumentNullException(nameof(charStrings));
            Subroutines = subroutines ?? throw new ArgumentNullException(nameof(subroutines));
        }

        public bool TryGenerate(string name, out PdfPath path)
        {
            path = default(PdfPath);
            lock (locker)
            {
                if (glyphs.TryGetValue(name, out path))
                {
                    return true;
                }

                if (!CharStrings.TryGetValue(name, out var sequence))
                {
                    return false;
                }

                path = Run(sequence);

                glyphs[name] = path;
            }

            return true;
        }

        private PdfPath Run(CommandSequence sequence)
        {
            var context = new Type1BuildCharContext(Subroutines, i =>
            {
                if (!charStringIndexToName.TryGetValue(i, out var name))
                {
                    throw new InvalidOperationException($"Tried to retrieve Type 1 charstring by index {i} which did not exist.");
                }

                if (glyphs.TryGetValue(name, out var result))
                {
                    return result;
                }

                if (!CharStrings.TryGetValue(name, out var charstring))
                {
                    throw new InvalidOperationException($"Tried to retrieve Type 1 charstring by index {i} which mapped to name {name} but was not found in the charstrings.");
                }

                var path = Run(charstring);

                glyphs[name] = path;

                return path;
            }, s =>
            {
                if (glyphs.TryGetValue(s, out var result))
                {
                    return result;
                }

                if (!CharStrings.TryGetValue(s, out var charstring))
                {
                    throw new InvalidOperationException($"Tried to retrieve Type 1 charstring by name {s} but it was not found in the charstrings.");
                }

                var path = Run(charstring);

                glyphs[s] = path;

                return path;
            });

            foreach (var command in sequence.Commands)
            {
                if (command.TryGetFirst(out var num))
                {
                    context.Stack.Push(num);
                }
                else if (command.TryGetSecond(out var lazyCommand))
                {
                    lazyCommand.Run(context);
                }
            }

            return context.Path;
        }

        public class CommandSequence
        {
            /// <summary>
            /// The ordered list of numbers and commands for a Type 1 charstring or subroutine.
            /// </summary>
            public IReadOnlyList<Union<double, LazyType1Command>> Commands { get; }

            public CommandSequence(IReadOnlyList<Union<double, LazyType1Command>> commands)
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
