namespace UglyToad.Pdf.Tests.Graphics.Operations.General
{
    using Pdf.Graphics.Operations.General;
    using Xunit;

    public class SetMiterLimitTests
    {
        private readonly TestResourceStore resourceStore = new TestResourceStore();
        private readonly TestOperationContext context = new TestOperationContext();

        [Fact]
        public void RunSetsMiterLimitOfCurrentState()
        {
            var limit = new SetMiterLimit(25);
            
            limit.Run(context, resourceStore);

            Assert.Equal(25, context.GetCurrentState().MiterLimit);
        }

        [Fact]
        public void MiterLimitSymbolCorrect()
        {
            Assert.Equal("M", SetMiterLimit.Symbol);
        }
    }
}
