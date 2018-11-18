namespace UglyToad.PdfPig.Fonts.CompactFontFormat.CharStrings
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Util;

    /// <summary>
    /// Stores the decoded command sequences for Type 2 CharStrings from a Compact Font Format font as well
    /// as the local (per font) and global (per font set) subroutines.
    /// The CharStrings are lazily evaluated.
    /// </summary>
    internal class Type2CharStrings
    {
        private readonly object locker = new object();
        private readonly Dictionary<string, CharacterPath> glyphs = new Dictionary<string, CharacterPath>();

        public IReadOnlyDictionary<string, CommandSequence> CharStrings { get; }

        public IReadOnlyDictionary<int, CommandSequence> LocalSubroutines { get; }
        public IReadOnlyDictionary<int, CommandSequence> GlobalSubroutines { get; }

        public Type2CharStrings(IReadOnlyDictionary<string, CommandSequence> charStrings, IReadOnlyDictionary<int, CommandSequence> localSubroutines,
            IReadOnlyDictionary<int, CommandSequence> globalSubroutines)
        {
            CharStrings = charStrings ?? throw new ArgumentNullException(nameof(charStrings));
            LocalSubroutines = localSubroutines ?? throw new ArgumentNullException(nameof(localSubroutines));
            GlobalSubroutines = globalSubroutines ?? throw new ArgumentNullException(nameof(globalSubroutines));
        }

        /// <summary>
        /// Evaluate the CharString for the character with a given name returning the path constructed for the glyph.
        /// </summary>
        /// <param name="name">The name of the character to retrieve the CharString for.</param>
        /// <returns>A <see cref="CharacterPath"/> for the glyph.</returns>
        public CharacterPath Generate(string name)
        {
            CharacterPath glyph;
            lock (locker)
            {
                if (glyphs.TryGetValue(name, out var result))
                {
                    return result;
                }

                if (!CharStrings.TryGetValue(name, out var sequence))
                {
                    throw new InvalidOperationException($"No charstring sequence with the name /{name} in this font.");
                }

                glyph = Run(sequence);

                glyphs[name] = glyph;
            }

            return glyph;
        }

        private CharacterPath Run(CommandSequence sequence)
        {
            var context = new Type2BuildCharContext(LocalSubroutines, GlobalSubroutines);

            var hasRunStackClearingCommand = false;
            foreach (var command in sequence.Commands)
            {
                command.Match(x => context.Stack.Push(x),
                   x =>
                   {
                       if (!hasRunStackClearingCommand)
                       {
                           /*
                            * The first stack-clearing operator, which must be one of hstem, hstemhm, vstem, vstemhm, cntrmask, hintmask, hmoveto, vmoveto,
                            * rmoveto, or endchar, takes an additional argument — the width (as described earlier), which may be expressed as zero or one numeric argument.
                            */
                           hasRunStackClearingCommand = true;
                           switch (x.Name)
                           {
                               case "hstem":
                               case "hstemhm":
                               case "vstemhm":
                               case "vstem":
                               {
                                   var oddArgCount = context.Stack.Length % 2 != 0;
                                   if (oddArgCount)
                                   {
                                       context.Width = context.Stack.PopBottom();
                                   }
                                   break;
                               }
                               case "hmoveto":
                               case "vmoveto":
                                   SetWidthFromArgumentsIfPresent(context, 1);
                                   break;
                               case "rmoveto":
                                   SetWidthFromArgumentsIfPresent(context, 2);
                                   break;
                               case "cntrmask":
                               case "hintmask":
                               case "endchar:":
                                   SetWidthFromArgumentsIfPresent(context, 0);
                                    break;
                                default:
                                    hasRunStackClearingCommand = false;
                                    break;
                                
                           }

                       }
                       x.Run(context);
                   });
            }

            return context.Path;
        }

        private static void SetWidthFromArgumentsIfPresent(Type2BuildCharContext context, int expectedArgumentLength)
        {
            if (context.Stack.Length > expectedArgumentLength)
            {
                context.Width = context.Stack.PopBottom();
            }
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
