namespace UglyToad.Pdf.Tests.Graphics.Operations.SpecialGraphicsState
{
    using Pdf.Graphics.Operations.SpecialGraphicsState;
    using Xunit;

    public class PushTests
    {
        private readonly TestResourceStore resourceStore = new TestResourceStore();
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
            Push.Value.Run(context, resourceStore);

            Assert.Equal(2, context.StackSize);
        }
    }
}
