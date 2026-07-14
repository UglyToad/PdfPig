namespace UglyToad.PdfPig.Functions.Type4
{
    internal readonly struct Instruction
    {
        public readonly OpCode Op;

        /// <summary>
        /// Only meaningful for <see cref="OpCode.Push"/>.
        /// </summary>
        public readonly Operand Immediate;

        /// <summary>
        /// Only set for <see cref="OpCode.Unknown"/>.
        /// </summary>
        public readonly string? Name;

        public Instruction(OpCode op)
        {
            Op = op;
            Immediate = default;
            Name = null;
        }

        public Instruction(OpCode op, Operand immediate)
        {
            Op = op;
            Immediate = immediate;
            Name = null;
        }

        private Instruction(string name)
        {
            Op = OpCode.Unknown;
            Immediate = default;
            Name = name;
        }

        public static Instruction Unknown(string name) => new Instruction(name);
    }
}
