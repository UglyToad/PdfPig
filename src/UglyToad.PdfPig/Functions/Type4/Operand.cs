namespace UglyToad.PdfPig.Functions.Type4
{
    using System;
    using System.Globalization;

    internal enum OperandKind : byte
    {
        Integer = 0,
        Real = 1,
        Boolean = 2,
        Procedure = 3
    }

    /// <summary>
    /// A single PostScript calculator value: an integer, real, boolean or procedure reference.
    /// The payload is stored as a double (exact for all int32 values); procedures are stored as
    /// an index into the program's procedure table. The struct is unmanaged so the operand stack
    /// can be stack-allocated.
    /// </summary>
    internal readonly struct Operand
    {
        public readonly double Value;
        public readonly OperandKind Kind;

        private Operand(double value, OperandKind kind)
        {
            Value = value;
            Kind = kind;
        }

        public static Operand Integer(int value) => new Operand(value, OperandKind.Integer);

        public static Operand Real(double value) => new Operand(value, OperandKind.Real);

        public static Operand Boolean(bool value) => new Operand(value ? 1 : 0, OperandKind.Boolean);

        public static Operand Procedure(int procedureIndex) => new Operand(procedureIndex, OperandKind.Procedure);

        public bool IsNumber => Kind == OperandKind.Integer || Kind == OperandKind.Real;

        public int AsInteger => (int)Value;

        public bool AsBoolean => Value != 0;

        public int AsProcedureIndex => AsInteger;

        /// <summary>
        /// The numeric value as a double. Throws for booleans and procedures.
        /// </summary>
        public double ToReal()
        {
            return IsNumber ? Value : throw new InvalidCastException("The object popped is neither an integer or a real.");
        }

        public override string ToString()
        {
            return Kind switch
            {
                OperandKind.Integer => AsInteger.ToString(CultureInfo.InvariantCulture),
                OperandKind.Real => Value.ToString(CultureInfo.InvariantCulture),
                OperandKind.Boolean => AsBoolean ? "true" : "false",
                _ => "proc#" + AsProcedureIndex
            };
        }
    }
}
