namespace UglyToad.PdfPig.Tests.Functions.Type4
{
    using UglyToad.PdfPig.Functions.Type4;

    public class Type4ProgramTests
    {
        private static Operand[] Run(string text)
        {
            Type4Program program = Type4Compiler.Parse(text.Trim());
            var stack = new OperandStack(new Operand[100]);
            program.Execute(ref stack);
            return stack.ToArray();
        }

        [Fact]
        public void AddIntegers()
        {
            Operand[] result = Run("5 6 add");
            Assert.Single(result);
            Assert.Equal(OperandKind.Integer, result[0].Kind);
            Assert.Equal(11, result[0].AsInteger);
        }

        [Fact]
        public void AddIntegerOverflowPromotesToReal()
        {
            int big = int.MaxValue - 2;
            Operand[] result = Run($"{big} {big} add");
            Assert.Single(result);
            Assert.Equal(OperandKind.Real, result[0].Kind);
            Assert.Equal(2.0 * big, result[0].Value);
        }

        [Fact]
        public void DivAlwaysReal()
        {
            Operand[] result = Run("4 2 div");
            Assert.Single(result);
            Assert.Equal(OperandKind.Real, result[0].Kind);
            Assert.Equal(2.0, result[0].Value);
        }

        [Fact]
        public void MixedAddIsReal()
        {
            Operand[] result = Run("5 0.23 add");
            Assert.Single(result);
            Assert.Equal(OperandKind.Real, result[0].Kind);
            Assert.Equal(5.23, result[0].Value, 10);
        }

        [Fact]
        public void UnknownNameThrowsAtExecutionTime()
        {
            // Parsing must succeed; execution must fail.
            Type4Program program = Type4Compiler.Parse("1 frobnicate");
            Assert.Throws<InvalidOperationException>(() =>
            {
                var stack = new OperandStack(new Operand[10]);
                program.Execute(ref stack);
            });
        }

        [Fact]
        public void TopLevelProcedureIsExecuted()
        {
            // A proc left on the stack at the end of the sequence is executed
            // (mirrors the previous InstructionSequence.Execute tail loop).
            Operand[] result = Run("{ 5 6 add }");
            Assert.Single(result);
            Assert.Equal(11, result[0].AsInteger);
        }

        [Fact]
        public void LogicalOperatorsOnBoolsAndInts()
        {
            Operand[] result = Run("true false and 99 1 and");
            Assert.Equal(2, result.Length);
            Assert.Equal(OperandKind.Boolean, result[0].Kind);
            Assert.False(result[0].AsBoolean);
            Assert.Equal(OperandKind.Integer, result[1].Kind);
            Assert.Equal(1, result[1].AsInteger);
        }

        [Fact]
        public void EqDistinguishesIntFromReal()
        {
            // Previous boxed behaviour: int 1 does not equal real 1.0.
            Operand[] result = Run("1 1.0 eq 1 1 eq 1.0 1.0 eq");
            Assert.False(result[0].AsBoolean);
            Assert.True(result[1].AsBoolean);
            Assert.True(result[2].AsBoolean);
        }

        [Fact]
        public void IfExecutesWhenTrueAndRequiresStrictBoolean()
        {
            Operand[] result = Run("true { 2 1 add } if");
            Assert.Single(result);
            Assert.Equal(3, result[0].AsInteger);

            result = Run("false { 2 1 add } if");
            Assert.Empty(result);

            // An int condition throws InvalidCastException (strict bool, as before).
            Assert.Throws<InvalidCastException>(() =>
            {
                var stack = new OperandStack(new Operand[10]);
                Type4Compiler.Parse("0 { 2 1 add } if").Execute(ref stack);
            });
        }

        [Fact]
        public void IfElseTakesTheRightBranch()
        {
            Operand[] result = Run("0.4 0.5 le { 10 } { 20 } ifelse");
            Assert.Single(result);
            Assert.Equal(10, result[0].AsInteger);

            result = Run("0.6 0.5 le { 10 } { 20 } ifelse");
            Assert.Single(result);
            Assert.Equal(20, result[0].AsInteger);
        }

        [Fact]
        public void BitshiftShiftsBothDirections()
        {
            Operand[] result = Run("1 3 bitshift 16 -2 bitshift");
            Assert.Equal(8, result[0].AsInteger);
            Assert.Equal(4, result[1].AsInteger);
        }

        [Fact]
        public void StackOperators()
        {
            // dup / pop / exch
            Operand[] result = Run("5 dup 6 pop 7 exch");
            Assert.Equal(new[] { 5, 7, 5 }, System.Array.ConvertAll(result, o => o.AsInteger));

            // copy: 1 2 3 2 copy -> 1 2 3 2 3
            result = Run("1 2 3 2 copy");
            Assert.Equal(new[] { 1, 2, 3, 2, 3 }, System.Array.ConvertAll(result, o => o.AsInteger));

            // index: 1 2 3 2 index -> 1 2 3 1
            result = Run("1 2 3 2 index");
            Assert.Equal(new[] { 1, 2, 3, 1 }, System.Array.ConvertAll(result, o => o.AsInteger));

            // roll: 1 2 3 3 1 roll -> 3 1 2
            result = Run("1 2 3 3 1 roll");
            Assert.Equal(new[] { 3, 1, 2 }, System.Array.ConvertAll(result, o => o.AsInteger));

            // negative roll: 1 2 3 3 -1 roll -> 2 3 1
            result = Run("1 2 3 3 -1 roll");
            Assert.Equal(new[] { 2, 3, 1 }, System.Array.ConvertAll(result, o => o.AsInteger));
        }

        [Fact]
        public void IndexRangecheckThrows()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                var stack = new OperandStack(new Operand[10]);
                Type4Compiler.Parse("1 2 -1 index").Execute(ref stack);
            });
        }
    }
}
