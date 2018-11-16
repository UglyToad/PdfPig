namespace UglyToad.PdfPig.Fonts.CompactFontFormat.CharStrings
{
    using System.Collections.Generic;
    using Util;

    /// <summary>
    /// </summary>
    /// <remarks>
    /// A Type 2 charstring program is a sequence of unsigned 8-bit bytes that encode numbers and operators.
    /// The byte value specifies a operator, a number, or subsequent bytes that are to be interpreted in a specific manner
    /// </remarks>
    internal class Type2CharStringParser
    {
        public static void Parse(IReadOnlyList<IReadOnlyList<byte>> charStringBytes)
        {
            for (var i = 0; i < charStringBytes.Count; i++)
            {
                var charString = charStringBytes[i];
                ParseSingle(charString);
            }
        }

        private static IReadOnlyList<Union<decimal, LazyType2Command>> ParseSingle(IReadOnlyList<byte> bytes)
        {
            var instructions = new List<Union<decimal, LazyType2Command>>();
            for (var i = 0; i < bytes.Count; i++)
            {
                var b = bytes[i];
                if (b <= 31 && b != 28)
                {
                    var command = GetCommand(b, bytes, ref i);
                    instructions.Add(Union<decimal, LazyType2Command>.Two(command));
                }
                else
                {
                    var number = InterpretNumber(b, bytes, ref i);
                    instructions.Add(Union<decimal, LazyType2Command>.One(number));
                }
            }

            return instructions;
        }

        /// <summary>
        /// The Type 2 interpretation of a number with an initial byte value of 255 differs from how it is interpreted in the Type 1 format
        /// and 28 has a special meaning.
        /// </summary>
        private static decimal InterpretNumber(byte b, IReadOnlyList<byte> bytes, ref int i)
        {
            if (b == 28)
            {
                return bytes[++i] << 8 | bytes[++i];
            }

            if (b >= 32 && b <= 246)
            {
                return b - 139;
            }

            if (b >= 247 && b <= 250)
            {
                var w = bytes[++i];
                return ((b - 247) * 256) + w + 108;
            }

            if (b >= 251 && b <= 254)
            {
                var w = bytes[++i];
                return -((b - 251) * 256) - w - 108;
            }

            /*
             * If the charstring byte contains the value 255, the next four bytes indicate a two's complement signed number.
             * The first of these the four bytes contains the highest order bits, the second byte contains the next higher order bits
             * and the fourth byte contains the lowest order bits.
             * This number is interpreted as a Fixed; that is, a signed number with 16 bits of fraction
             */
            var lead = bytes[++i] << 8 | bytes[++i];
            var fractionalPart = bytes[++i] << 8 | bytes[++i];

            return lead + (fractionalPart / 65535m);
        }

        private static readonly IReadOnlyDictionary<int, LazyType2Command> SingleByteCommandStore = new Dictionary<int, LazyType2Command>
        {
            { 1,  new LazyType2Command("hstem", x => { })},
            { 3,  new LazyType2Command("vstem", x => { })},
            { 4,  new LazyType2Command("vmoveto", x => { })},
            { 5,  new LazyType2Command("rlineto", x => { })},
            { 6,  new LazyType2Command("hlineto", x => { })},
            { 7,  new LazyType2Command("vlineto", x => { })},
            { 8,  new LazyType2Command("rrcurveto", x => { })},
            { 10,  new LazyType2Command("callsubr", x => { })},
            { 11,  new LazyType2Command("return", x => { })},
            { 14,  new LazyType2Command("endchar", x => { })},
            { 18,  new LazyType2Command("hstemhm", x => { })},
            { 19,  new LazyType2Command("hintmask", x => { })},
            { 20,  new LazyType2Command("cntrmask", x => { })},
            { 21,  new LazyType2Command("rmoveto", x => { })},
            { 22,  new LazyType2Command("hmoveto", x => { })},
            { 23,  new LazyType2Command("vstemhm", x => { })},
            { 24,  new LazyType2Command("rcurveline", x => { })},
            { 25,  new LazyType2Command("rlinecurve", x => { })},
            { 26,  new LazyType2Command("vvcurveto", x => { })},
            { 27,  new LazyType2Command("hhcurveto", x => { })},
            { 29,  new LazyType2Command("callgsubr", x => { })},
            { 30,  new LazyType2Command("vhcurveto", x => { })},
            { 31,  new LazyType2Command("hvcurveto", x => { })}
        };

        private static readonly IReadOnlyDictionary<int, LazyType2Command> TwoByteCommandStore = new Dictionary<int, LazyType2Command>
        {
            { 3,  new LazyType2Command("and", x => { })},
            { 4,  new LazyType2Command("or", x => { })},
            { 5,  new LazyType2Command("not", x => { })},
            { 9,  new LazyType2Command("abs", x => { })},
            { 10,  new LazyType2Command("add", x => { })},
            { 11,  new LazyType2Command("sub", x => { })},
            { 12,  new LazyType2Command("div", x => { })},
            { 14,  new LazyType2Command("neg", x => { })},
            { 15,  new LazyType2Command("eq", x => { })},
            { 18,  new LazyType2Command("drop", x => { })},
            { 20,  new LazyType2Command("put", x => { })},
            { 21,  new LazyType2Command("get", x => { })},
            { 22,  new LazyType2Command("ifelse", x => { })},
            { 23,  new LazyType2Command("random", x => { })},
            { 24,  new LazyType2Command("mul", x => { })},
            { 26,  new LazyType2Command("sqrt", x => { })},
            { 27,  new LazyType2Command("dup", x => { })},
            { 28,  new LazyType2Command("exch", x => { })},
            { 29,  new LazyType2Command("index", x => { })},
            { 30,  new LazyType2Command("roll", x => { })},
            { 34,  new LazyType2Command("hflex", x => { })},
            { 35,  new LazyType2Command("flex", x => { })},
            { 36,  new LazyType2Command("hflex1", x => { })},
            { 37,  new LazyType2Command("flex1", x => { })},
        };

        private static LazyType2Command GetCommand(byte b, IReadOnlyList<byte> bytes, ref int i)
        {
            if (b == 12)
            {
                var b2 = bytes[++i];
                if (TwoByteCommandStore.TryGetValue(b2, out var commandTwoByte))
                {
                    return commandTwoByte;
                }

                return new LazyType2Command($"unknown: {b} {b2}", x => {});
            }

            if (SingleByteCommandStore.TryGetValue(b, out var command))
            {
                return command;
            }

            return new LazyType2Command($"unknown: {b}", x => {});
        }
    }
}
