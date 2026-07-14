namespace UglyToad.PdfPig.Tests.Functions.Type4
{
    using UglyToad.PdfPig.Functions.Type4;

    public class OperandStackTests
    {
        [Fact]
        public void PushPopPreservesKindAndValue()
        {
            var stack = new OperandStack(new Operand[8]);
            stack.Push(Operand.Integer(42));
            stack.Push(Operand.Real(1.5));
            stack.Push(Operand.Boolean(true));

            Operand b = stack.Pop();
            Assert.Equal(OperandKind.Boolean, b.Kind);
            Assert.True(b.AsBoolean);

            Operand r = stack.Pop();
            Assert.Equal(OperandKind.Real, r.Kind);
            Assert.Equal(1.5, r.Value);

            Operand i = stack.Pop();
            Assert.Equal(OperandKind.Integer, i.Kind);
            Assert.Equal(42, i.AsInteger);
            Assert.Equal(0, stack.Count);
        }

        [Fact]
        public void PopRealConvertsIntegerAndThrowsForBoolean()
        {
            var stack = new OperandStack(new Operand[4]);
            stack.Push(Operand.Integer(7));
            Assert.Equal(7.0, stack.PopReal());

            Assert.Throws<InvalidCastException>(() => { var s = new OperandStack(new Operand[1]); s.Push(Operand.Boolean(true)); s.PopReal(); });
        }

        [Fact]
        public void PopIntThrowsForReal()
        {
            var stack = new OperandStack(new Operand[4]);
            stack.Push(Operand.Real(4.4));
            Assert.Throws<InvalidCastException>(() => { var s = new OperandStack(new Operand[1]); s.Push(Operand.Real(4.4)); s.PopInt(); });
        }

        [Fact]
        public void PopOnEmptyThrowsInvalidOperation()
        {
            Assert.Throws<InvalidOperationException>(() => { var s = new OperandStack(new Operand[1]); s.Pop(); });
        }

        [Fact]
        public void GrowsBeyondInitialBufferAndSurvivesDispose()
        {
            var stack = new OperandStack(new Operand[2]);
            for (int i = 0; i < 150; i++)
            {
                stack.Push(Operand.Integer(i));
            }
            Assert.Equal(150, stack.Count);
            for (int i = 149; i >= 0; i--)
            {
                Assert.Equal(i, stack.Pop().AsInteger);
            }
            stack.Dispose();
        }

        [Fact]
        public void CopyTopDuplicatesTopItemsInOrder()
        {
            // PostScript: a b c 2 copy -> a b c b c
            var stack = new OperandStack(new Operand[8]);
            stack.Push(Operand.Integer(1));
            stack.Push(Operand.Integer(2));
            stack.Push(Operand.Integer(3));
            stack.CopyTop(2);
            Assert.Equal(new[] { 1, 2, 3, 2, 3 }, ToInts(stack.ToArray()));
        }

        [Fact]
        public void RollRotatesTopItems()
        {
            // PostScript: a b c 3 1 roll -> c a b
            var stack = new OperandStack(new Operand[8]);
            stack.Push(Operand.Integer(1));
            stack.Push(Operand.Integer(2));
            stack.Push(Operand.Integer(3));
            stack.Roll(3, 1);
            Assert.Equal(new[] { 3, 1, 2 }, ToInts(stack.ToArray()));

            // roll back: 3 -1 roll -> a b c
            stack.Roll(3, -1);
            Assert.Equal(new[] { 1, 2, 3 }, ToInts(stack.ToArray()));
        }

        [Fact]
        public void ExchangeSwapsTopTwo()
        {
            var stack = new OperandStack(new Operand[4]);
            stack.Push(Operand.Integer(1));
            stack.Push(Operand.Integer(2));
            stack.Exchange();
            Assert.Equal(new[] { 2, 1 }, ToInts(stack.ToArray()));
        }

        [Fact]
        public void FromTopIndexesFromTheTop()
        {
            var stack = new OperandStack(new Operand[4]);
            stack.Push(Operand.Integer(10));
            stack.Push(Operand.Integer(20));
            Assert.Equal(20, stack.FromTop(0).AsInteger);
            Assert.Equal(10, stack.FromTop(1).AsInteger);
            Assert.Throws<ArgumentOutOfRangeException>(() => { var s = new OperandStack(new Operand[1]); s.Push(Operand.Integer(1)); s.FromTop(5); });
        }

        private static int[] ToInts(Operand[] operands)
        {
            var result = new int[operands.Length];
            for (int i = 0; i < operands.Length; i++)
            {
                result[i] = operands[i].AsInteger;
            }
            return result;
        }
    }
}
