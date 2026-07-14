namespace UglyToad.PdfPig.Functions.Type4
{
    internal enum OpCode : byte
    {
        /// <summary>
        /// Push <see cref="Instruction.Immediate"/> onto the operand stack.
        /// </summary>
        Push,

        // Arithmetic operators
        Abs, Add, Atan, Ceiling, Cos, Cvi, Cvr, Div, Exp, Floor, IDiv, Ln, Log, Mod, Mul, Neg, Round, Sin, Sqrt, Sub, Truncate,

        // Relational, boolean and bitwise operators
        And, Bitshift, Eq, False, Ge, Gt, Le, Lt, Ne, Not, Or, True, Xor,

        // Conditional operators
        If, IfElse,

        // Stack operators
        Copy, Dup, Exch, Index, Pop, Roll,

        /// <summary>
        /// A name that did not resolve to an operator; throws when executed.
        /// </summary>
        Unknown
    }
}
