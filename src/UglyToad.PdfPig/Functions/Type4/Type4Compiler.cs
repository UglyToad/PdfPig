namespace UglyToad.PdfPig.Functions.Type4
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;

#if NET8_0_OR_GREATER
    using System.Collections.Frozen;
#endif

    /// <summary>
    /// Compiles Type 4 function text into a <see cref="Type4Program"/>. Operator names are
    /// resolved to opcodes once, at parse time, so execution never performs string lookups.
    /// </summary>
    internal sealed class Type4Compiler : Parser.AbstractSyntaxHandler
    {
#if NET8_0_OR_GREATER
        private static readonly FrozenDictionary<string, OpCode>
#else
        private static readonly Dictionary<string, OpCode>
#endif
            OperatorNames = new Dictionary<string, OpCode>(StringComparer.Ordinal)
        {
            ["abs"] = OpCode.Abs,
            ["add"] = OpCode.Add,
            ["atan"] = OpCode.Atan,
            ["ceiling"] = OpCode.Ceiling,
            ["cos"] = OpCode.Cos,
            ["cvi"] = OpCode.Cvi,
            ["cvr"] = OpCode.Cvr,
            ["div"] = OpCode.Div,
            ["exp"] = OpCode.Exp,
            ["floor"] = OpCode.Floor,
            ["idiv"] = OpCode.IDiv,
            ["ln"] = OpCode.Ln,
            ["log"] = OpCode.Log,
            ["mod"] = OpCode.Mod,
            ["mul"] = OpCode.Mul,
            ["neg"] = OpCode.Neg,
            ["round"] = OpCode.Round,
            ["sin"] = OpCode.Sin,
            ["sqrt"] = OpCode.Sqrt,
            ["sub"] = OpCode.Sub,
            ["truncate"] = OpCode.Truncate,
            ["and"] = OpCode.And,
            ["bitshift"] = OpCode.Bitshift,
            ["eq"] = OpCode.Eq,
            ["false"] = OpCode.False,
            ["ge"] = OpCode.Ge,
            ["gt"] = OpCode.Gt,
            ["le"] = OpCode.Le,
            ["lt"] = OpCode.Lt,
            ["ne"] = OpCode.Ne,
            ["not"] = OpCode.Not,
            ["or"] = OpCode.Or,
            ["true"] = OpCode.True,
            ["xor"] = OpCode.Xor,
            ["if"] = OpCode.If,
            ["ifelse"] = OpCode.IfElse,
            ["copy"] = OpCode.Copy,
            ["dup"] = OpCode.Dup,
            ["exch"] = OpCode.Exch,
            ["index"] = OpCode.Index,
            ["pop"] = OpCode.Pop,
            ["roll"] = OpCode.Roll
        }
#if NET8_0_OR_GREATER
            .ToFrozenDictionary()
#endif
            ;

        private readonly List<List<Instruction>> procedures = new List<List<Instruction>> { new List<Instruction>() };
        private readonly Stack<int> procedureStack = new Stack<int>();

        private Type4Compiler()
        {
            procedureStack.Push(0);
        }

        /// <summary>
        /// Parses the given text into an executable Type 4 program.
        /// </summary>
        public static Type4Program Parse(string text)
        {
            var compiler = new Type4Compiler();
            Parser.Parse(text, compiler);
            var compiled = new Instruction[compiler.procedures.Count][];
            for (int i = 0; i < compiled.Length; i++)
            {
                compiled[i] = compiler.procedures[i].ToArray();
            }
            return new Type4Program(compiled);
        }

        private List<Instruction> CurrentProcedure => procedures[procedureStack.Peek()];

        public override void Token(string token)
        {
            if ("{".Equals(token))
            {
                int childIndex = procedures.Count;
                procedures.Add(new List<Instruction>());
                CurrentProcedure.Add(new Instruction(OpCode.Push, Operand.Procedure(childIndex)));
                procedureStack.Push(childIndex);
            }
            else if ("}".Equals(token))
            {
                procedureStack.Pop();
            }
            else if (int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out int tokenInt))
            {
                CurrentProcedure.Add(new Instruction(OpCode.Push, Operand.Integer(tokenInt)));
            }
            else if (double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out double tokenReal))
            {
                CurrentProcedure.Add(new Instruction(OpCode.Push, Operand.Real(tokenReal)));
            }
            else if (OperatorNames.TryGetValue(token, out OpCode op))
            {
                CurrentProcedure.Add(new Instruction(op));
            }
            else
            {
                // Unknown names are only an error if executed (they may sit in a branch
                // that is never taken)
                CurrentProcedure.Add(Instruction.Unknown(token));
            }
        }
    }
}
