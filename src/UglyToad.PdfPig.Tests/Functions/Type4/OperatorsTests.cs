namespace UglyToad.PdfPig.Tests.Functions.Type4
{
    using System;
    using UglyToad.PdfPig.Functions.Type4;
    using Xunit;

    public class OperatorsTests
    {
        /// <summary>
        /// Tests the "add" operator.
        /// </summary>
        [Fact]
        public void Add()
        {
            Type4Tester.Create("5 6 add").Pop(11).IsEmpty();

            Type4Tester.Create("5 0.23 add").Pop(5.23).IsEmpty();

            const int bigValue = int.MaxValue - 2;
            ExecutionContext context = Type4Tester.Create($"{bigValue} {bigValue} add").ToExecutionContext();
            double floatResult = Convert.ToDouble(context.Stack.Pop());
            Assert.Equal((long)2 * (long)int.MaxValue - (long)4, floatResult, 1);

            Assert.Empty(context.Stack);
        }

        /// <summary>
        /// Tests the "abs" operator.
        /// </summary>
        [Fact]
        public void Abs()
        {
            Type4Tester.Create("-3 abs 2.1 abs -2.1 abs -7.5 abs")
                .Pop(7.5).Pop(2.1).Pop(2.1).Pop(3).IsEmpty();
        }

        /// <summary>
        /// Tests the "and" operator.
        /// </summary>
        [Fact]
        public void And()
        {
            Type4Tester.Create("true true and true false and")
                .Pop(false).Pop(true).IsEmpty();

            Type4Tester.Create("99 1 and 52 7 and")
                .Pop(4).Pop(1).IsEmpty();
        }

        /// <summary>
        /// Tests the "atan" operator.
        /// </summary>
        [Fact]
        public void Atan()
        {
            Type4Tester.Create("0 1 atan").Pop(0.0).IsEmpty();
            Type4Tester.Create("1 0 atan").Pop(90.0).IsEmpty();
            Type4Tester.Create("-100 0 atan").Pop(270.0).IsEmpty();
            Type4Tester.Create("4 4 atan").Pop(45.0).IsEmpty();
        }

        /// <summary>
        /// Tests the "ceiling" operator.
        /// </summary>
        [Fact]
        public void Ceiling()
        {
            Type4Tester.Create("3.2 ceiling -4.8 ceiling 99 ceiling")
                .Pop(99.0).Pop(-4.0).Pop(4.0).IsEmpty();
        }

        /// <summary>
        /// Tests the "cos" operator.
        /// </summary>
        [Fact]
        public void Cos()
        {
            Type4Tester.Create("0 cos").PopReal(1).IsEmpty();
            Type4Tester.Create("90 cos").PopReal(0).IsEmpty();
        }

        /// <summary>
        /// Tests the "cvi" operator.
        /// </summary>
        [Fact]
        public void Cvi()
        {
            Type4Tester.Create("-47.8 cvi").Pop(-47).IsEmpty();
            Type4Tester.Create("520.9 cvi").Pop(520).IsEmpty();
        }

        /// <summary>
        /// Tests the "cvr" operator.
        /// </summary>
        [Fact]
        public void Cvr()
        {
            Type4Tester.Create("-47.8 cvr").PopReal(-47.8).IsEmpty();
            Type4Tester.Create("520.9 cvr").PopReal(520.9).IsEmpty();
            Type4Tester.Create("77 cvr").PopReal(77).IsEmpty();

            //Check that the data types are really right
            ExecutionContext context = Type4Tester.Create("77 77 cvr").ToExecutionContext();
            Assert.True(context.Stack.Pop() is double, "Expected a real as the result of 'cvr'");
            Assert.True(context.Stack.Pop() is int, "Expected an int from an int literal");
        }

        /// <summary>
        /// Tests the "div" operator.
        /// </summary>
        [Fact]
        public void Div()
        {
            Type4Tester.Create("3 2 div").PopReal(1.5).IsEmpty();
            Type4Tester.Create("4 2 div").PopReal(2.0).IsEmpty();
        }

        /// <summary>
        /// Tests the "exp" operator.
        /// </summary>
        [Fact]
        public void Exp()
        {
            Type4Tester.Create("9 0.5 exp").PopReal(3.0).IsEmpty();
            Type4Tester.Create("-9 -1 exp").PopReal(-0.111111, 0.000001).IsEmpty();
        }

        /// <summary>
        /// Tests the "floor" operator.
        /// </summary>
        [Fact]
        public void Floor()
        {
            Type4Tester.Create("3.2 floor -4.8 floor 99 floor")
                .Pop(99.0).Pop(-5.0).Pop(3.0).IsEmpty();
        }

        /// <summary>
        /// Tests the "div" operator.
        /// </summary>
        [Fact]
        public void IDiv()
        {
            Type4Tester.Create("3 2 idiv").Pop(1).IsEmpty();
            Type4Tester.Create("4 2 idiv").Pop(2).IsEmpty();
            Type4Tester.Create("-5 2 idiv").Pop(-2).IsEmpty();

            Assert.Throws<InvalidCastException>(() => Type4Tester.Create("4.4 2 idiv"));
        }

        /// <summary>
        /// Tests the "ln" operator.
        /// </summary>
        [Fact]
        public void Ln()
        {
            Type4Tester.Create("10 ln").PopReal(2.30259, 0.00001).IsEmpty();
            Type4Tester.Create("100 ln").PopReal(4.60517, 0.00001).IsEmpty();
        }

        /// <summary>
        /// Tests the "log" operator.
        /// </summary>
        [Fact]
        public void Log()
        {
            Type4Tester.Create("10 log").PopReal(1.0).IsEmpty();
            Type4Tester.Create("100 log").PopReal(2.0).IsEmpty();
        }

        /// <summary>
        /// Tests the "mod" operator.
        /// </summary>
        [Fact]
        public void Mod()
        {
            Type4Tester.Create("5 3 mod").Pop(2).IsEmpty();
            Type4Tester.Create("5 2 mod").Pop(1).IsEmpty();
            Type4Tester.Create("-5 3 mod").Pop(-2).IsEmpty();

            Assert.Throws<InvalidCastException>(() => Type4Tester.Create("4.4 2 mod"));
        }

        /// <summary>
        /// Tests the "mul" operator.
        /// </summary>
        [Fact]
        public void Mul()
        {
            Type4Tester.Create("1 2 mul").Pop(2).IsEmpty();
            Type4Tester.Create("1.5 2 mul").PopReal(3.0).IsEmpty();
            Type4Tester.Create("1.5 2.1 mul").PopReal(3.15, 0.001).IsEmpty();
            Type4Tester.Create($"{(int.MaxValue - 3)} 2 mul") //int overflow -> real
                .PopReal(2L * (int.MaxValue - 3), 0.001).IsEmpty();
        }

        /// <summary>
        /// Tests the "neg" operator.
        /// </summary>
        [Fact]
        public void Neg()
        {
            Type4Tester.Create("4.5 neg").PopReal(-4.5).IsEmpty();
            Type4Tester.Create("-3 neg").Pop(3).IsEmpty();

            //Border cases
            Type4Tester.Create((int.MinValue + 1) + " neg").Pop(int.MaxValue).IsEmpty();
            Type4Tester.Create(int.MinValue + " neg").PopReal(-(double)int.MinValue).IsEmpty();
        }

        /// <summary>
        /// Tests the "round" operator.
        /// </summary>
        [Fact]
        public void Round()
        {
            Type4Tester.Create("3.2 round").PopReal(3.0).IsEmpty();
            Type4Tester.Create("6.5 round").PopReal(7.0).IsEmpty();
            Type4Tester.Create("-4.8 round").PopReal(-5.0).IsEmpty();
            Type4Tester.Create("-6.5 round").PopReal(-6.0).IsEmpty();
            Type4Tester.Create("99 round").Pop(99).IsEmpty();
        }

        /// <summary>
        /// Tests the "sin" operator.
        /// </summary>
        [Fact]
        public void Sin()
        {
            Type4Tester.Create("0 sin").PopReal(0).IsEmpty();
            Type4Tester.Create("90 sin").PopReal(1).IsEmpty();
            Type4Tester.Create("-90.0 sin").PopReal(-1).IsEmpty();
        }

        /// <summary>
        /// Tests the "sqrt" operator.
        /// </summary>
        [Fact]
        public void Sqrt()
        {
            Type4Tester.Create("0 sqrt").PopReal(0).IsEmpty();
            Type4Tester.Create("1 sqrt").PopReal(1).IsEmpty();
            Type4Tester.Create("4 sqrt").PopReal(2).IsEmpty();
            Type4Tester.Create("4.4 sqrt").PopReal(2.097617, 0.000001).IsEmpty();
            Assert.Throws<ArgumentException>(() => Type4Tester.Create("-4.1 sqrt"));
        }

        /// <summary>
        /// Tests the "sub" operator.
        /// </summary>
        [Fact]
        public void Sub()
        {
            Type4Tester.Create("5 2 sub -7.5 1 sub").Pop(-8.5f).Pop(3).IsEmpty();
        }

        /// <summary>
        /// Tests the "truncate" operator.
        /// </summary>
        [Fact]
        public void Truncate()
        {
            Type4Tester.Create("3.2 truncate").PopReal(3.0).IsEmpty();
            Type4Tester.Create("-4.8 truncate").PopReal(-4.0).IsEmpty();
            Type4Tester.Create("99 truncate").Pop(99).IsEmpty();
        }

        /// <summary>
        /// Tests the "bitshift" operator.
        /// </summary>
        [Fact]
        public void Bitshift()
        {
            Type4Tester.Create("7 3 bitshift 142 -3 bitshift")
                .Pop(17).Pop(56).IsEmpty();
        }

        /// <summary>
        /// Tests the "eq" operator.
        /// </summary>
        [Fact]
        public void Eq()
        {
            Type4Tester.Create("7 7 eq 7 6 eq 7 -7 eq true true eq false true eq 7.7 7.7 eq")
                .Pop(true).Pop(false).Pop(true).Pop(false).Pop(false).Pop(true).IsEmpty();
        }

        /// <summary>
        /// Tests the "ge" operator.
        /// </summary>
        [Fact]
        public void Ge()
        {
            Type4Tester.Create("5 7 ge 7 5 ge 7 7 ge -1 2 ge")
                .Pop(false).Pop(true).Pop(true).Pop(false).IsEmpty();
        }

        /// <summary>
        /// Tests the "gt" operator.
        /// </summary>
        [Fact]
        public void Gt()
        {
            Type4Tester.Create("5 7 gt 7 5 gt 7 7 gt -1 2 gt")
                .Pop(false).Pop(false).Pop(true).Pop(false).IsEmpty();
        }

        /// <summary>
        /// Tests the "le" operator.
        /// </summary>
        [Fact]
        public void Le()
        {
            Type4Tester.Create("5 7 le 7 5 le 7 7 le -1 2 le")
                .Pop(true).Pop(true).Pop(false).Pop(true).IsEmpty();
        }

        /// <summary>
        /// Tests the "lt" operator.
        /// </summary>
        [Fact]
        public void Lt()
        {
            Type4Tester.Create("5 7 lt 7 5 lt 7 7 lt -1 2 lt")
                .Pop(true).Pop(false).Pop(false).Pop(true).IsEmpty();
        }

        /// <summary>
        /// Tests the "ne" operator.
        /// </summary>
        [Fact]
        public void Ne()
        {
            Type4Tester.Create("7 7 ne 7 6 ne 7 -7 ne true true ne false true ne 7.7 7.7 ne")
                .Pop(false).Pop(true).Pop(false).Pop(true).Pop(true).Pop(false).IsEmpty();
        }

        /// <summary>
        /// Tests the "not" operator.
        /// </summary>
        [Fact]
        public void Not()
        {
            Type4Tester.Create("true not false not")
                .Pop(true).Pop(false).IsEmpty();

            Type4Tester.Create("52 not -37 not")
                .Pop(37).Pop(-52).IsEmpty();
        }

        /// <summary>
        /// Tests the "or" operator.
        /// </summary>
        [Fact]
        public void Or()
        {
            Type4Tester.Create("true true or true false or false false or")
                .Pop(false).Pop(true).Pop(true).IsEmpty();

            Type4Tester.Create("17 5 or 1 1 or")
                .Pop(1).Pop(21).IsEmpty();
        }

        /// <summary>
        /// Tests the "cor" operator.
        /// </summary>
        [Fact]
        public void Xor()
        {
            Type4Tester.Create("true true xor true false xor false false xor")
                .Pop(false).Pop(true).Pop(false).IsEmpty();

            Type4Tester.Create("7 3 xor 12 3 or")
                .Pop(15).Pop(4);
        }

        /// <summary>
        /// Tests the "if" operator.
        /// </summary>
        [Fact]
        public void If()
        {
            Type4Tester.Create("true { 2 1 add } if")
                .Pop(3).IsEmpty();

            Type4Tester.Create("false { 2 1 add } if")
                .IsEmpty();

            Assert.Throws<InvalidCastException>(() => Type4Tester.Create("0 { 2 1 add } if"));
        }

        /// <summary>
        /// Tests the "ifelse" operator.
        /// </summary>
        [Fact]
        public void IfElse()
        {
            Type4Tester.Create("true { 2 1 add } { 2 1 sub } ifelse")
                .Pop(3).IsEmpty();

            Type4Tester.Create("false { 2 1 add } { 2 1 sub } ifelse")
                .Pop(1).IsEmpty();
        }

        /// <summary>
        /// Tests the "copy" operator.
        /// </summary>
        [Fact]
        public void Copy()
        {
            Type4Tester.Create("true 1 2 3 3 copy")
                .Pop(3).Pop(2).Pop(1)
                .Pop(3).Pop(2).Pop(1)
                .Pop(true)
                .IsEmpty();
        }

        /// <summary>
        /// Tests the "dup" operator.
        /// </summary>
        [Fact]
        public void Dup()
        {
            Type4Tester.Create("true 1 2 dup")
                .Pop(2).Pop(2).Pop(1)
                .Pop(true)
                .IsEmpty();
            Type4Tester.Create("true dup")
                .Pop(true).Pop(true).IsEmpty();
        }

        /// <summary>
        /// Tests the "exch" operator.
        /// </summary>
        [Fact]
        public void Exch()
        {
            Type4Tester.Create("true 1 exch")
                .Pop(true).Pop(1).IsEmpty();
            Type4Tester.Create("1 2.5 exch")
                .Pop(1).Pop(2.5).IsEmpty();
        }

        /// <summary>
        /// Tests the "index" operator.
        /// </summary>
        [Fact]
        public void Index()
        {
            Type4Tester.Create("1 2 3 4 0 index")
                .Pop(4).Pop(4).Pop(3).Pop(2).Pop(1).IsEmpty();
            Type4Tester.Create("1 2 3 4 3 index")
                .Pop(1).Pop(4).Pop(3).Pop(2).Pop(1).IsEmpty();
        }

        /// <summary>
        /// Tests the "pop" operator.
        /// </summary>
        [Fact]
        public void Pop()
        {
            Type4Tester.Create("1 pop 7 2 pop")
                .Pop(7).IsEmpty();
            Type4Tester.Create("1 2 3 pop pop")
                .Pop(1).IsEmpty();
        }

        /// <summary>
        /// Tests the "roll" operator.
        /// </summary>
        [Fact]
        public void Roll()
        {
            Type4Tester.Create("1 2 3 4 5 5 -2 roll")
                .Pop(2).Pop(1).Pop(5).Pop(4).Pop(3).IsEmpty();
            Type4Tester.Create("1 2 3 4 5 5 2 roll")
                .Pop(3).Pop(2).Pop(1).Pop(5).Pop(4).IsEmpty();
            Type4Tester.Create("1 2 3 3 0 roll")
                .Pop(3).Pop(2).Pop(1).IsEmpty();
        }
    }
}
