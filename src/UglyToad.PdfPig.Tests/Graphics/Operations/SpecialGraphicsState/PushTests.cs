namespace UglyToad.PdfPig.Tests.Graphics.Operations.SpecialGraphicsState
{
    using PdfPig.Graphics.Operations.SpecialGraphicsState;

    public class PushTests
    {
        private readonly TestOperationContext context = new TestOperationContext();

        [Fact]
        public void PushSymbolCorrect()
        {
            Assert.Equal("q", Push.Symbol);
            Assert.Equal("q", Push.Value.Operator);
        }

        [Fact]
        public void PushAddsToStack()
        {
            Push.Value.Run(context);

            Assert.Equal(2, context.StackSize);
        }
    }
}
